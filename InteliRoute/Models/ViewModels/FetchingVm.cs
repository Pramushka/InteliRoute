using InteliRoute.DAL.Entities;

namespace InteliRoute.Models.ViewModels;

public class FetchingVm
{
    public bool IsSuperAdmin { get; set; }
    public int? TenantId { get; set; }

    public string NewAddress { get; set; } = string.Empty;
    public int NewPollIntervalSec { get; set; } = 60;

    public IReadOnlyList<Mailbox> Mailboxes { get; set; } = Array.Empty<Mailbox>();
}
