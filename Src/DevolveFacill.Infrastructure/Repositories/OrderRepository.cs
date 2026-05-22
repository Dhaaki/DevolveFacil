using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevolveFacill.Infrastructure.Repositories;

public class OrderRepository(AppDbContext db)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public Task<Order?> FindByExternalIdAsync(string externalOrderId, CancellationToken ct = default) =>
        db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.ExternalOrderId == externalOrderId, ct);

    public Task<List<Order>> FindByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId && o.CachedAt > DateTimeOffset.UtcNow - CacheTtl)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync(ct);

    public async Task UpsertAsync(Order order, CancellationToken ct = default)
    {
        var existing = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.ExternalOrderId == order.ExternalOrderId, ct);

        if (existing is null)
        {
            db.Orders.Add(order);
        }
        else
        {
            existing.Status = order.Status;
            existing.Total = order.Total;
            existing.Payload = order.Payload;
            existing.CachedAt = DateTimeOffset.UtcNow;
            db.OrderItems.RemoveRange(existing.Items);
            existing.Items.Clear();
            foreach (var item in order.Items)
            {
                item.OrderId = existing.Id;
                db.OrderItems.Add(item);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<OrderItem?> FindItemByIdAsync(Guid itemId, CancellationToken ct = default) =>
        db.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
}
