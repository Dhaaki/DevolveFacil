using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Domain.Enums;
using DevolveFacill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevolveFacill.Infrastructure.Repositories;

public class ReturnRequestRepository(AppDbContext db)
{
    public Task<ReturnRequest?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ReturnRequests
            .Include(r => r.Customer)
            .Include(r => r.Order).ThenInclude(o => o.Items)
            .Include(r => r.Items).ThenInclude(i => i.OrderItem)
            .Include(r => r.Shipment)
            .Include(r => r.QualityAssessment)
            .Include(r => r.Events.OrderBy(e => e.OccurredAt))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<ReturnRequest>> FindByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        db.ReturnRequests
            .Include(r => r.Shipment)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<List<ReturnRequest>> ListAdminAsync(
        ReturnStatus? status,
        string? carrier,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.ReturnRequests
            .Include(r => r.Customer)
            .Include(r => r.Shipment)
            .AsQueryable();

        if (status.HasValue) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrEmpty(carrier)) query = query.Where(r => r.CarrierCode == carrier);
        if (from.HasValue) query = query.Where(r => r.CreatedAt >= from);
        if (to.HasValue) query = query.Where(r => r.CreatedAt <= to);

        return query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<ReturnRequest> CreateAsync(ReturnRequest r, CancellationToken ct = default)
    {
        db.ReturnRequests.Add(r);
        await db.SaveChangesAsync(ct);
        return r;
    }

    public Task<OrderItem?> FindItemByIdAsync(Guid itemId, CancellationToken ct = default) =>
        db.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);

    public void AddEvent(ReturnRequest? _, ReturnEvent evt) => db.ReturnEvents.Add(evt);

    public void AddQualityAssessment(QualityAssessment assessment) => db.QualityAssessments.Add(assessment);

    public Task SaveAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<string> NextRequestNumberAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.ReturnRequests.CountAsync(r => r.CreatedAt.Year == year, ct);
        return $"RET-{year}-{count + 1:D6}";
    }

    public Task<Dictionary<string, int>> GetStatsAsync(CancellationToken ct = default) =>
        db.ReturnRequests
            .GroupBy(r => r.Status.ToString())
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
}
