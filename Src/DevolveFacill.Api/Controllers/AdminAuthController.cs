using Asp.Versioning;
using DevolveFacill.Api.Auth;
using DevolveFacill.Api.DTOs;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Infrastructure.Persistence;
using DevolveFacill.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevolveFacill.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth/admin")]
public class AdminAuthController(
    AppDbContext db,
    JwtService jwt,
    AdminRefreshTokenRepository adminTokens) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] AdminLoginRequest req, CancellationToken ct)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(
            a => a.Email == req.Email && a.Active, ct);

        if (admin is null || !BCrypt.Net.BCrypt.Verify(req.Password, admin.PasswordHash))
            return Unauthorized(new { error = "Email ou senha inválidos." });

        var accessToken = jwt.GenerateAccessToken(admin.Id, admin.Role, admin.Name);
        var refreshToken = JwtService.GenerateRefreshToken();

        await adminTokens.SaveAsync(new AdminRefreshToken
        {
            Id = Guid.NewGuid(),
            AdminUserId = admin.Id,
            Token = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwt.RefreshTokenExpiryDays),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return Ok(new AuthResponse(accessToken, refreshToken, admin.Name, admin.Role));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        var stored = await adminTokens.FindRefreshTokenAsync(req.RefreshToken, ct);
        if (stored is null) return Unauthorized(new { error = "Token inválido ou expirado." });

        await adminTokens.RevokeAsync(stored, ct);

        var newAccess = jwt.GenerateAccessToken(stored.AdminUser.Id, stored.AdminUser.Role, stored.AdminUser.Name);
        var newRefresh = JwtService.GenerateRefreshToken();

        await adminTokens.SaveAsync(new AdminRefreshToken
        {
            Id = Guid.NewGuid(),
            AdminUserId = stored.AdminUserId,
            Token = newRefresh,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwt.RefreshTokenExpiryDays),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return Ok(new AuthResponse(newAccess, newRefresh, stored.AdminUser.Name, stored.AdminUser.Role));
    }
}
