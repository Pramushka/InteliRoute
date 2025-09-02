using InteliRoute.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Repositories.Interfaces
{
    public interface IMailboxAdminRepository
    {
        Task<IReadOnlyList<Mailbox>> GetByTenantAsync(int tenantId, CancellationToken ct = default);
        Task<Mailbox?> GetByIdAsync(int tenantId, int mailboxId, CancellationToken ct = default);
        Task<Mailbox> UpsertByAddressAsync(int tenantId, string email, CancellationToken ct = default);
        Task SetActiveAsync(int tenantId, int mailboxId, bool isActive, CancellationToken ct = default);
        Task SetPollIntervalAsync(int tenantId, int mailboxId, int pollIntervalSec, CancellationToken ct = default);
    }
}
