using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc;

[Authorize(Roles = "TenantAdmin,SuperAdmin")]
public class HomeController : Controller
{
    private readonly IEmailAnalyticsRepository _analytics;

    public HomeController(IEmailAnalyticsRepository analytics) => _analytics = analytics;

    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var isSuper = User.IsSuperAdmin();
        var tenantId = isSuper ? (int?)null : User.GetTenantId();

        var totals = await _analytics.GetTotalsAsync(tenantId, sinceUtc: DateTime.UtcNow.AddDays(-30), ct);
        var monthly = await _analytics.GetMonthlySeriesAsync(tenantId, months: 6, ct);
        var intents = await _analytics.GetIntentDistributionAsync(tenantId, sinceUtc: DateTime.UtcNow.AddDays(-30), ct);
        var recent = await _analytics.GetRecentEmailsAsync(tenantId, take: 10, ct);

        var vm = new DashboardVm
        {
            IsSuperAdmin = isSuper,
            TenantId = tenantId,
            Total = totals.Total,
            Routed = totals.Routed,
            Triage = totals.Triage,
            Failed = totals.Failed,
            MonthlySeries = monthly,
            IntentDistribution = intents,
            Recent = recent
        };

        return View(vm);
    }

    public IActionResult Index() => RedirectToAction(nameof(Dashboard));
}
