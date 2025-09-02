using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class MailboxAdminRepository : IMailboxAdminRepository
{
    private readonly AppDbContext _db;
    public MailboxAdminRepository(AppDbContext db) => _db = db;

    private IQueryable<Mailbox> Scope(int tenantId, bool tracking = false)
        => (tracking ? _db.Mailboxes : _db.Mailboxes.AsNoTracking())
           .Where(m => m.TenantId == tenantId);

    public Task<IReadOnlyList<Mailbox>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
        => Scope(tenantId).OrderBy(m => m.Address).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Mailbox>)t.Result, ct);

    public Task<Mailbox?> GetByIdAsync(int tenantId, int mailboxId, CancellationToken ct = default)
        => Scope(tenantId).FirstOrDefaultAsync(m => m.Id == mailboxId, ct);

    public async Task<Mailbox> UpsertByAddressAsync(int tenantId, string email, CancellationToken ct = default)
    {
        var e = await _db.Mailboxes.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Address == email, ct);
        if (e is null)
        {
            e = new Mailbox { TenantId = tenantId, Address = email, IsActive = true };
            _db.Mailboxes.Add(e);
        }
        else
        {
            e.IsActive = true;
            _db.Mailboxes.Update(e);
        }
        await _db.SaveChangesAsync(ct);
        return e;
    }

    public async Task SetActiveAsync(int tenantId, int mailboxId, bool isActive, CancellationToken ct = default)
    {
        var e = await _db.Mailboxes.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mailboxId, ct)
                ?? throw new InvalidOperationException("Mailbox not found");
        e.IsActive = isActive;
        _db.Mailboxes.Update(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetPollIntervalAsync(int tenantId, int mailboxId, int pollIntervalSec, CancellationToken ct = default)
    {
        var secs = (int)Math.Clamp(pollIntervalSec, 15, 3600);
        var e = await _db.Mailboxes.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mailboxId, ct)
                ?? throw new InvalidOperationException("Mailbox not found");
        e.PollIntervalSec = secs;
        _db.Mailboxes.Update(e);
        await _db.SaveChangesAsync(ct);
    }
}
