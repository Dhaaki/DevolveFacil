namespace DevolveFacill.Core.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTimeOffset OrderedAt { get; set; }
    public string Payload { get; set; } = "{}";
    public bool AwaitingDelivery { get; set; }
    public DateTimeOffset CachedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
