using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc;

[Authorize(Roles = "TenantAdmin,SuperAdmin")]
public class SettingsController : Controller
{
    private readonly IMailboxRepository _mailboxes;

    public SettingsController(IMailboxRepository mailboxes) => _mailboxes = mailboxes;

    [HttpGet]
    public async Task<IActionResult> Fetching(CancellationToken ct)
    {
        var isSuper = User.IsInRole("SuperAdmin");
        var tenantId = isSuper ? (int?)null : User.GetTenantId();

        var list = await _mailboxes.GetByTenantAsync(tenantId, ct);
        return View(new FetchingVm
        {
            IsSuperAdmin = isSuper,
            TenantId = tenantId,
            Mailboxes = list
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddMailbox(FetchingVm vm, CancellationToken ct)
    {
        var tenantId = User.GetTenantId() ?? 0;   // if you add “select tenant” for superadmin, pass that value instead
        if (string.IsNullOrWhiteSpace(vm.NewAddress))
        {
            TempData["Msg"] = "Email address is required.";
            return RedirectToAction(nameof(Fetching));
        }

        var exists = await _mailboxes.GetByAddressAsync(tenantId, vm.NewAddress.Trim(), ct);
        if (exists is null)
        {
            await _mailboxes.AddAsync(new Mailbox
            {
                TenantId = tenantId,
                Address = vm.NewAddress.Trim(),
                Provider = "Gmail",
                IsActive = true,
                FetchMode = FetchMode.Polling,
                PollIntervalSec = vm.NewPollIntervalSec <= 0 ? 60 : vm.NewPollIntervalSec
            }, ct);
            TempData["Msg"] = "Mailbox added. Place credentials.json for this tenant to connect.";
        }
        else TempData["Msg"] = "This mailbox already exists.";

        return RedirectToAction(nameof(Fetching));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var box = await _mailboxes.GetByIdAsync(id, ct);
        if (box is null) return NotFound();
        box.IsActive = !box.IsActive;
        await _mailboxes.UpdateAsync(box, ct);
        return RedirectToAction(nameof(Fetching));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateFrequency(int id, int pollSec, CancellationToken ct)
    {
        var box = await _mailboxes.GetByIdAsync(id, ct);
        if (box is null) return NotFound();
        box.PollIntervalSec = Math.Max(15, pollSec);
        await _mailboxes.UpdateAsync(box, ct);
        return RedirectToAction(nameof(Fetching));
    }

    [HttpPost]
    public async Task<IActionResult> ManualFetch(int id, CancellationToken ct)
    {
        var box = await _mailboxes.GetByIdAsync(id, ct);
        if (box is null) return NotFound();

        // For now, nudge LastSyncUtc; worker will pick it up. Swap to IGmailClient.FetchNewAsync(box) when ready.
        box.LastSyncUtc = DateTime.UtcNow.AddMinutes(-10);
        await _mailboxes.UpdateAsync(box, ct);
        TempData["Msg"] = $"Manual fetch queued for {box.Address}.";
        return RedirectToAction(nameof(Fetching));
    }
}
