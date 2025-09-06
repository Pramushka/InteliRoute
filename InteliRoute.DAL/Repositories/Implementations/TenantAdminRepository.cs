using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class TenantAdminRepository : ITenantAdminRepository
{
    private readonly AppDbContext _db;
    public TenantAdminRepository(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<TenantAdmin>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
        => _db.Set<TenantAdmin>().AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Username)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<TenantAdmin>)t.Result, ct);

    public Task<TenantAdmin?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default)
    => _db.TenantAdmins
          .AsNoTracking()
          .Where(x => x.IsActive &&
                      (x.Username == usernameOrEmail || x.Email == usernameOrEmail))
          .FirstOrDefaultAsync(ct);
    public Task<TenantAdmin?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Set<TenantAdmin>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<TenantAdmin> CreateAsync(
        int tenantId,
        string username,
        string email,
        string passwordHash,
        string role,
        bool isActive,
        CancellationToken ct = default)
    {
        var exists = await _db.Set<TenantAdmin>()
            .AnyAsync(a => a.Email == email || a.Username == username, ct);
        if (exists)
            throw new InvalidOperationException("Username or Email already exists.");

        var e = new TenantAdmin
        {
            TenantId = tenantId,
            Username = username.Trim(),
            Email = email.Trim(),
            PasswordHash = passwordHash, // already hashed
            Role = string.IsNullOrWhiteSpace(role) ? "TenantAdmin" : role.Trim(),
            IsActive = isActive,
            CreatedUtc = DateTime.UtcNow
        };

        _db.Set<TenantAdmin>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e;
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var e = await _db.Set<TenantAdmin>().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Tenant admin not found");
        e.IsActive = isActive;
        _db.Set<TenantAdmin>().Update(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash required", nameof(passwordHash));

        var e = await _db.Set<TenantAdmin>().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Tenant admin not found");

        e.PasswordHash = passwordHash; // already hashed
        _db.Set<TenantAdmin>().Update(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateProfileAsync(
      int adminId,
      string username,
      string email,
      string role,
      bool isActive,
      CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));

        username = username.Trim();
        email = email.Trim();
        role = string.IsNullOrWhiteSpace(role) ? "TenantAdmin" : role.Trim();

        var admin = await _db.Set<TenantAdmin>()
                             .FirstOrDefaultAsync(a => a.Id == adminId, ct)
                    ?? throw new KeyNotFoundException($"Admin id {adminId} not found.");

        var tenantId = admin.TenantId;

        // Uniqueness checks (case-insensitive) within the same tenant
        var unameLower = username.ToLower();
        var emailLower = email.ToLower();

        var usernameTaken = await _db.Set<TenantAdmin>()
            .AnyAsync(a => a.TenantId == tenantId
                           && a.Id != adminId
                           && a.Username.ToLower() == unameLower, ct);

        if (usernameTaken)
            throw new InvalidOperationException("Username is already in use for this tenant.");

        var emailTaken = await _db.Set<TenantAdmin>()
            .AnyAsync(a => a.TenantId == tenantId
                           && a.Id != adminId
                           && a.Email.ToLower() == emailLower, ct);

        if (emailTaken)
            throw new InvalidOperationException("Email is already in use for this tenant.");

        // Apply updates
        admin.Username = username;
        admin.Email = email;
        admin.Role = role;
        admin.IsActive = isActive;

        // If you track updates:
        // admin.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}

