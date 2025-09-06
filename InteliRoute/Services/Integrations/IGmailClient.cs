using InteliRoute.DAL.Entities;

namespace InteliRoute.Services.Integrations;

public interface IGmailClient
{
    /// Fetch new messages since the mailbox.GmailHistoryId; returns number persisted.
    Task<int> FetchNewAsync(Mailbox mailbox, CancellationToken ct = default);
}
