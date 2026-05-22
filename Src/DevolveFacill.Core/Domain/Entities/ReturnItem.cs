using DevolveFacill.Core.Domain.Enums;

namespace DevolveFacill.Core.Domain.Entities;

public class ReturnItem
{
    public Guid Id { get; set; }
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public int QuantityReturned { get; set; }
    public ItemCondition? Condition { get; set; }
}
