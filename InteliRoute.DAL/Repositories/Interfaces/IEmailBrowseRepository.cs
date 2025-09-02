using InteliRoute.DAL.Entities;

namespace InteliRoute.DAL.Repositories.Interfaces;

public interface IEmailBrowseRepository
{
    Task<(IReadOnlyList<EmailItem> Items, int Total)> GetPagedAsync(
        int tenantId,
        int page,
        int pageSize,
        string? q,
        RouteStatus? status,
        int? mailboxId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);

    Task<EmailItem?> GetByIdAsync(int tenantId, int id, CancellationToken ct = default);
}
