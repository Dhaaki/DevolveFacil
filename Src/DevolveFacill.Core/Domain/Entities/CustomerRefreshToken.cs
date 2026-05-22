namespace DevolveFacill.Core.Domain.Entities;

public class CustomerRefreshToken
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}
