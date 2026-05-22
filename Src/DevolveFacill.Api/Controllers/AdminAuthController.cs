using DevolveFacill.Api.Auth;
using DevolveFacill.Api.DTOs;
using DevolveFacill.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevolveFacill.Api.Controllers;

[ApiController]
[Route("api/auth/admin")]
public class AdminAuthController(AppDbContext db, JwtService jwt) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] AdminLoginRequest req, CancellationToken ct)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(
            a => a.Email == req.Email && a.Active, ct);

        if (admin is null || !BCrypt.Net.BCrypt.Verify(req.Password, admin.PasswordHash))
            return Unauthorized(new { error = "Email ou senha inválidos." });

        var token = jwt.GenerateAccessToken(admin.Id, admin.Role, admin.Name);
        return Ok(new AuthResponse(token, null, admin.Name, admin.Role));
    }
}
