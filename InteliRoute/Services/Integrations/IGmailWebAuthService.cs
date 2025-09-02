namespace InteliRoute.Services.Integrations
{
    public interface IGmailWebAuthService
    {
        // Build the consent URL (absolute redirect) and we auto-embed state=tenantId:mailboxId
        Task<Uri> GetConsentUrlAsync(int tenantId, int mailboxId, string email, string redirectAbs, CancellationToken ct);

        // Exchange the code for tokens and save them under secrets/tokens/tenant-{id}/mailbox-{id}/
        Task CompleteConsentAsync(int tenantId, int mailboxId, string email, string code, string redirectAbs, CancellationToken ct);
    }
}
