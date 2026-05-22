using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevolveFacill.Infrastructure.Repositories;

public class CustomerRepository(AppDbContext db)
{
    public Task<Customer?> FindByCpfAsync(string cpf, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Cpf == cpf, ct);

    public Task<Customer?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> FindByExternalIdAsync(string externalId, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.ExternalId == externalId, ct);

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    public Task<CustomerRefreshToken?> FindRefreshTokenAsync(string token, CancellationToken ct = default) =>
        db.CustomerRefreshTokens
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked && t.ExpiresAt > DateTimeOffset.UtcNow, ct);

    public async Task SaveRefreshTokenAsync(CustomerRefreshToken token, CancellationToken ct = default)
    {
        db.CustomerRefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeRefreshTokenAsync(CustomerRefreshToken token, CancellationToken ct = default)
    {
        token.IsRevoked = true;
        await db.SaveChangesAsync(ct);
    }
}
