using Ganss.Xss;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Models.ViewModels;
using InteliRoute.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.RegularExpressions;

namespace InteliRoute.Controllers.Mvc;

[Authorize(Roles = "TenantAdmin")]
public sealed class EmailsController : Controller
{
    private readonly IEmailBrowseRepository _emails;
    private readonly IMailboxRepository _mailboxes;

    public EmailsController(IEmailBrowseRepository emails, IMailboxRepository mailboxes)
    {
        _emails = emails;
        _mailboxes = mailboxes;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EmailListFiltersVm f, CancellationToken ct)
    {
        // Scope: require tenant id from claims
        var tenantId = User.GetTenantId();
        if (tenantId is null)
            return Forbid();

        // Normalize date filters (inclusive end-of-day)
        DateTime? from = f.FromDate?.Date;
        DateTime? to = f.ToDate.HasValue
            ? f.ToDate.Value.Date.AddDays(1).AddTicks(-1)
            : (DateTime?)null;

        // Avoid tuple deconstruction; use a temporary result
        var result = await _emails.GetPagedAsync(
            tenantId.Value, f.Page, f.PageSize, f.Q, f.Status, f.MailboxId, from, to, ct);

        var items = result.Items;
        var total = result.Total;

        var mboxes = await _mailboxes.GetByTenantAsync(tenantId.Value, ct);

        var vm = new EmailListVm
        {
            Filters = f,
            Total = total,
            Items = items.Select(e => new EmailRowVm
            {
                Id = e.Id,
                MailboxId = e.MailboxId,
                From = e.From,
                To = e.To,
                Subject = e.Subject,
                Snippet = e.Snippet,
                ReceivedUtc = e.ReceivedUtc,
                RouteStatus = e.RouteStatus
            }).ToList(),

            // If tuples cause issues in your view, switch this to a small VM instead.
            Mailboxes = mboxes.Select(m => (m.Id, m.Address)).ToList()
        };

        return View(vm); // Views/Emails/Index.cshtml
    }


    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        if (tenantId is null) return Forbid();

        var e = await _emails.GetByIdAsync(tenantId.Value, id, ct);
        if (e is null) return NotFound();

        // If your DAL has a BodyHtml field, prefer it; else try to detect HTML in BodyText.
        string? rawHtml = null;

        // Prefer an HTML body if your repo exposes one (pseudo: e.BodyHtml)
        // rawHtml = e.BodyHtml;

        if (string.IsNullOrWhiteSpace(rawHtml))
        {
            // Heuristic: if BodyText looks like HTML, treat as HTML; else convert plaintext -> HTML
            if (!string.IsNullOrWhiteSpace(e.BodyText) &&
                Regex.IsMatch(e.BodyText, @"<\s*([a-z]+)(\s|>)", RegexOptions.IgnoreCase))
            {
                rawHtml = e.BodyText;
            }
            else if (!string.IsNullOrWhiteSpace(e.BodyText))
            {
                // Convert plaintext safely to HTML with <br> for line breaks
                var encoded = WebUtility.HtmlEncode(e.BodyText);
                rawHtml = encoded.Replace("\r\n", "<br>").Replace("\n", "<br>").Replace("\r", "<br>");
            }
        }

        // Sanitize
        var sanitizer = HttpContext.RequestServices.GetRequiredService<HtmlSanitizer>();
        var safeHtml = rawHtml is not null ? sanitizer.Sanitize(rawHtml) : null;

        var vm = new EmailDetailsVm
        {
            Id = e.Id,
            From = e.From,
            To = e.To,
            Subject = e.Subject,
            Snippet = e.Snippet,
            BodyText = e.BodyText,
            BodyHtmlSanitized = safeHtml,             
            ReceivedUtc = e.ReceivedUtc,
            RouteStatus = e.RouteStatus,
            RoutedDepartmentId = e.RoutedDepartmentId,
            RoutedDepartmentName = e.RoutedDepartment?.Name,
            RoutedEmail = e.RoutedEmail,
            ErrorMessage = e.ErrorMessage
        };

        return View(vm);
    }

}
