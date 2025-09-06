using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class EmailAnalyticsRepository : IEmailAnalyticsRepository
{
    private readonly AppDbContext _db;
    public EmailAnalyticsRepository(AppDbContext db) => _db = db;

    private IQueryable<EmailItem> Scope(int? tenantId) =>
        tenantId is null
            ? _db.Emails.AsNoTracking()
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

        var result = new Dictionary<string, int>();
        for (int i = months - 1; i >= 0; i--)
        {
            var d = DateTime.UtcNow.AddMonths(-i);
            var key = $"{d.Year:D4}-{d.Month:D2}";
            result[key] = grouped.FirstOrDefault(x => x.Key == key)?.Count ?? 0;
        }
        return result;
    }

    // Convenience overload used by HomeController; delegates to the strong-typed one
    public Task<IDictionary<string, int>> GetIntentDistributionAsync(int? tenantId, DateTime? sinceUtc = null, CancellationToken ct = default)
        => GetIntentDistributionAsync(tenantId, sinceUtc ?? DateTime.UtcNow.AddDays(-30), ct);

    // Department-style distribution (RoutedDepartment.Name, else Spam, else Other)
    public async Task<IDictionary<string, int>> GetIntentDistributionAsync(int? tenantId, DateTime sinceUtc, CancellationToken ct)
    {
        var query = Scope(tenantId).Where(e => e.ReceivedUtc >= sinceUtc);

        // EF will LEFT JOIN RoutedDepartment because we reference it in the projection
        var dict = await query
            .Select(e => new
            {
                Label = e.RoutedDepartment != null && !string.IsNullOrEmpty(e.RoutedDepartment.Name)
                            ? e.RoutedDepartment.Name
                            : (e.PredictedIntent == Intent.Spam ? "Spam" : "Other")
            })
            .GroupBy(x => x.Label)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return dict;
    }

    public async Task<IReadOnlyList<EmailItem>> GetRecentEmailsAsync(int? tenantId, int take = 10, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);

        var q = _db.Emails                      // <<-- FIX: Emails (not EmailItems)
            .AsNoTracking()
            .Where(e => e.ReceivedUtc >= since);

        if (tenantId.HasValue)
            q = q.Where(e => e.TenantId == tenantId.Value);

        return await q
            .OrderByDescending(e => e.ReceivedUtc)
            .Take(take)
            .Include(e => e.RoutedDepartment)   // ensure name is populated
            .ToListAsync(ct);
    }
}
