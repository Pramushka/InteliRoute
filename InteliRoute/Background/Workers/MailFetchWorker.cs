using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Services.Integrations;
using Serilog.Context; 

namespace InteliRoute.Background.Workers;

public class MailFetchWorker : BackgroundService
{
    private readonly ILogger<MailFetchWorker> _log;
    private readonly IServiceProvider _sp;

    public MailFetchWorker(ILogger<MailFetchWorker> log, IServiceProvider sp)
    {
        _log = log;
        _sp = sp;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("MailFetchWorker started");

        // Enrich every line from this worker
        using (LogContext.PushProperty("Source", "MailFetchWorker"))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var mailboxes = scope.ServiceProvider.GetRequiredService<IMailboxRepository>();
                    var gmail = scope.ServiceProvider.GetRequiredService<IGmailClient>();

                    var now = DateTime.UtcNow;
                    var due = await mailboxes.GetActiveDueAsync(now, stoppingToken);

                    if (due.Count == 0)
                    {
                        _log.LogDebug("No mailboxes due at {Now:u}. Sleeping...", now);
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        continue;
                    }

                    _log.LogInformation("Found {Count} mailbox(es) due for fetching at {Now:u}", due.Count, now);

                    foreach (var m in due)
                    {
                        // Per-mailbox context so logs include identifiers automatically
                        using (_log.BeginScope(new Dictionary<string, object?>
                        {
                            ["TenantId"] = m.TenantId,
                            ["MailboxId"] = m.Id,
                            ["Address"] = m.Address
                        }))
                        {
                            try
                            {
                                _log.LogInformation("Fetching mailbox {Address} (PollIntervalSec={Poll})",
                                    m.Address, m.PollIntervalSec);

                                var count = await gmail.FetchNewAsync(m, stoppingToken);

                                _log.LogInformation("Mailbox {Address}: fetched {Count} new message(s)",
                                    m.Address, count);
                            }
                            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                            {
                                _log.LogWarning("Fetch canceled for {Address} due to shutdown request", m.Address);
                                throw; // bubble up to stop gracefully
                            }
                            catch (Exception ex)
                            {
                                _log.LogError(ex, "Fetch failed for {Address}", m.Address);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _log.LogInformation("MailFetchWorker stopping (cancellation requested).");
                    break;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Worker loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _log.LogInformation("MailFetchWorker stopped");
    }
}
