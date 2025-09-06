using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces
{
    public interface IMailboxRepository
    {
        // UI / Tenant scope
        Task<IReadOnlyList<Mailbox>> GetByTenantAsync(int tenantId, CancellationToken ct = default);
        Task<Mailbox?> GetByIdAsync(int tenantId, int mailboxId, CancellationToken ct = default);
        Task<Mailbox?> GetByAddressAsync(int tenantId, string address, CancellationToken ct = default);
        Task<Mailbox> UpsertByAddressAsync(int tenantId, string email, CancellationToken ct = default);
        Task SetActiveAsync(int tenantId, int mailboxId, bool isActive, CancellationToken ct = default);
        Task SetPollIntervalAsync(int tenantId, int mailboxId, int pollIntervalSec, CancellationToken ct = default);

        // Worker / generic ops
        Task<IReadOnlyList<Mailbox>> GetActiveDueAsync(DateTime utcNow, CancellationToken ct = default);
        Task<Mailbox?> GetByIdAsync(int id, CancellationToken ct = default); // non-tenant-scoped (worker)
        Task<int> AddAsync(Mailbox box, CancellationToken ct = default);
        Task UpdateAsync(Mailbox box, CancellationToken ct = default);
    }
}
