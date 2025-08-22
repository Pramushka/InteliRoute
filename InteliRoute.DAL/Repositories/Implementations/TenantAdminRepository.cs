using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Context;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class TenantAdminRepository : ITenantAdminRepository
{
    private readonly AppDbContext _db;
    public TenantAdminRepository(AppDbContext db) => _db = db;

    public Task<TenantAdmin?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default)
        => _db.TenantAdmins
              .AsNoTracking()
              .Where(x => x.IsActive &&
                          (x.Username == usernameOrEmail || x.Email == usernameOrEmail))
              .FirstOrDefaultAsync(ct);
}
