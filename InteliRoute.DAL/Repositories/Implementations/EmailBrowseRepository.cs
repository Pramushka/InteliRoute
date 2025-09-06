using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public sealed class EmailBrowseRepository : IEmailBrowseRepository
{
    private readonly AppDbContext _db;
    public EmailBrowseRepository(AppDbContext db) => _db = db;

    public async Task<(IReadOnlyList<EmailItem> Items, int Total)> GetPagedAsync(
        int tenantId, int page, int pageSize, string? q, RouteStatus? status,
        int? mailboxId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = _db.Emails.AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e =>
                e.Subject.Contains(q) || e.From.Contains(q) || e.To.Contains(q) || (e.Snippet ?? "").Contains(q));

        if (status.HasValue) query = query.Where(e => e.RouteStatus == status.Value);
        if (mailboxId.HasValue) query = query.Where(e => e.MailboxId == mailboxId.Value);
        if (from.HasValue) query = query.Where(e => e.ReceivedUtc >= from.Value);
        if (to.HasValue) query = query.Where(e => e.ReceivedUtc <= to.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.ReceivedUtc)
            .Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
            .Take(Math.Max(1, pageSize))
            .Include(e => e.RoutedDepartment)         // <-- load the name
            .ToListAsync(ct);

        return (items, total);
    }


    public async Task<EmailItem?> GetByIdAsync(int tenantId, int id, CancellationToken ct)
    {
        return await _db.Emails.AsNoTracking()
            .Include(e => e.RoutedDepartment)         // <-- load the name
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id, ct);
    }
}
