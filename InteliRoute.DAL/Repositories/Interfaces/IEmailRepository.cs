using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces;

public interface IEmailRepository
{
    Task<bool> ExistsAsync(int mailboxId, string externalMessageId, CancellationToken ct = default);
    Task<int> AddAsync(EmailItem email, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<EmailItem> emails, CancellationToken ct = default);
}
