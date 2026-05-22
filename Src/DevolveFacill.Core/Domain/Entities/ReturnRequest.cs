using DevolveFacill.Core.Domain.Enums;

namespace DevolveFacill.Core.Domain.Entities;

public class ReturnRequest
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public ResolutionType ResolutionType { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Draft;
    public string ReasonCode { get; set; } = string.Empty;
    public string? ReasonNotes { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? CarrierCode { get; set; }
    public string? VoucherCode { get; set; }
    public string? ErpReturnOrderId { get; set; }
    public DateTimeOffset? FinanceNotifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ReturnItem> Items { get; set; } = [];
    public Shipment? Shipment { get; set; }
    public QualityAssessment? QualityAssessment { get; set; }
    public ICollection<ReturnEvent> Events { get; set; } = [];
}
