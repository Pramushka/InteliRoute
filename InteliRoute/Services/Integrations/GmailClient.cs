// Services/Integrations/GmailClient.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using InteliRoute.DAL.Entities;
using InteliRoute.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InteliRoute.Services.Integrations
{
    public class GmailClient : IGmailClient
    {
        private readonly GmailOptions _opt;
        private readonly IEmailRepository _emails;
        private readonly IMailboxRepository _mailboxes;
        private readonly ILogger<GmailClient> _log;
        private readonly IWebHostEnvironment _env;

        public GmailClient(
            IOptions<GmailOptions> opt,
            IEmailRepository emails,
            IMailboxRepository mailboxes,
            ILogger<GmailClient> log,
            IWebHostEnvironment env)
        {
            _opt = opt.Value;
            _emails = emails;
            _mailboxes = mailboxes;
            _log = log;
            _env = env;
        }

        public async Task<int> FetchNewAsync(Mailbox mailbox, CancellationToken ct = default)
        {
            // Build absolute roots
            var root = _env.ContentRootPath;
            var credsRoot = Path.Combine(root, _opt.CredentialsPathRoot);
            var tokensRoot = Path.Combine(root, _opt.TokenStoreRoot);

            // Prefer tenant creds, fall back to default
            var credPathTenant = Path.Combine(credsRoot, $"tenant-{mailbox.TenantId}", "credentials.json");
            var credPathDefault = Path.Combine(credsRoot, "default", "credentials.json");
            var credPath = File.Exists(credPathTenant) ? credPathTenant : credPathDefault;

            if (!File.Exists(credPath))
                throw new FileNotFoundException($"Missing Google API credentials at {credPathTenant} or {credPathDefault}");

            var tokenDir = Path.Combine(tokensRoot, $"tenant-{mailbox.TenantId}", $"mailbox-{mailbox.Id}");
            Directory.CreateDirectory(Path.GetDirectoryName(credPath)!);
            Directory.CreateDirectory(tokenDir);

            // Authorize using mailbox address as user key (must match web auth)
            using var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read);
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                _opt.Scopes,
                mailbox.Address,
                CancellationToken.None,
                new FileDataStore(tokenDir, true));

            using var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _opt.ApplicationName
            });

            // First run: prime history cursor and stop
            if (mailbox.GmailHistoryId is null)
            {
                var profile = await service.Users.GetProfile("me").ExecuteAsync(ct);
                mailbox.GmailHistoryId = profile.HistoryId.HasValue ? (long)profile.HistoryId.Value : null;
                mailbox.LastSyncUtc = DateTime.UtcNow;
                await _mailboxes.UpdateAsync(mailbox, ct);
                _log.LogInformation("Initialized history for {Mailbox}", mailbox.Address);
                return 0;
            }

            // Pull history since last
            var listReq = service.Users.History.List("me");
            listReq.StartHistoryId = (ulong)mailbox.GmailHistoryId.Value;
            listReq.HistoryTypes = UsersResource.HistoryResource.ListRequest.HistoryTypesEnum.MessageAdded;

            var newEmails = new List<EmailItem>();
            string? pageToken = null;
            ulong? maxSeenHistoryId = null;

            do
            {
                listReq.PageToken = pageToken;
                ListHistoryResponse? history;
                try
                {
                    history = await listReq.ExecuteAsync(ct);
                }
                catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
                {
                    // history expired -> reset and stop
                    var profile = await service.Users.GetProfile("me").ExecuteAsync(ct);
                    mailbox.GmailHistoryId = profile.HistoryId.HasValue ? (long)profile.HistoryId.Value : null;
                    mailbox.LastSyncUtc = DateTime.UtcNow;
                    await _mailboxes.UpdateAsync(mailbox, ct);
                    _log.LogWarning("History expired for {Mailbox}. Reset to {HistoryId}", mailbox.Address, mailbox.GmailHistoryId);
                    return 0;
                }

                if (history?.History != null)
                {
                    foreach (var record in history.History)
                    {
                        if (record.MessagesAdded == null) continue;

                        foreach (var added in record.MessagesAdded)
                        {
                            var msgId = added.Message.Id;
                            if (await _emails.ExistsAsync(mailbox.Id, msgId, ct)) continue;

                            var msg = await service.Users.Messages.Get("me", msgId).ExecuteAsync(ct);
                            var h = msg.Payload.Headers;

                            string from = h.FirstOrDefault(x => x.Name == "From")?.Value ?? "";
                            string to = h.FirstOrDefault(x => x.Name == "To")?.Value ?? "";
                            string subject = h.FirstOrDefault(x => x.Name == "Subject")?.Value ?? "";
                            DateTime receivedUtc = ParseDate(h.FirstOrDefault(x => x.Name == "Date")?.Value);

                            var email = new EmailItem
                            {
                                TenantId = mailbox.TenantId,
                                MailboxId = mailbox.Id,
                                ExternalMessageId = msg.Id,
                                ThreadId = msg.ThreadId,
                                From = from,
                                To = to,
                                Subject = subject,
                                Snippet = msg.Snippet,
                                BodyText = ExtractPlainText(msg.Payload),
                                ReceivedUtc = receivedUtc == default ? DateTime.UtcNow : receivedUtc,
                                RouteStatus = RouteStatus.New,
                                CreatedUtc = DateTime.UtcNow
                            };
                            newEmails.Add(email);
                        }
                    }
                }

                if (history?.HistoryId != null)
                    maxSeenHistoryId = history.HistoryId;

                pageToken = history?.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            if (newEmails.Count > 0)
                await _emails.AddRangeAsync(newEmails, ct);

            // Advance cursor and touch sync time
            if (maxSeenHistoryId.HasValue)
                mailbox.GmailHistoryId = (long)maxSeenHistoryId.Value;

            mailbox.LastSyncUtc = DateTime.UtcNow;
            await _mailboxes.UpdateAsync(mailbox, ct);

            _log.LogInformation("Fetched {Count} emails for {Mailbox}", newEmails.Count, mailbox.Address);
            return newEmails.Count;
        }

        // --- helpers ---
        private static DateTime ParseDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return default;
            return DateTimeOffset.TryParse(raw, out var dto) ? dto.UtcDateTime : default;
        }

        private static string ExtractPlainText(MessagePart payload)
        {
            try
            {
                var plain = FindPartByMimeType(payload, "text/plain");
                if (plain?.Body?.Data != null) return DecodeBase64(plain.Body.Data);

                var html = FindPartByMimeType(payload, "text/html");
                if (html?.Body?.Data != null) return "[HTML]\n" + DecodeBase64(html.Body.Data);

                if (payload.Body?.Data != null) return DecodeBase64(payload.Body.Data);
                return "(No readable body found)";
            }
            catch { return "(Error decoding body)"; }
        }

        private static MessagePart? FindPartByMimeType(MessagePart part, string mimeType)
        {
            if (part.MimeType == mimeType) return part;
            if (part.Parts != null)
            {
                foreach (var p in part.Parts)
                {
                    var found = FindPartByMimeType(p, mimeType);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static string DecodeBase64(string data)
        {
            var fixedData = data.Replace("-", "+").Replace("_", "/");
            var bytes = Convert.FromBase64String(fixedData);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
