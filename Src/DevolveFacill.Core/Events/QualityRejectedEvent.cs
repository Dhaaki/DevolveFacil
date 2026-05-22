namespace DevolveFacill.Core.Events;

public record QualityRejectedEvent
{
    public Guid ReturnRequestId { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public IReadOnlyList<string> DamageImageKeys { get; init; } = [];
}
