using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces;

public interface ITenantMgmtRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Tenant> CreateAsync(string name, string domainsCsv, bool isActive, CancellationToken ct = default);
    Task<Tenant> UpdateAsync(int id, string name, string domainsCsv, bool isActive, CancellationToken ct = default);
    Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
}
