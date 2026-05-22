using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Domain.Enums;

namespace DevolveFacill.Core.UseCases;

public record CreateReturnInput(
    Guid CustomerId,
    string Platform,
    Guid OrderId,
    ResolutionType ResolutionType,
    string ReasonCode,
    string? ReasonNotes,
    IReadOnlyList<ReturnItemInput> Items
);

public record ReturnItemInput(Guid OrderItemId, int QuantityReturned);

public interface IReturnRequestWriter
{
    Task<string> NextRequestNumberAsync(CancellationToken ct);
    Task<ReturnRequest> CreateAsync(ReturnRequest r, CancellationToken ct);
    Task<OrderItem?> FindItemByIdAsync(Guid itemId, CancellationToken ct);
}

public class CreateReturnRequest(IReturnRequestWriter writer)
{
    public async Task<ReturnRequest> ExecuteAsync(CreateReturnInput input, CancellationToken ct = default)
    {
        if (input.Items.Count == 0)
            throw new ArgumentException("At least one item is required.");

        var requestNumber = await writer.NextRequestNumberAsync(ct);

        var returnItems = new List<ReturnItem>();
        foreach (var item in input.Items)
        {
            var orderItem = await writer.FindItemByIdAsync(item.OrderItemId, ct)
                ?? throw new KeyNotFoundException($"OrderItem {item.OrderItemId} not found.");

            if (item.QuantityReturned < 1 || item.QuantityReturned > orderItem.Quantity)
                throw new ArgumentException($"Invalid quantity for item {item.OrderItemId}.");

            returnItems.Add(new ReturnItem
            {
                Id = Guid.NewGuid(),
                OrderItemId = item.OrderItemId,
                QuantityReturned = item.QuantityReturned
            });
        }

        var returnRequest = new ReturnRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = requestNumber,
            CustomerId = input.CustomerId,
            OrderId = input.OrderId,
            Platform = input.Platform,
            ResolutionType = input.ResolutionType,
            ReasonCode = input.ReasonCode,
            ReasonNotes = input.ReasonNotes,
            Status = ReturnStatus.Draft,
            Items = returnItems,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await writer.CreateAsync(returnRequest, ct);
    }
}
