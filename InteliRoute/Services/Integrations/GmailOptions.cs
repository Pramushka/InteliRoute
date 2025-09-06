// Services/Integrations/GmailOptions.cs
using Google.Apis.Gmail.v1;

namespace InteliRoute.Services.Integrations
{
    public class GmailOptions
    {
        public string ApplicationName { get; set; } = "InteliRoute";
        public string[] Scopes { get; set; } = new[] { GmailService.Scope.GmailReadonly };
        public string CredentialsPathRoot { get; set; } = "secrets/credentials";
        public string TokenStoreRoot { get; set; } = "secrets/tokens";
    }
}
