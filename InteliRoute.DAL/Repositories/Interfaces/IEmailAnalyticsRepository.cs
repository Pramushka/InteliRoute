using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces;

public record DashboardTotals(int Total, int Routed, int Triage, int Failed);

public interface IEmailAnalyticsRepository
{
    Task<DashboardTotals> GetTotalsAsync(int? tenantId, DateTime? sinceUtc = null, CancellationToken ct = default);

    /// <summary> Month label (yyyy-MM) → count for last N months </summary>
    Task<IDictionary<string, int>> GetMonthlySeriesAsync(int? tenantId, int months = 6, CancellationToken ct = default);

    /// <summary> Intent → count since 'sinceUtc' (default: last 30 days) </summary>
    Task<IDictionary<string, int>> GetIntentDistributionAsync(int? tenantId, DateTime? sinceUtc = null, CancellationToken ct = default);

    Task<IReadOnlyList<EmailItem>> GetRecentEmailsAsync(int? tenantId, int take = 10, CancellationToken ct = default);
}
