using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc;

[Authorize(Roles = "TenantAdmin,SuperAdmin")]
public class RoutingController : Controller
{
    // your single source of truth
    private readonly IDepartmentAdminRepository _deps;

    // the canonical list to always show
    private static readonly string[] DefaultDepartments =
    {
        "HR", "IT", "Finance", "Support", "Sales", "Legal", "Operations", "Other"
    };

    public RoutingController(IDepartmentAdminRepository deps) => _deps = deps;

    [HttpGet]
    public async Task<IActionResult> Setup(CancellationToken ct)
    {
        var tenantId = User.GetTenantId()!.Value;
        var rows = await _deps.GetByTenantAsync(tenantId, ct);

        // map by lower-name for quick lookups
        var byName = rows.ToDictionary(r => r.Name.Trim().ToLowerInvariant(), r => r);

        var vm = new RoutingSetupVm();

        // show all predefined depts
        foreach (var name in DefaultDepartments)
        {
            if (byName.TryGetValue(name.ToLowerInvariant(), out var d))
            {
                vm.Departments.Add(new DepartmentRowVm
                {
                    Id = d.Id,
                    Name = d.Name,
                    RoutingEmail = d.RoutingEmail ?? "",
                    Enabled = d.Enabled
                });
            }
            else
            {
                vm.Departments.Add(new DepartmentRowVm
                {
                    Id = 0,                // not created yet
                    Name = name,
                    RoutingEmail = "",
                    Enabled = false
                });
            }
        }

        // (optional) surface any custom departments already in DB (non-default names)
        foreach (var d in rows)
        {
            if (!DefaultDepartments.Any(n => n.Equals(d.Name, StringComparison.OrdinalIgnoreCase)))
            {
                vm.Departments.Add(new DepartmentRowVm
                {
                    Id = d.Id,
                    Name = d.Name,
                    RoutingEmail = d.RoutingEmail ?? "",
                    Enabled = d.Enabled
                });
            }
        }

        return View(vm);
    }

    // Enable/Disable. If Id==0 and enable==true => create via UpsertAsync.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, string name, bool enable, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToAction(nameof(Setup));

        var tenantId = User.GetTenantId()!.Value;

        if (enable)
        {
            if (id == 0)
            {
                // create & enable with empty email
                _ = await _deps.UpsertAsync(tenantId, name, "", ct);
            }
            else
            {
                await _deps.SetEnabledAsync(tenantId, id, true, ct);
            }
        }
        else
        {
            if (id != 0)
                await _deps.SetEnabledAsync(tenantId, id, false, ct);
            // if id==0 and disable => nothing to do (row doesn’t exist yet)
        }

        return RedirectToAction(nameof(Setup));
    }

    // Save/update routing email (Upsert keeps Enabled=true for existing rows and creates if needed)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEmail(string name, string routingEmail, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToAction(nameof(Setup));

        var tenantId = User.GetTenantId()!.Value;

        // NOTE: UpsertAsync in your repo sets Enabled=true. We only render this form when Enabled,
        // so this won’t accidentally enable a disabled one.
        _ = await _deps.UpsertAsync(tenantId, name, routingEmail ?? "", ct);

        return RedirectToAction(nameof(Setup));
    }
}
