using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace InteliRoute.Services.Mail;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "no-reply@intelliroute.local";
    public string FromName { get; set; } = "InteliRoute Router";
}

public interface IMailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}

public sealed class SmtpMailSender : IMailSender
{
    private readonly SmtpOptions _opt;

    public SmtpMailSender(IOptions<SmtpOptions> opt) => _opt = opt.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        using var msg = new MailMessage
        {
            From = new MailAddress(_opt.FromAddress, _opt.FromName),
            Subject = subject ?? "(no subject)",
            Body = body ?? "",
            IsBodyHtml = false
        };
        msg.To.Add(to);

        using var client = new SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.UseStartTls,
            Credentials = string.IsNullOrWhiteSpace(_opt.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_opt.Username, _opt.Password)
        };

        await client.SendMailAsync(msg, ct);
    }
}
