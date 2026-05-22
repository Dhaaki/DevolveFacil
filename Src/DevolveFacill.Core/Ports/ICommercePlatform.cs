using System.Text.Json;

namespace DevolveFacill.Core.Ports;

public record OrderSummary(
    string ExternalOrderId,
    string Platform,
    string CustomerExternalId,
    IReadOnlyList<OrderItemSummary> Items,
    decimal Total,
    string Currency,
    DateTimeOffset OrderedAt,
    bool EligibleForReturn,
    int ReturnDeadlineDays,
    bool AwaitingDelivery,
    JsonDocument RawPayload
);

public record OrderItemSummary(
    string Sku,
    string Name,
    int Quantity,
    decimal UnitPrice,
    string ImageUrl
);

public record StoreCreditRequest(
    string ExternalCustomerId,
    decimal Value,
    string ReturnRequestId,
    string Currency
);

public record VoucherResult(
    string VoucherCode,
    decimal Value,
    DateTimeOffset ExpiresAt
);

public record CustomerInfo(
    string ExternalId,
    string Name,
    string Email
);

public interface ICommercePlatform
{
    Task<IReadOnlyList<OrderSummary>> GetOrdersForCustomerAsync(string externalCustomerId, CancellationToken ct = default);
    Task<OrderSummary> GetOrderAsync(string externalOrderId, CancellationToken ct = default);
    Task<VoucherResult> CreateStoreCreditAsync(StoreCreditRequest request, CancellationToken ct = default);
    Task<CustomerInfo?> FindCustomerByDocumentAsync(string cpf, CancellationToken ct = default);
}
