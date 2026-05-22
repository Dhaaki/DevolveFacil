namespace DevolveFacill.Core.Domain.Entities;

public class ReturnEvent
{
    public Guid Id { get; set; }
    public Guid ReturnRequestId { get; set; }
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}
