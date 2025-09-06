using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class TenantMgmtRepository : ITenantMgmtRepository
{
    private readonly AppDbContext _db;
    public TenantMgmtRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default)
        => _db.Set<Tenant>().AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct)
               .ContinueWith(t => (IReadOnlyList<Tenant>)t.Result, ct);

    public Task<Tenant?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Tenant> CreateAsync(string name, string domainsCsv, bool isActive, CancellationToken ct = default)
    {
        var e = new Tenant
        {
            Name = name.Trim(),
            DomainsCsv = (domainsCsv ?? string.Empty).Trim(),
            IsActive = isActive,
            CreatedUtc = DateTime.UtcNow
        };
        _db.Set<Tenant>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e;
    }

    public async Task<Tenant> UpdateAsync(int id, string name, string domainsCsv, bool isActive, CancellationToken ct = default)
    {
        var e = await _db.Set<Tenant>().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Tenant not found");
        e.Name = name.Trim();
        e.DomainsCsv = (domainsCsv ?? string.Empty).Trim();
        e.IsActive = isActive;
        _db.Set<Tenant>().Update(e);
        await _db.SaveChangesAsync(ct);
        return e;
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var e = await _db.Set<Tenant>().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Tenant not found");
        e.IsActive = isActive;
        _db.Set<Tenant>().Update(e);
        await _db.SaveChangesAsync(ct);
    }
}
