using InteliRoute.DAL.Entities;

namespace InteliRoute.Models.ViewModels
{
    public sealed class RecentEmailVm
    {
        public int Id { get; set; }
        public DateTime ReceivedUtc { get; set; }
        public string From { get; set; } = "";
        public string Subject { get; set; } = "";

        // For tenant-defined departments
        public string? PredictedDepartment { get; set; }

        // For system buckets (Spam, Other, legacy enum values)
        public Intent? PredictedIntent { get; set; }

        public RouteStatus RouteStatus { get; set; }

        // This property is what the Razor page binds to
        public string IntentLabel =>
            !string.IsNullOrWhiteSpace(PredictedDepartment)
                ? PredictedDepartment!
                : PredictedIntent?.ToString() ?? "—";
    }
}
