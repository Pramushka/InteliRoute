using InteliRoute.DAL.Entities;

namespace InteliRoute.Models.ViewModels
{
    public class DashboardVm
    {
        public bool IsSuperAdmin { get; set; }
        public int? TenantId { get; set; }

        // KPIs
        public int Total { get; set; }
        public int Routed { get; set; }
        public int Triage { get; set; }
        public int Failed { get; set; }

        // Charts
        public IDictionary<string, int> MonthlySeries { get; set; } = new Dictionary<string, int>();
        public IDictionary<string, int> IntentDistribution { get; set; } = new Dictionary<string, int>();

        // Recent table (use the VM, not the DAL entity)
        public IReadOnlyList<RecentEmailVm> Recent { get; set; } = Array.Empty<RecentEmailVm>();
    }
}
