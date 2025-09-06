using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc;

[Authorize(Roles = "TenantAdmin")]
public class RoutingController : Controller
{
    private readonly IDepartmentAdminRepository _deps;

    private const string OtherName = "Other";
    private static readonly string[] DefaultDepartments =
    {
        "HR", "IT", "Finance", "Support", "Sales", "Legal", "Operations", OtherName
    };

    public RoutingController(IDepartmentAdminRepository deps) => _deps = deps;

    [HttpGet]
    public async Task<IActionResult> Setup(CancellationToken ct)
    {
        var tenantId = User.GetTenantId()!.Value;
        var rows = await _deps.GetByTenantAsync(tenantId, ct);

        var byName = rows.ToDictionary(r => r.Name.Trim().ToLowerInvariant(), r => r);
        var vm = new RoutingSetupVm();

        // Always render the canonical set
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
                    Id = 0,
                    Name = name,
                    RoutingEmail = "",
                    Enabled = false
                });
            }
        }

        // Show any custom departments (non-default) that exist in DB
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

    // Enable/Disable with business rules:
    // - Other cannot be disabled.
    // - After any change there must be >= 2 enabled departments AND at least one non-Other enabled.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, string name, bool enable, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToAction(nameof(Setup));

        var tenantId = User.GetTenantId()!.Value;

        // Build current state
        var rows = await _deps.GetByTenantAsync(tenantId, ct);
        var map = rows.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        // Create a simulated "after" state set (name -> enabled)
        var enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Seed with current DB rows
        foreach (var r in rows)
            enabled[r.Name] = r.Enabled;

        // Include missing default departments (treated as currently disabled)
        foreach (var dn in DefaultDepartments)
            if (!enabled.ContainsKey(dn)) enabled[dn] = false;

        // Apply requested change
        if (enable)
        {
            enabled[name] = true;
        }
        else
        {
            // Guard: Other cannot be disabled
            if (name.Equals(OtherName, StringComparison.OrdinalIgnoreCase))
            {
                TempData["err"] = "The fallback department 'Other' cannot be disabled.";
                return RedirectToAction(nameof(Setup));
            }

            // If it doesn't exist yet (id==0) and user asked to disable — no change,
            // but apply rule checks against current state anyway
            if (enabled.ContainsKey(name))
                enabled[name] = false;
        }

        // Validate business rules on the simulated state
        var totalEnabled = enabled.Count(kv => kv.Value);
        var otherEnabled = enabled.TryGetValue(OtherName, out var oe) && oe;
        var nonOtherEnabled = enabled.Any(kv => kv.Value && !kv.Key.Equals(OtherName, StringComparison.OrdinalIgnoreCase));

        if (!otherEnabled)
        {
            TempData["err"] = "The fallback department 'Other' must remain enabled.";
            return RedirectToAction(nameof(Setup));
        }

        if (totalEnabled < 2 || !nonOtherEnabled)
        {
            TempData["err"] = "At least two departments must be enabled: 'Other' plus one additional department.";
            return RedirectToAction(nameof(Setup));
        }

        // Commit the change
        if (enable)
        {
            if (id == 0)
                _ = await _deps.UpsertAsync(tenantId, name, routingEmail: "", ct);
            else
                await _deps.SetEnabledAsync(tenantId, id, true, ct);

            TempData["ok"] = $"Enabled {name}.";
        }
        else
        {
            if (id != 0)
                await _deps.SetEnabledAsync(tenantId, id, false, ct);

            TempData["ok"] = $"Disabled {name}.";
        }

        return RedirectToAction(nameof(Setup));
    }

    // Save/update routing email (keeps Enabled=true on upsert)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEmail(string name, string routingEmail, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToAction(nameof(Setup));

        var tenantId = User.GetTenantId()!.Value;

        _ = await _deps.UpsertAsync(tenantId, name, routingEmail ?? "", ct);

        TempData["ok"] = $"Routing email saved for {name}.";
        return RedirectToAction(nameof(Setup));
    }
}
