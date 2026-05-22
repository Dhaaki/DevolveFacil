using DevolveFacill.Core.Domain.Enums;

namespace DevolveFacill.Core.Events;

public record QualityApprovedEvent
{
    public Guid ReturnRequestId { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public ResolutionType ResolutionType { get; init; }
    public string CustomerExternalId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerCpf { get; init; } = string.Empty;
    public decimal OrderTotal { get; init; }
    public string Currency { get; init; } = "BRL";
}
