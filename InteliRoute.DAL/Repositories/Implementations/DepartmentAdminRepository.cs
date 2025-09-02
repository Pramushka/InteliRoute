using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class DepartmentAdminRepository : IDepartmentAdminRepository
{
    private readonly AppDbContext _db;
    public DepartmentAdminRepository(AppDbContext db) => _db = db;

    private IQueryable<Department> Scope(int tenantId, bool tracking = false)
        => (tracking ? _db.Departments : _db.Departments.AsNoTracking())
           .Where(d => d.TenantId == tenantId);

    public Task<IReadOnlyList<Department>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
        => Scope(tenantId).OrderBy(d => d.Name).ToListAsync(ct)
           .ContinueWith(t => (IReadOnlyList<Department>)t.Result, ct);

    public Task<Department?> GetByIdAsync(int tenantId, int id, CancellationToken ct = default)
        => Scope(tenantId).FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Department> UpsertAsync(int tenantId, string name, string routingEmail, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        var existing = await _db.Departments
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Name == normalized, ct);

        if (existing is null)
        {
            existing = new Department
            {
                TenantId = tenantId,
                Name = normalized,
                RoutingEmail = routingEmail.Trim(),
                Enabled = true,
                CreatedUtc = DateTime.UtcNow
            };
            _db.Departments.Add(existing);
        }
        else
        {
            existing.RoutingEmail = routingEmail.Trim();
            existing.Enabled = true;
            _db.Departments.Update(existing);
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task SetEnabledAsync(int tenantId, int id, bool enabled, CancellationToken ct = default)
    {
        var dep = await _db.Departments.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct)
                  ?? throw new InvalidOperationException("Department not found");
        dep.Enabled = enabled;
        _db.Departments.Update(dep);
        await _db.SaveChangesAsync(ct);
    }
}
