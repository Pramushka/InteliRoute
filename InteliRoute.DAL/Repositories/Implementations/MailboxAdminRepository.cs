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

    public async Task SetActiveExclusiveAsync(int tenantId, int mailboxId, CancellationToken ct)
    {
        // Ensure target exists and belongs to tenant
        var target = await _db.Set<Mailbox>()
                              .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mailboxId, ct);
        if (target is null)
            throw new KeyNotFoundException("Mailbox not found.");

        // Transaction to keep state consistent
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Disable all current actives for this tenant
        var all = await _db.Set<Mailbox>()
                           .Where(m => m.TenantId == tenantId)
                           .ToListAsync(ct);

        foreach (var m in all)
            m.IsActive = m.Id == mailboxId; // only the target becomes active

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
