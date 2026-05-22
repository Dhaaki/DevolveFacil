namespace DevolveFacill.Core.Events;

public record CarrierDeliveryEvent
{
    public string TrackingCode { get; init; } = string.Empty;
    public string CarrierCode { get; init; } = string.Empty;
    public DateTimeOffset DeliveredAt { get; init; }
}
