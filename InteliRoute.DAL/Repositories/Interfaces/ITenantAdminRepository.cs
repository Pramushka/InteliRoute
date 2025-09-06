using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces;

public interface ITenantAdminRepository
{
    Task<TenantAdmin?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default);
    Task<IReadOnlyList<TenantAdmin>> GetByTenantAsync(int tenantId, CancellationToken ct = default);
    Task<TenantAdmin?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<TenantAdmin> CreateAsync(
        int tenantId,
        string username,
        string email,
        string passwordHash,
        string role,
        bool isActive,
        CancellationToken ct = default);

    Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default);

    Task ResetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default);
    Task UpdateProfileAsync(int adminId, string username, string email, string role, bool isActive, CancellationToken ct);


}
