namespace DevolveFacill.Core.Domain.Entities;

public class AdminUser
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Agent";
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
