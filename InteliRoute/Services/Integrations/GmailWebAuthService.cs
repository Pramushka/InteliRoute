using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.WebUtilities; // <—
using Microsoft.Extensions.Options;

namespace InteliRoute.Services.Integrations
{
    public sealed class GmailWebAuthService : IGmailWebAuthService
    {
        private readonly GmailOptions _opt;
        private readonly IWebHostEnvironment _env;

        public GmailWebAuthService(IOptions<GmailOptions> opt, IWebHostEnvironment env)
        { 
            _opt = opt.Value;
            _env = env; 
        }

        public Task<Uri> GetConsentUrlAsync(int tenantId, int mailboxId, string email, string redirectAbs, CancellationToken ct)
        {
            var (flow, _, _) = CreateFlow(tenantId, mailboxId, redirectAbs);

            // Build the library’s base URL first
            var req = flow.CreateAuthorizationCodeRequest(redirectAbs);
            req.State = $"{tenantId}:{mailboxId}"; // State IS supported even on older versions
            var baseUri = req.Build();

            // Now replace or insert the params (no duplicates)
            var final = SetQueryParam(baseUri, "access_type", "offline");
            final = SetQueryParam(final, "prompt", "consent");
            final = SetQueryParam(final, "login_hint", email);

            return Task.FromResult(final);
        }

        public async Task CompleteConsentAsync(int tenantId, int mailboxId, string email, string code, string redirectAbs, CancellationToken ct)
        {
            var (flow, _, _) = CreateFlow(tenantId, mailboxId, redirectAbs);
            _ = await flow.ExchangeCodeForTokenAsync(userId: email, code: code, redirectUri: redirectAbs, taskCancellationToken: ct);
        }
        private (GoogleAuthorizationCodeFlow flow, string tokenDir, string redirectUri)
              CreateFlow(int tenantId, int mailboxId, string? overrideRedirect = null)
        {
            var credPathTenant = Path.Combine(_env.ContentRootPath, _opt.CredentialsPathRoot, $"tenant-{tenantId}", "credentials.json");
            var credPathDefault = Path.Combine(_env.ContentRootPath, _opt.CredentialsPathRoot, "default", "credentials.json");
            var credPath = File.Exists(credPathTenant) ? credPathTenant : credPathDefault;

            if (!File.Exists(credPath))
                throw new FileNotFoundException($"Missing Google API credentials at {credPathTenant} or {credPathDefault}");

            // IMPORTANT: this is the directory FileDataStore writes to
            var tokenDir = Path.Combine(_env.ContentRootPath, _opt.TokenStoreRoot, $"tenant-{tenantId}", $"mailbox-{mailboxId}");
            Directory.CreateDirectory(tokenDir);

            using var stream = File.OpenRead(credPath);
            var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = _opt.Scopes,
                DataStore = new FileDataStore(tokenDir, /*fullPath*/ true)
            });

            var redirect = overrideRedirect ?? "/Mailbox/OAuthCallback";
            return (flow, tokenDir, redirect);
        }
        // Replace or add a query parameter exactly once
        private static Uri SetQueryParam(Uri input, string key, string value)
        {
            var baseUrl = input.GetLeftPart(UriPartial.Path);  // scheme://host[:port]/path
            var parsed = QueryHelpers.ParseQuery(input.Query);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // collapse to a single value per key
            foreach (var kv in parsed)
                dict[kv.Key] = kv.Value.LastOrDefault() ?? string.Empty;

            dict[key] = value; // overwrite or add

            var result = new Uri(QueryHelpers.AddQueryString(baseUrl, dict));
            return result;
        }

        public Task<bool> HasTokenAsync(int tenantId, int mailboxId, string address, CancellationToken ct)
        {
            // Use the SAME directory layout as CreateFlow() / FileDataStore
            var tokenDir = Path.Combine(_env.ContentRootPath, _opt.TokenStoreRoot, $"tenant-{tenantId}", $"mailbox-{mailboxId}");

            try
            {
                if (!Directory.Exists(tokenDir))
                    return Task.FromResult(false);

                // FileDataStore uses the key “Google.Apis.Auth.OAuth2.Responses.TokenResponse-{userId}”
                // where userId == the 'email' you passed to ExchangeCodeForTokenAsync.
                var byUserKey = Path.Combine(tokenDir, $"Google.Apis.Auth.OAuth2.Responses.TokenResponse-{address}");
                if (File.Exists(byUserKey))
                    return Task.FromResult(true);

                // Fallback: any file in the token folder indicates we have tokens
                var any = Directory.EnumerateFileSystemEntries(tokenDir).Any();
                return Task.FromResult(any);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
