using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevolveFacill.Infrastructure.Repositories;

public class AdminRefreshTokenRepository(AppDbContext db)
{
    public Task<AdminRefreshToken?> FindRefreshTokenAsync(string token, CancellationToken ct = default) =>
        db.AdminRefreshTokens
            .Include(t => t.AdminUser)
            .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked && t.ExpiresAt > DateTimeOffset.UtcNow, ct);

    public async Task SaveAsync(AdminRefreshToken token, CancellationToken ct = default)
    {
        db.AdminRefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(AdminRefreshToken token, CancellationToken ct = default)
    {
        token.IsRevoked = true;
        await db.SaveChangesAsync(ct);
    }
}
