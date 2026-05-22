using DevolveFacill.Api.Auth;
using DevolveFacill.Api.DTOs;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Ports;
using DevolveFacill.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DevolveFacill.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    CustomerRepository customers,
    JwtService jwt,
    ICommercePlatform commerce,
    IConfiguration config) : ControllerBase
{
    [HttpPost("customer/login")]
    public async Task<ActionResult<AuthResponse>> CustomerLogin([FromBody] CustomerLoginRequest req, CancellationToken ct)
    {
        // Normalize: strip mask characters so "123.456.789-00" == "12345678900"
        var cpf = System.Text.RegularExpressions.Regex.Replace(req.Cpf, @"\D", "");
        req = req with { Cpf = cpf };

        var customer = await customers.FindByCpfAsync(req.Cpf, ct);

        if (customer is null)
        {
            var info = await commerce.FindCustomerByDocumentAsync(req.Cpf, ct);
            if (info is null) return Unauthorized(new { error = "CPF não encontrado." });

            // Guard against duplicate (e.g. same mock customer reached via different CPF input)
            customer = await customers.FindByExternalIdAsync(info.ExternalId, ct);
            if (customer is null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ExternalId = info.ExternalId,
                    Platform = config["Commerce:Provider"] ?? "Mock",
                    Name = info.Name,
                    Email = info.Email,
                    Cpf = req.Cpf,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await customers.CreateAsync(customer, ct);
            }
        }
        else if (!BCrypt.Net.BCrypt.Verify(req.Password, customer.PasswordHash))
        {
            return Unauthorized(new { error = "Senha incorreta." });
        }

        var accessToken = jwt.GenerateAccessToken(customer.Id, "customer", customer.Name);
        var refreshToken = JwtService.GenerateRefreshToken();
        var refreshDays = int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        await customers.SaveRefreshTokenAsync(new CustomerRefreshToken
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Token = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshDays),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return Ok(new AuthResponse(accessToken, refreshToken, customer.Name, "customer"));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        var stored = await customers.FindRefreshTokenAsync(req.RefreshToken, ct);
        if (stored is null) return Unauthorized(new { error = "Token inválido ou expirado." });

        await customers.RevokeRefreshTokenAsync(stored, ct);

        var newAccess = jwt.GenerateAccessToken(stored.Customer.Id, "customer", stored.Customer.Name);
        var newRefresh = JwtService.GenerateRefreshToken();
        var refreshDays = int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        await customers.SaveRefreshTokenAsync(new CustomerRefreshToken
        {
            Id = Guid.NewGuid(),
            CustomerId = stored.CustomerId,
            Token = newRefresh,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshDays),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return Ok(new AuthResponse(newAccess, newRefresh, stored.Customer.Name, "customer"));
    }
}
