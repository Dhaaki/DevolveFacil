using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DevolveFacill.Api.Auth;

public record TokenPair(string AccessToken, string RefreshToken);

public class JwtService(IConfiguration config)
{
    private readonly string _key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key required");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "devolvefacill-api";
    private readonly string _audience = config["Jwt:Audience"] ?? "devolvefacill-client";
    private readonly int _accessMinutes = int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
    public int RefreshTokenExpiryDays { get; } = int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "7");

    public string GenerateAccessToken(Guid subjectId, string role, string name)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, name),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateRefreshToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
}
