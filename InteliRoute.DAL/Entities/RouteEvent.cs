using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Entities;

public class RouteEvent
{
    public int Id { get; set; }
    public int EmailItemId { get; set; }
    public DateTime WhenUtc { get; set; } = DateTime.UtcNow;

    public int? RuleId { get; set; }
    public ActionType ActionType { get; set; }
    public string ActionValue { get; set; } = string.Empty;
    public OutcomeType Outcome { get; set; } = OutcomeType.Applied;
    public string? Notes { get; set; }

    public EmailItem? EmailItem { get; set; }
    public Rule? Rule { get; set; }
}
