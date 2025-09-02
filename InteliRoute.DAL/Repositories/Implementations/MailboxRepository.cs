using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations
{
    public class MailboxRepository : IMailboxRepository
    {
        private readonly AppDbContext _db;
        public MailboxRepository(AppDbContext db) => _db = db;

        private IQueryable<Mailbox> TenantScope(int tenantId, bool tracking = false)
            => (tracking ? _db.Mailboxes : _db.Mailboxes.AsNoTracking())
               .Where(m => m.TenantId == tenantId);

        public async Task<IReadOnlyList<Mailbox>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
        {
            var list = await TenantScope(tenantId).OrderBy(m => m.Address).ToListAsync(ct);
            return (IReadOnlyList<Mailbox>)list;
        }

        public Task<Mailbox?> GetByIdAsync(int tenantId, int mailboxId, CancellationToken ct = default)
            => TenantScope(tenantId).FirstOrDefaultAsync(m => m.Id == mailboxId, ct);

        // worker/global
        public Task<Mailbox?> GetByIdAsync(int id, CancellationToken ct = default)
            => _db.Mailboxes.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

        public Task<Mailbox?> GetByAddressAsync(int tenantId, string address, CancellationToken ct = default)
            => TenantScope(tenantId).FirstOrDefaultAsync(m => m.Address == address, ct);

        public async Task<Mailbox> UpsertByAddressAsync(int tenantId, string email, CancellationToken ct = default)
        {
            var tracked = await _db.Mailboxes
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Address == email, ct);

            if (tracked is null)
            {
                tracked = new Mailbox
                {
                    TenantId = tenantId,
                    Provider = "Gmail",
                    Address = email.Trim(),
                    IsActive = true,
                    FetchMode = FetchMode.Polling,
                    PollIntervalSec = 60
                };
                _db.Mailboxes.Add(tracked);
            }
            else
            {
                tracked.IsActive = true;
                _db.Mailboxes.Update(tracked);
            }

            await _db.SaveChangesAsync(ct);
            return tracked;
        }

        public async Task SetActiveAsync(int tenantId, int mailboxId, bool isActive, CancellationToken ct = default)
        {
            var tracked = await _db.Mailboxes
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mailboxId, ct)
                ?? throw new InvalidOperationException("Mailbox not found");

            tracked.IsActive = isActive;
            _db.Mailboxes.Update(tracked);
            await _db.SaveChangesAsync(ct);
        }

        public async Task SetPollIntervalAsync(int tenantId, int mailboxId, int pollIntervalSec, CancellationToken ct = default)
        {
            var secs = (int)Math.Clamp(pollIntervalSec, 15, 3600);
            var tracked = await _db.Mailboxes
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mailboxId, ct)
                ?? throw new InvalidOperationException("Mailbox not found");

            tracked.PollIntervalSec = secs;
            _db.Mailboxes.Update(tracked);
            await _db.SaveChangesAsync(ct);
        }

        // *** FIXED: compute "due" in C# (MySQL-safe) ***
        public async Task<IReadOnlyList<Mailbox>> GetActiveDueAsync(DateTime utcNow, CancellationToken ct = default)
        {
            var active = await _db.Mailboxes
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Id)
                .ToListAsync(ct);

            var due = active.Where(m =>
                m.LastSyncUtc == null ||
                (utcNow - m.LastSyncUtc.Value).TotalSeconds >= m.PollIntervalSec
            ).ToList();

            return due;
        }

        public async Task<int> AddAsync(Mailbox box, CancellationToken ct = default)
        {
            _db.Mailboxes.Add(box);
            await _db.SaveChangesAsync(ct);
            return box.Id;
        }

        public async Task UpdateAsync(Mailbox box, CancellationToken ct = default)
        {
            _db.Mailboxes.Update(box);
            await _db.SaveChangesAsync(ct);
        }
    }
}
