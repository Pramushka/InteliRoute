// Background/Workers/EmailRoutingWorker.cs
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Services.Mail;
using InteliRoute.Services.Routing;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace InteliRoute.Background.Workers;

public sealed class EmailRoutingWorker : BackgroundService
{
    private readonly ILogger<EmailRoutingWorker> _log;
    private readonly IServiceProvider _sp;
    private readonly RouterApiOptions _router;

    private static readonly HashSet<string> Canonical =
        new(new[] { "HR", "IT", "Finance", "Support", "Sales", "Legal", "Operations", "Other" },
            StringComparer.OrdinalIgnoreCase);

    public EmailRoutingWorker(
        ILogger<EmailRoutingWorker> log,
        IServiceProvider sp,
        IOptions<RouterApiOptions> router)
    {
        _log = log;
        _sp = sp;
        _router = router.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("EmailRoutingWorker started");

        using (LogContext.PushProperty("Source", "EmailRoutingWorker"))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var emails = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
                    var depsRepo = scope.ServiceProvider.GetRequiredService<IDepartmentAdminRepository>();
                    var router = scope.ServiceProvider.GetRequiredService<IRouterClient>();
                    var smtp = scope.ServiceProvider.GetRequiredService<IMailSender>();

                    var batch = await emails.GetForRoutingAsync(take: 50, stoppingToken);

                    if (batch.Count == 0)
                    {
                        _log.LogDebug("No emails pending routing. Sleeping...");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    _log.LogInformation("Routing batch size: {Count}", batch.Count);

                    foreach (var e in batch)
                    {
                        using (_log.BeginScope(new Dictionary<string, object?>
                        {
                            ["EmailId"] = e.Id,
                            ["TenantId"] = e.TenantId,
                            ["MailboxId"] = e.MailboxId
                        }))
                        {
                            try
                            {
                                // 1) tenant’s enabled departments (ensure “Other” present)
                                var rows = await depsRepo.GetByTenantAsync(e.TenantId, stoppingToken);
                                var enabledNames = rows.Where(r => r.Enabled)
                                                       .Select(r => r.Name.Trim())
                                                       .ToList();
                                if (!enabledNames.Any(n => n.Equals("Other", StringComparison.OrdinalIgnoreCase)))
                                    enabledNames.Add("Other");

                                // clamp to canonical names (defensive)
                                enabledNames = enabledNames
                                    .Where(n => Canonical.Contains(n, StringComparer.OrdinalIgnoreCase))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                                _log.LogDebug(
                                    "Enabled={Enabled}; MinConfidence={MinConf:0.00}; UseRules={UseRules}",
                                    string.Join(", ", enabledNames),
                                    _router.MinConfidence,
                                    _router.UseRules
                                );

                                // 2) call router
                                var bodyText = e.BodyText ?? e.Snippet ?? string.Empty;
                                var pred = await router.PredictAsync(
                                    e.Subject ?? string.Empty,
                                    bodyText,
                                    enabledNames,
                                    _router.UseRules,
                                    _router.MinConfidence,
                                    stoppingToken);

                                // Initial label/prob
                                var label = string.IsNullOrWhiteSpace(pred.department) ? "Other" : pred.department.Trim();
                                var prob = pred.prob ?? 0.0;

                                _log.LogInformation("Prediction: {Label} (prob={Prob:0.00})", label, prob);

                                // 3) map label -> enabled row
                                var target = rows.FirstOrDefault(d =>
                                    d.Enabled && label.Equals(d.Name, StringComparison.OrdinalIgnoreCase));

                                // ---- HARD FALLBACKS ----
                                // Fallback to 'Other' if the predicted dept isn't enabled
                                if (target == null)
                                {
                                    var other = rows.FirstOrDefault(d => d.Enabled &&
                                        d.Name.Equals("Other", StringComparison.OrdinalIgnoreCase));
                                    if (other != null)
                                    {
                                        _log.LogWarning("Fallback to Other for email {EmailId} (label '{Label}' not enabled).", e.Id, label);
                                        label = "Other";
                                        target = other;
                                    }
                                }

                                // Optional single-department fallback (besides Other)
                                var realEnabled = rows.Where(d => d.Enabled &&
                                                             !d.Name.Equals("Other", StringComparison.OrdinalIgnoreCase) &&
                                                             !string.IsNullOrWhiteSpace(d.RoutingEmail))
                                                      .ToList();
                                if (target == null && realEnabled.Count == 1)
                                {
                                    target = realEnabled[0];
                                    label = target.Name;
                                    _log.LogInformation("Single-dept fallback: routing email {EmailId} to {Dept}.", e.Id, label);
                                }
                                // ---- END FALLBACKS ----

                                // 4) decide route vs triage
                                var canRoute = target != null &&
                                               !string.IsNullOrWhiteSpace(target.RoutingEmail) &&
                                               prob >= _router.MinConfidence;

                                // Save final intent AFTER fallback so dashboard matches final decision
                                e.PredictedIntent = (Intent)MapIntentId(label);
                                e.Confidence = pred.prob;

                                if (canRoute)
                                {
                                    e.RoutedDepartmentId = target!.Id;
                                    e.RoutedEmail = target!.RoutingEmail;

                                    var fwdSubject = $"[InteliRoute] {e.Subject}";
                                    var body = $"From: {e.From}\nTo: {e.To}\nReceived: {e.ReceivedUtc:u}\n\n{bodyText}";

                                    _log.LogInformation("Routing to {ToEmail} (DepartmentId={DeptId})",
                                        target!.RoutingEmail, target.Id);

                                    await smtp.SendAsync(target!.RoutingEmail!, fwdSubject, body, stoppingToken);

                                    e.RouteStatus = RouteStatus.Routed;
                                    e.ErrorMessage = null;

                                    _log.LogInformation("Routing succeeded");
                                }
                                else
                                {
                                    // keep route fields null if not routed (avoids UI confusion)
                                    e.RoutedDepartmentId = null;
                                    e.RoutedEmail = null;
                                    e.RouteStatus = RouteStatus.Triage;

                                    if (target == null || string.IsNullOrWhiteSpace(target?.RoutingEmail))
                                    {
                                        e.ErrorMessage = "Target department disabled or missing routing email.";
                                        _log.LogWarning("Triaged: target disabled or missing routing email. Label={Label}", label);
                                    }
                                    else if (prob < _router.MinConfidence)
                                    {
                                        e.ErrorMessage = $"Below confidence threshold ({prob:0.00} < {_router.MinConfidence:0.00}).";
                                        _log.LogWarning("Triaged: below confidence threshold ({Prob:0.00} < {Min:0.00})", prob, _router.MinConfidence);
                                    }
                                    else
                                    {
                                        _log.LogWarning("Triaged: unspecified condition");
                                    }
                                }

                                await emails.UpdateAsync(e, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                e.RouteStatus = RouteStatus.Failed;
                                e.ErrorMessage = ex.Message;

                                // persist failure
                                await _sp.CreateScope().ServiceProvider
                                    .GetRequiredService<IEmailRepository>()
                                    .UpdateAsync(e, stoppingToken);

                                _log.LogError(ex, "Routing failed for email {EmailId}", e.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "EmailRoutingWorker loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    // DB mapping for enumerated Intent column
    private static readonly Dictionary<string, int> IntentMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HR"] = 0,
        ["IT"] = 1,
        ["Finance"] = 2,
        ["Support"] = 3,
        ["Sales"] = 4,
        ["Legal"] = 5,
        ["Operations"] = 6,
        ["Other"] = 7
    };

    private static int MapIntentId(string name)
        => IntentMap.TryGetValue(name ?? "", out var id) ? id : IntentMap["Other"];
}
