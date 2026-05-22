using System.Text.Json;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Ports;

namespace DevolveFacill.Core.UseCases;

public record CustomerOrdersResult(
    string ExternalOrderId,
    string Platform,
    decimal Total,
    string Currency,
    DateTimeOffset OrderedAt,
    bool EligibleForReturn,
    int ReturnDeadlineDays,
    bool AwaitingDelivery,
    IReadOnlyList<CustomerOrderItemResult> Items
);

public record CustomerOrderItemResult(
    Guid LocalId,
    string Sku,
    string Name,
    int Quantity,
    decimal UnitPrice,
    string ImageUrl
);

public interface IOrderCache
{
    Task<List<Order>> FindByCustomerIdAsync(Guid customerId, CancellationToken ct);
    Task UpsertAsync(Order order, CancellationToken ct);
}

public class GetCustomerOrders(ICommercePlatform commerce, IOrderCache cache)
{
    public async Task<IReadOnlyList<CustomerOrdersResult>> ExecuteAsync(
        Customer customer, CancellationToken ct = default)
    {
        var cached = await cache.FindByCustomerIdAsync(customer.Id, ct);

        if (cached.Count == 0)
        {
            var fresh = await commerce.GetOrdersForCustomerAsync(customer.ExternalId, ct);
            foreach (var summary in fresh)
                await SyncOrderAsync(customer, summary, ct);
            // Re-query so returned items have real DB-generated IDs
            cached = await cache.FindByCustomerIdAsync(customer.Id, ct);
        }

        return cached.Select(MapOrder).ToList();
    }

    private async Task SyncOrderAsync(Customer customer, OrderSummary summary, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ExternalOrderId = summary.ExternalOrderId,
            Platform = summary.Platform,
            CustomerId = customer.Id,
            Status = summary.AwaitingDelivery ? "AwaitingDelivery" : "Completed",
            Total = summary.Total,
            Currency = summary.Currency,
            OrderedAt = summary.OrderedAt,
            AwaitingDelivery = summary.AwaitingDelivery,
            Payload = summary.RawPayload.RootElement.ToString(),
            CachedAt = DateTimeOffset.UtcNow,
            Items = summary.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                Sku = i.Sku,
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                ImageUrl = i.ImageUrl,
                Payload = "{}"
            }).ToList()
        };
        await cache.UpsertAsync(order, ct);
    }

    private static CustomerOrdersResult MapOrder(Order o) => new(
        o.ExternalOrderId, o.Platform, o.Total, o.Currency, o.OrderedAt,
        EligibleForReturn(o), RemainingDays(o), o.AwaitingDelivery,
        o.Items.Select(i => new CustomerOrderItemResult(i.Id, i.Sku, i.Name, i.Quantity, i.UnitPrice, i.ImageUrl)).ToList()
    );

    private static bool EligibleForReturn(Order o) => RemainingDays(o) > 0;
    private static int RemainingDays(Order o)
    {
        var deadline = o.OrderedAt.AddDays(30);
        var remaining = (int)(deadline - DateTimeOffset.UtcNow).TotalDays;
        return Math.Max(0, remaining);
    }
}
