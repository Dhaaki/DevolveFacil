namespace DevolveFacill.Core.Ports;

public record ReturnOrderItem(
    string Sku,
    int Quantity,
    decimal UnitPrice
);

public record ReturnOrderRequest(
    string ReturnRequestId,
    IReadOnlyList<ReturnOrderItem> Items,
    string CustomerDocument,
    string CustomerName
);

public record FinanceNotificationRequest(
    string ReturnRequestId,
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    decimal RefundAmount,
    string Currency,
    string? BankAgency,
    string? BankAccount,
    string? BankCode
);

public interface IErpIntegration
{
    Task<string> CreateReturnOrderAsync(ReturnOrderRequest request, CancellationToken ct = default);
    Task NotifyFinanceDepartmentAsync(FinanceNotificationRequest request, CancellationToken ct = default);
}
