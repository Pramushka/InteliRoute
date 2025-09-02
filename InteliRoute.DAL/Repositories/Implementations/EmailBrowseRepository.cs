using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public sealed class EmailBrowseRepository : IEmailBrowseRepository
{
    private readonly AppDbContext _db;
    public EmailBrowseRepository(AppDbContext db) => _db = db;

    public async Task<(IReadOnlyList<EmailItem> Items, int Total)> GetPagedAsync( int tenantId, int page, int pageSize, string? q, RouteStatus? status, int? mailboxId,  DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var query = _db.Set<EmailItem>()
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim();
            query = query.Where(e =>
                e.Subject.Contains(needle) ||
                e.From.Contains(needle) ||
                e.To.Contains(needle) ||
                (e.Snippet != null && e.Snippet.Contains(needle)));
        }

        if (status.HasValue)
            query = query.Where(e => e.RouteStatus == status.Value);

        if (mailboxId.HasValue)
            query = query.Where(e => e.MailboxId == mailboxId.Value);

        if (fromUtc.HasValue)
            query = query.Where(e => e.ReceivedUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(e => e.ReceivedUtc <= toUtc.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.ReceivedUtc)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<EmailItem?> GetByIdAsync(int tenantId, int id, CancellationToken ct = default)
        => _db.Set<EmailItem>()
              .AsNoTracking()
              .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id, ct);
}
