using System.Text.Json;
using DevolveFacill.Core.Ports;

namespace DevolveFacill.Adapters.Commerce.Mock;

public class MockCommercePlatform : ICommercePlatform
{
    private static readonly OrderSummary[] SeedOrders =
    [
        new(
            ExternalOrderId: "ORDER-001",
            Platform: "mock",
            CustomerExternalId: "CUST-001",
            Items: [
                new("SKU-DRESS-01", "Vestido Floral Verão", 1, 199.90m, "https://via.placeholder.com/150"),
                new("SKU-BELT-01",  "Cinto de Couro",       1,  89.90m, "https://via.placeholder.com/150")
            ],
            Total: 289.80m,
            Currency: "BRL",
            OrderedAt: DateTimeOffset.UtcNow.AddDays(-10),
            EligibleForReturn: true,
            ReturnDeadlineDays: 20,
            AwaitingDelivery: false,
            RawPayload: JsonDocument.Parse("{\"mock\":true}")
        ),
        new(
            ExternalOrderId: "ORDER-002",
            Platform: "mock",
            CustomerExternalId: "CUST-001",
            Items: [
                new("SKU-SHOES-01", "Scarpin Preto 36", 1, 349.00m, "https://via.placeholder.com/150")
            ],
            Total: 349.00m,
            Currency: "BRL",
            OrderedAt: DateTimeOffset.UtcNow.AddDays(-40),
            EligibleForReturn: false,
            ReturnDeadlineDays: 0,
            AwaitingDelivery: false,
            RawPayload: JsonDocument.Parse("{\"mock\":true}")
        ),
        new(
            ExternalOrderId: "ORDER-003",
            Platform: "mock",
            CustomerExternalId: "CUST-001",
            Items: [
                new("SKU-JACKET-01", "Jaqueta Jeans Oversized", 1, 429.90m, "https://via.placeholder.com/150"),
                new("SKU-SCARF-01",  "Lenço de Seda",           1,  79.90m, "https://via.placeholder.com/150")
            ],
            Total: 509.80m,
            Currency: "BRL",
            OrderedAt: DateTimeOffset.UtcNow.AddDays(-3),
            EligibleForReturn: false,
            ReturnDeadlineDays: 0,
            AwaitingDelivery: true,
            RawPayload: JsonDocument.Parse("{\"mock\":true,\"status\":\"in_transit\"}")
        )
    ];

    public Task<IReadOnlyList<OrderSummary>> GetOrdersForCustomerAsync(string externalCustomerId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OrderSummary>>(SeedOrders);

    public Task<OrderSummary> GetOrderAsync(string externalOrderId, CancellationToken ct = default)
    {
        var order = SeedOrders.FirstOrDefault(o => o.ExternalOrderId == externalOrderId)
            ?? throw new KeyNotFoundException($"Order {externalOrderId} not found in mock.");
        return Task.FromResult(order);
    }

    public Task<VoucherResult> CreateStoreCreditAsync(StoreCreditRequest request, CancellationToken ct = default)
        => Task.FromResult(new VoucherResult(
            VoucherCode: $"MOCK-VOUCHER-{Guid.NewGuid():N}"[..20].ToUpper(),
            Value: request.Value,
            ExpiresAt: DateTimeOffset.UtcNow.AddMonths(6)
        ));

    public Task<CustomerInfo?> FindCustomerByDocumentAsync(string cpf, CancellationToken ct = default)
        => Task.FromResult<CustomerInfo?>(new CustomerInfo("CUST-001", "Cliente Teste", "cliente@teste.com"));
}
