using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InteliRoute.Controllers.Mvc;

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly ITenantMgmtRepository _tenants;
    private readonly ITenantAdminRepository _admins;
    private readonly IAppLogRepository _logsRepo;

    public SuperAdminController(ITenantMgmtRepository tenants, ITenantAdminRepository admins, IAppLogRepository logsRepo)
    {
        _tenants = tenants;
        _admins = admins;
        _logsRepo = logsRepo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? tenantId, CancellationToken ct)
    {
        var all = await _tenants.GetAllAsync(ct);
        var vm = new SuperAdminIndexVm
        {
            Tenants = all.Select(t => new TenantRowVm
            {
                Id = t.Id,
                Name = t.Name,
                DomainsCsv = t.DomainsCsv,
                IsActive = t.IsActive,
                CreatedUtc = t.CreatedUtc
            }).ToList()
        };

        vm.SelectedTenantId = tenantId ?? vm.Tenants.FirstOrDefault()?.Id;

        if (vm.SelectedTenantId is int selId)
        {
            var t = await _tenants.GetByIdAsync(selId, ct);
            if (t != null)
            {
                vm.EditTenant = new UpdateTenantVm
                {
                    Id = t.Id,
                    Name = t.Name,
                    DomainsCsv = t.DomainsCsv,
                    IsActive = t.IsActive
                };
            }

            var admins = await _admins.GetByTenantAsync(selId, ct);
            vm.TenantAdmins = admins.Select(a => new TenantAdminRowVm
            {
                Id = a.Id,
                Username = a.Username,
                Email = a.Email,
                Role = a.Role,
                IsActive = a.IsActive,
                CreatedUtc = a.CreatedUtc
            }).ToList();

            vm.NewAdmin = new NewTenantAdminVm { TenantId = selId, Role = "TenantAdmin", IsActive = true };
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTenantAdmin(UpdateTenantAdminVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["err"] = "Please complete the admin fields.";
            return RedirectToAction(nameof(Index), new { tenantId = vm.TenantId });
        }

        // Update profile
        await _admins.UpdateProfileAsync(vm.Id, vm.Username, vm.Email, vm.Role, vm.IsActive, ct);

        TempData["ok"] = "Admin updated.";
        return RedirectToAction(nameof(Index), new { tenantId = vm.TenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant([Bind(Prefix = "NewTenant")] NewTenantVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["err"] = "Please fill tenant name.";
            return RedirectToAction(nameof(Index));
        }

        _ = await _tenants.CreateAsync(vm.Name, vm.DomainsCsv ?? "", vm.IsActive, ct);
        TempData["ok"] = "Tenant created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTenant([Bind(Prefix = "EditTenant")] UpdateTenantVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["err"] = "Invalid tenant data.";
            return RedirectToAction(nameof(Index), new { tenantId = vm.Id });
        }

        _ = await _tenants.UpdateAsync(vm.Id, vm.Name, vm.DomainsCsv ?? "", vm.IsActive, ct);
        TempData["ok"] = "Tenant updated.";
        return RedirectToAction(nameof(Index), new { tenantId = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTenantActive(int id, bool enable, CancellationToken ct)
    {
        await _tenants.SetActiveAsync(id, enable, ct);
        TempData["ok"] = "Tenant status updated.";
        return RedirectToAction(nameof(Index), new { tenantId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenantAdmin([Bind(Prefix = "NewAdmin")] NewTenantAdminVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["err"] = "Please complete admin fields.";
            return RedirectToAction(nameof(Index), new { tenantId = vm.TenantId });
        }

        var hash = PasswordHelper.HashPassword(vm.Password);

        _ = await _admins.CreateAsync(
            vm.TenantId,
            vm.Username,
            vm.Email,
            hash,
            string.IsNullOrWhiteSpace(vm.Role) ? "TenantAdmin" : vm.Role,
            vm.IsActive,
            ct);

        TempData["ok"] = "Tenant admin created.";
        return RedirectToAction(nameof(Index), new { tenantId = vm.TenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdminActive(int id, int tenantId, bool enable, CancellationToken ct)
    {
        await _admins.SetActiveAsync(id, enable, ct);
        TempData["ok"] = "Admin status updated.";
        return RedirectToAction(nameof(Index), new { tenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAdminPassword(ResetAdminPasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["err"] = "Password required.";
            return RedirectToAction(nameof(Index), new { tenantId = vm.TenantId });
        }

        var hash = PasswordHelper.HashPassword(vm.NewPassword);
        await _admins.ResetPasswordHashAsync(vm.Id, hash, ct);

        TempData["ok"] = "Password reset.";
        return RedirectToAction(nameof(Index), new { tenantId = vm.TenantId });
    }

    [HttpGet]
    public async Task<IActionResult> Logs([FromQuery] LogFilterVm filter, CancellationToken ct)
    {
        // Normalize: make DateTo inclusive → convert to exclusive upper bound (end-of-day + 1 tick)
        DateTime? toExclusive = null;
        if (filter.DateTo.HasValue)
            toExclusive = filter.DateTo.Value.Date.AddDays(1);

        var q = new AppLogQuery
        {
            DateFrom = filter.DateFrom?.Date,
            DateTo = filter.DateTo.HasValue
                      ? filter.DateTo.Value.Date.AddDays(1)
                      : (DateTime?)null,
            Level = string.IsNullOrWhiteSpace(filter.Level) ? "All" : filter.Level,
            TenantId = filter.TenantId,
            MailboxId = filter.MailboxId,
            EmailId = filter.EmailId,
            HasException = filter.HasException,
            Text = filter.Text,
            Page = filter.Page <= 0 ? 1 : filter.Page,
            PageSize = filter.PageSize <= 0 ? 25 : filter.PageSize
        };


        var page = await _logsRepo.SearchAsync(q, ct);

        var tenants = await _tenants.GetAllAsync(ct);
        var vm = new LogsIndexVm
        {
            Filter = filter,
            Rows = page.Items.Select(x => new LogRowVm
            {
                Id = x.Id,
                Timestamp = x.Timestamp,
                Level = x.Level,
                Source = x.Source,
                Message = x.Message,
                TenantId = x.TenantId,
                MailboxId = x.MailboxId,
                EmailId = x.EmailId,
                HasException = !string.IsNullOrEmpty(x.Exception)
            }).ToList(),
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize,
            IsSuperAdmin = true,
            Tenants = tenants.Select(t => new TenantChoiceVm { Id = t.Id, Name = t.Name }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LogDetails(long id, CancellationToken ct)
    {
        var log = await _logsRepo.FindAsync(id, ct);
        if (log == null) return NotFound();
        return View(log);
    }
}
