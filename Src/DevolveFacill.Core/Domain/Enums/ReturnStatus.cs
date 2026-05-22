namespace DevolveFacill.Core.Domain.Enums;

public enum ReturnStatus
{
    Draft,
    PendingLabel,
    LabelGenerated,
    InTransit,
    Delivered,
    QualityApproved,
    QualityRejected,
    PendingRefund,
    PendingVoucher,
    ClosedRefund,
    ClosedExchange,
    ClosedRejected,
    Cancelled,
    Error
}
