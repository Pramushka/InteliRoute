using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Entities;

public class Mailbox
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Provider { get; set; } = "Gmail"; // keep for future providers
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public FetchMode FetchMode { get; set; } = FetchMode.Polling;
    public int PollIntervalSec { get; set; } = 15;

    public DateTime? LastSyncUtc { get; set; }
    public long? GmailHistoryId { get; set; }
    public string? OAuthCredentialRef { get; set; }  // path/key to creds (not the secret itself)

    public Tenant? Tenant { get; set; }
}

