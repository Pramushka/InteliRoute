using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class EmailAnalyticsRepository : IEmailAnalyticsRepository
{
    private readonly AppDbContext _db;
    public EmailAnalyticsRepository(AppDbContext db) => _db = db;

    private IQueryable<EmailItem> Scope(int? tenantId)
        => tenantId is null ? _db.Emails.AsNoTracking()
                            : _db.Emails.AsNoTracking().Where(e => e.TenantId == tenantId.Value);

    public async Task<DashboardTotals> GetTotalsAsync(int? tenantId, DateTime? sinceUtc = null, CancellationToken ct = default)
    {
        var q = Scope(tenantId);
        if (sinceUtc is not null) q = q.Where(e => e.ReceivedUtc >= sinceUtc);

        var total = await q.CountAsync(ct);
        var routed = await q.Where(e => e.RouteStatus == RouteStatus.Routed).CountAsync(ct);
        var triage = await q.Where(e => e.RouteStatus == RouteStatus.Triage).CountAsync(ct);
        var failed = await q.Where(e => e.RouteStatus == RouteStatus.Failed).CountAsync(ct);

        return new DashboardTotals(total, routed, triage, failed);
    }

    public async Task<IDictionary<string, int>> GetMonthlySeriesAsync(int? tenantId, int months = 6, CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day).AddMonths(1 - months); // first day N-1 months ago
        var q = Scope(tenantId).Where(e => e.ReceivedUtc >= from);

        var grouped = await q
            .GroupBy(e => new { e.ReceivedUtc.Year, e.ReceivedUtc.Month })
            .Select(g => new { Key = $"{g.Key.Year:D4}-{g.Key.Month:D2}", Count = g.Count() })
            .ToListAsync(ct);

        // ensure missing months have 0
        var result = new Dictionary<string, int>();
        for (int i = months - 1; i >= 0; i--)
        {
            var d = DateTime.UtcNow.AddMonths(-i);
            var key = $"{d.Year:D4}-{d.Month:D2}";
            result[key] = grouped.FirstOrDefault(x => x.Key == key)?.Count ?? 0;
        }
        return result;
    }

    public async Task<IDictionary<string, int>> GetIntentDistributionAsync(int? tenantId, DateTime? sinceUtc = null, CancellationToken ct = default)
    {
        var since = sinceUtc ?? DateTime.UtcNow.AddDays(-30);
        var q = Scope(tenantId).Where(e => e.ReceivedUtc >= since);

        var data = await q
            .GroupBy(e => e.PredictedIntent.HasValue ? e.PredictedIntent.Value.ToString() : "Unknown")
            .Select(g => new { Intent = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return data.ToDictionary(x => x.Intent, x => x.Count);
    }

    public async Task<IReadOnlyList<EmailItem>> GetRecentEmailsAsync(int? tenantId, int take = 10, CancellationToken ct = default)
    {
        var q = Scope(tenantId)
            .OrderByDescending(e => e.ReceivedUtc)
            .Take(take);
        return await q.ToListAsync(ct);
    }
}
