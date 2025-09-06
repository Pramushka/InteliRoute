namespace InteliRoute.Services.Security
{
    public sealed class BootstrapOptions
    {
        public bool Enabled { get; set; }
        public string TenantName { get; set; } = "System";
        public string? DomainsCsv { get; set; }
        public string Username { get; set; } = "super";
        public string Email { get; set; } = "super@inteli.local";
        public string Password { get; set; } = "change-me";
    }

}
