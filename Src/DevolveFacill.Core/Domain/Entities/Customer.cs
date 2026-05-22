namespace DevolveFacill.Core.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ReturnRequest> ReturnRequests { get; set; } = [];
}
