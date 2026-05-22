namespace DevolveFacill.Api.DTOs;

public record CustomerLoginRequest(string Cpf, string Password);
public record AdminLoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string? RefreshToken, string Name, string Role);
