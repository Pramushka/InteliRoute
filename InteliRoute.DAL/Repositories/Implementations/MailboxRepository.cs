using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Repositories.Implementations
{
    public class MailboxRepository : IMailboxRepository
    {
        private readonly AppDbContext _db;
        public MailboxRepository(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<Mailbox>> GetActiveDueAsync(DateTime utcNow, CancellationToken ct = default)
        {
            // run if never synced OR interval elapsed
            return await _db.Mailboxes.AsNoTracking()
                .Where(m => m.IsActive &&
                            (m.LastSyncUtc == null || EF.Functions.DateDiffSecond(m.LastSyncUtc.Value, utcNow) >= m.PollIntervalSec))
                .OrderBy(m => m.TenantId).ThenBy(m => m.Address)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(Mailbox box, CancellationToken ct = default)
        {
            _db.Mailboxes.Update(box);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Mailbox>> GetByTenantAsync(int? tenantId, CancellationToken ct = default)
            => tenantId is null
               ? await _db.Mailboxes.AsNoTracking().OrderBy(x => x.Address).ToListAsync(ct)
               : await _db.Mailboxes.AsNoTracking().Where(x => x.TenantId == tenantId)
                        .OrderBy(x => x.Address).ToListAsync(ct);

        public Task<Mailbox?> GetByIdAsync(int id, CancellationToken ct = default)
            => _db.Mailboxes.FindAsync(new object?[] { id }, ct).AsTask();

        public Task<Mailbox?> GetByAddressAsync(int tenantId, string address, CancellationToken ct = default)
            => _db.Mailboxes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Address == address, ct);

        public async Task<int> AddAsync(Mailbox box, CancellationToken ct = default)
        {
            _db.Mailboxes.Add(box);
            await _db.SaveChangesAsync(ct);
            return box.Id;
        }

    }
}
