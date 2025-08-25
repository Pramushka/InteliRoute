using InteliRoute.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Repositories.Interfaces
{
    public interface IMailboxRepository
    {
        Task<IReadOnlyList<Mailbox>> GetActiveDueAsync(DateTime utcNow, CancellationToken ct = default);
        Task UpdateAsync(Mailbox box, CancellationToken ct = default);
        Task<IReadOnlyList<Mailbox>> GetByTenantAsync(int? tenantId, CancellationToken ct = default);
        Task<Mailbox?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Mailbox?> GetByAddressAsync(int tenantId, string address, CancellationToken ct = default);
        Task<int> AddAsync(Mailbox box, CancellationToken ct = default);

    }
}
