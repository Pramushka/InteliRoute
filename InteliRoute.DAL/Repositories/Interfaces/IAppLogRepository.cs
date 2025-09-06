using System.Threading;
using System.Threading.Tasks;
using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces
{
    public interface IAppLogRepository
    {
        Task<AppLog?> FindAsync(long id, CancellationToken ct);
        Task<PagedResult<AppLog>> SearchAsync(AppLogQuery q, CancellationToken ct);
    }
}
