namespace DevolveFacill.Core.Events;

public record ReturnSubmittedEvent
{
    public Guid ReturnRequestId { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string CarrierCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; init; }
}
