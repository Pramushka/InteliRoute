using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Integrations;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc;

[Authorize(Roles = "TenantAdmin")]
public class MailboxController : Controller
{
    private readonly IMailboxAdminRepository _adminRepo;
    private readonly IGmailWebAuthService _auth;
    private readonly IGmailClient _gmailClient;

    public MailboxController(IMailboxAdminRepository adminRepo, IGmailWebAuthService auth, IGmailClient gmailClient)
    {
        _adminRepo = adminRepo;
        _auth = auth;
        _gmailClient = gmailClient;
    }

    [HttpGet]
    public async Task<IActionResult> Setup(CancellationToken ct)
    {
        var tenantId = User.GetTenantId()!.Value;
        var boxes = await _adminRepo.GetByTenantAsync(tenantId, ct);

        // Keep only mailboxes that have a token (consent completed).
        // No DB schema change: we ask the auth service if a token exists.
        var checks = await Task.WhenAll(boxes.Select(async b =>
            new { Box = b, Connected = await _auth.HasTokenAsync(tenantId, b.Id, b.Address, ct) }));

        var connected = checks.Where(x => x.Connected).Select(x => x.Box).OrderBy(b => b.Address).ToList();

        var vm = new MailboxSetupVm
        {
            Mailboxes = connected.Select(b => new MailboxRowVm
            {
                Id = b.Id,
                Address = b.Address,
                IsActive = b.IsActive,
                PollIntervalSec = b.PollIntervalSec,
                LastSyncUtc = b.LastSyncUtc
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantConsent(MailboxSetupVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.Email))
            return await Setup(ct);

        var tenantId = User.GetTenantId()!.Value;
        var mailbox = await _adminRepo.UpsertByAddressAsync(tenantId, vm.Email, ct);

        var redirectAbs = Url.Action(nameof(OAuthCallback), "Mailbox", null, Request.Scheme)!;

        var url = await _auth.GetConsentUrlAsync(tenantId, mailbox.Id, vm.Email, redirectAbs, ct);
        return Redirect(url.AbsoluteUri);
    }

    // Google calls this without auth; we complete consent and redirect back to Setup.
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> OAuthCallback(string? code, string? state, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest("Missing code/state");

        var parts = state.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var tenantId) || !int.TryParse(parts[1], out var mailboxId))
            return BadRequest("Invalid state");

        var redirectAbs = Url.Action(nameof(OAuthCallback), "Mailbox", null, Request.Scheme)!;

        var mbox = await _adminRepo.GetByIdAsync(tenantId, mailboxId, ct);
        if (mbox is null) return RedirectToAction(nameof(Setup));

        // If this throws, consent failed -> nothing is shown on Setup (since we only show connected).
        await _auth.CompleteConsentAsync(tenantId, mailboxId, mbox.Address, code, redirectAbs, ct);

        // Optional: auto-activate this mailbox exclusively. If you prefer manual activation, comment this out.
        await _adminRepo.SetActiveExclusiveAsync(tenantId, mailboxId, ct);

        TempData["ok"] = $"Consent saved for {mbox.Address}.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(int mailboxId, CancellationToken ct)
    {
        var tenantId = User.GetTenantId()!.Value;
        var m = await _adminRepo.GetByIdAsync(tenantId, mailboxId, ct);
        if (m is null)
        {
            TempData["err"] = "Mailbox not found.";
            return RedirectToAction(nameof(Setup));
        }

        // Ensure consent completed; otherwise don't allow enabling.
        if (!await _auth.HasTokenAsync(tenantId, mailboxId, m.Address, ct))
        {
            TempData["err"] = "This mailbox has not completed Google consent.";
            return RedirectToAction(nameof(Setup));
        }

        // Enforce single active mailbox (exclusive using only IsActive).
        await _adminRepo.SetActiveExclusiveAsync(tenantId, mailboxId, ct);

        TempData["ok"] = $"Enabled {m.Address}. Other mailboxes disabled.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(int mailboxId, CancellationToken ct)
    {
        var tenantId = User.GetTenantId()!.Value;
        await _adminRepo.SetActiveAsync(tenantId, mailboxId, false, ct);
        TempData["ok"] = "Fetching disabled.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetFrequency(int mailboxId, int minutes, CancellationToken ct)
    {
        var tenantId = User.GetTenantId()!.Value;
        await _adminRepo.SetPollIntervalAsync(tenantId, mailboxId, Math.Max(1, minutes) * 60, ct);
        TempData["ok"] = "Fetch frequency saved.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualFetch(int mailboxId, CancellationToken ct)
    {
        var tenantId = User.GetTenantId()!.Value;
        var m = await _adminRepo.GetByIdAsync(tenantId, mailboxId, ct);
        if (m is null)
        {
            TempData["err"] = "Mailbox not found.";
            return RedirectToAction(nameof(Setup));
        }

        // Defense-in-depth: Only allow manual fetch when active
        if (!m.IsActive)
        {
            TempData["err"] = "Mailbox is disabled. Enable it first.";
            return RedirectToAction(nameof(Setup));
        }

        var count = await _gmailClient.FetchNewAsync(m, ct);
        TempData["ok"] = $"Fetched {count} new emails for {m.Address}.";
        return RedirectToAction(nameof(Setup));
    }
}
