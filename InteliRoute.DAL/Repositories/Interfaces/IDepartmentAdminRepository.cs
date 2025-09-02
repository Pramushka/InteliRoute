using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces;

public interface IDepartmentAdminRepository
{
    Task<IReadOnlyList<Department>> GetByTenantAsync(int tenantId, CancellationToken ct = default);
    Task<Department?> GetByIdAsync(int tenantId, int id, CancellationToken ct = default);
    /// <summary>
    /// Upsert by name (case-insensitive) for a tenant. If exists, updates RoutingEmail and Enabled=true.
    /// Returns the saved entity.
    /// </summary>
    Task<Department> UpsertAsync(int tenantId, string name, string routingEmail, CancellationToken ct = default);
    Task SetEnabledAsync(int tenantId, int id, bool enabled, CancellationToken ct = default);
}
