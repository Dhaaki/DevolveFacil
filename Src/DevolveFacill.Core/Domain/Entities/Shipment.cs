namespace DevolveFacill.Core.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public string CarrierCode { get; set; } = string.Empty;
    public string TrackingCode { get; set; } = string.Empty;
    public string? LabelStorageKey { get; set; }
    public string Status { get; set; } = "LabelGenerated";
    public DateOnly? EstimatedDeliveryDate { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string CarrierPayload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
