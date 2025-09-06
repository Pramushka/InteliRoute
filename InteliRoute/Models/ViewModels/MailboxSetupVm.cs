using System.ComponentModel.DataAnnotations;

namespace InteliRoute.Models.ViewModels;

public sealed class MailboxSetupVm
{
    [Required, EmailAddress]
    public string? Email { get; set; }
    public List<MailboxRowVm> Mailboxes { get; set; } = new();
}

public sealed class MailboxRowVm
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int PollIntervalSec { get; set; }
    public DateTime? LastSyncUtc { get; set; }
}
