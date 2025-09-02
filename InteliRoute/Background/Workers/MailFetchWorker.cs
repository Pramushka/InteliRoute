using InteliRoute.DAL.Repositories.Interfaces;
using InteliRoute.Services.Integrations;

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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var mailboxes = scope.ServiceProvider.GetRequiredService<IMailboxRepository>();
                var gmail = scope.ServiceProvider.GetRequiredService<IGmailClient>();

                var due = await mailboxes.GetActiveDueAsync(DateTime.UtcNow, stoppingToken);

                foreach (var m in due)
                {
                    try
                    {
                        var count = await gmail.FetchNewAsync(m, stoppingToken);
                        _log.LogInformation("Mailbox {Address}: {Count} new", m.Address, count);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Fetch failed for {Address}", m.Address);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Worker loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
