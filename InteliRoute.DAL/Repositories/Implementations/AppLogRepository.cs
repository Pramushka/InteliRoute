using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations
{
    public sealed class AppLogRepository : IAppLogRepository
    {
        private readonly AppDbContext _db;
        public AppLogRepository(AppDbContext db) => _db = db;

        public async Task<AppLog?> FindAsync(long id, CancellationToken ct)
            => await _db.AppLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<PagedResult<AppLog>> SearchAsync(AppLogQuery q, CancellationToken ct)
        {
            var src = _db.AppLogs.AsNoTracking().AsQueryable();

            if (q.DateFrom.HasValue) src = src.Where(x => x.Timestamp >= q.DateFrom.Value);
            if (q.DateTo.HasValue) src = src.Where(x => x.Timestamp < q.DateTo.Value); // exclusive upper bound

            if (!string.IsNullOrWhiteSpace(q.Level) && !q.Level.Equals("All", System.StringComparison.OrdinalIgnoreCase))
                src = src.Where(x => x.Level == q.Level);

            if (q.TenantId.HasValue) src = src.Where(x => x.TenantId == q.TenantId);
            if (q.MailboxId.HasValue) src = src.Where(x => x.MailboxId == q.MailboxId);
            if (q.EmailId.HasValue) src = src.Where(x => x.EmailId == q.EmailId);

            if (q.HasException == true) src = src.Where(x => x.Exception != null && x.Exception != "");
            if (q.HasException == false) src = src.Where(x => x.Exception == null || x.Exception == "");

            if (!string.IsNullOrWhiteSpace(q.Text))
            {
                var text = q.Text.Trim();
                src = src.Where(x =>
                    (x.Message != null && EF.Functions.Like(x.Message, $"%{text}%")) ||
                    (x.Exception != null && EF.Functions.Like(x.Exception, $"%{text}%")) ||
                    (x.Properties != null && EF.Functions.Like(x.Properties, $"%{text}%")) ||
                    (x.Source != null && EF.Functions.Like(x.Source, $"%{text}%"))
                );
            }

            src = src.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id);

            var total = await src.CountAsync(ct);

            var page = q.Page <= 0 ? 1 : q.Page;
            var size = q.PageSize <= 0 ? 25 : q.PageSize;

            var items = await src.Skip((page - 1) * size).Take(size).ToListAsync(ct);

            return new PagedResult<AppLog>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = size
            };
        }
    }
}
