using InteliRoute.DAL.Context;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteliRoute.DAL.Repositories.Implementations;

public class EmailRepository : IEmailRepository
{
    private readonly AppDbContext _db;
    public EmailRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(int mailboxId, string externalMessageId, CancellationToken ct = default)
        => _db.Emails.AsNoTracking()
            .AnyAsync(e => e.MailboxId == mailboxId && e.ExternalMessageId == externalMessageId, ct);

    public async Task<int> AddAsync(EmailItem email, CancellationToken ct = default)
    {
        _db.Emails.Add(email);
        await _db.SaveChangesAsync(ct);
        return email.Id;
    }

    public async Task AddRangeAsync(IEnumerable<EmailItem> emails, CancellationToken ct = default)
    {
        _db.Emails.AddRange(emails);
        await _db.SaveChangesAsync(ct);
    }
}
