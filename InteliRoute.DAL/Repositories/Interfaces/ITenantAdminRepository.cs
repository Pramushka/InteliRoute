using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces;

public interface ITenantAdminRepository
{
    Task<TenantAdmin?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken ct = default);
}
