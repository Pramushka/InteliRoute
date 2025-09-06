using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Entities;

public class EmailItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int MailboxId { get; set; }

    public string ExternalMessageId { get; set; } = string.Empty; // Gmail message id
    public string? ThreadId { get; set; }

    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public string? BodyText { get; set; }

    public DateTime ReceivedUtc { get; set; }

    public Intent? PredictedIntent { get; set; }    // null until classified
    public double? Confidence { get; set; }

    public RouteStatus RouteStatus { get; set; } = RouteStatus.New;
    public int? RoutedDepartmentId { get; set; }
    public string? RoutedEmail { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
    public Mailbox? Mailbox { get; set; }
    public Department? RoutedDepartment { get; set; }
    public ICollection<RouteEvent> RouteEvents { get; set; } = [];
}
