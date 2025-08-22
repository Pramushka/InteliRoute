using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Entities;

public class Rule
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;     // lower runs first
    public bool Enabled { get; set; } = true;

    public string Predicate { get; set; } = string.Empty; // e.g. "predicted==Support && conf>=0.85 || subject~invoice"
    public ActionType ActionType { get; set; } = ActionType.RouteToDepartment;
    public string ActionValue { get; set; } = string.Empty; // deptId or email depending on ActionType

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
