using Dog.Application.Contracts;
using Dog.Core.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Security.Cryptography;
using System.Text;

namespace Dog.Infrastructure.Services
{
    /// <summary>Sends email through the company's own SMTP account (Email Settings). Every attempt is logged in ka_emaillogs.</summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IUnitOfWork _uow;
        private readonly byte[] _key;
        private readonly IConfiguration _config;

        public SmtpEmailService(IUnitOfWork uow, IConfiguration config)
        {
            _uow = uow;
            _config = config;
            // The mail password is encrypted with a key derived from a server secret (Email:EncryptionKey, falling back to the JWT key).
            var secret = config["Email:EncryptionKey"] ?? config["JwtSettings:Key"] ?? "dog-section-email";
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("email|" + secret));
        }

        public string Encrypt(string plain)
        {
            using var aes = Aes.Create();
            aes.Key = _key; aes.GenerateIV();
            using var enc = aes.CreateEncryptor();
            var data = Encoding.UTF8.GetBytes(plain);
            var cipher = enc.TransformFinalBlock(data, 0, data.Length);
            return Convert.ToBase64String(aes.IV.Concat(cipher).ToArray());
        }

        public string? Decrypt(string? cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return null;
            try
            {
                var all = Convert.FromBase64String(cipher);
                using var aes = Aes.Create();
                aes.Key = _key; aes.IV = all[..16];
                using var dec = aes.CreateDecryptor();
                return Encoding.UTF8.GetString(dec.TransformFinalBlock(all, 16, all.Length - 16));
            }
            catch { return null; }
        }

        private static IEnumerable<string> Split(IEnumerable<string> raw)
            => raw.SelectMany(x => (x ?? "").Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Where(x => x.Contains('@')).Distinct(StringComparer.OrdinalIgnoreCase);

        public async Task<EmailResult> SendAsync(EmailMessage m, CancellationToken ct = default)
        {
            var to = Split(m.To).ToList();
            var cc = Split(m.Cc).ToList();
            string? error = null;
            try
            {
                if (to.Count == 0) throw new InvalidOperationException("There is no valid recipient email address.");
                var s = await _uow.Repository<EmailSetting>().TableNoTracking.FirstOrDefaultAsync(x => x.CompanyId == m.CompanyId, ct);
                // No settings saved in the portal: fall back to the Email:Smtp section of appsettings.json.
                if (s == null || string.IsNullOrWhiteSpace(s.Host))
                {
                    var c = _config.GetSection("Email:Smtp");
                    if (!string.IsNullOrWhiteSpace(c["Host"]))
                        s = new EmailSetting
                        {
                            Host = c["Host"]!, Port = int.TryParse(c["Port"], out var pt) ? pt : 465, Security = c["Security"] ?? "SslOnConnect", Username = c["Username"],
                            PasswordEncrypted = string.IsNullOrEmpty(c["Password"]) ? null : Encrypt(c["Password"]!), FromAddress = c["FromAddress"] ?? c["Username"] ?? "",
                            FromName = c["FromName"], ReplyTo = c["ReplyTo"], BccAddress = c["BccAddress"],
                        };
                }
                if (s == null || string.IsNullOrWhiteSpace(s.Host))
                    throw new InvalidOperationException("Email is not set up yet. Go to Configuration → Email Settings and enter your mail server details.");

                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress(s.FromName ?? s.FromAddress, s.FromAddress));
                foreach (var a in to) msg.To.Add(MailboxAddress.Parse(a));
                foreach (var a in cc) msg.Cc.Add(MailboxAddress.Parse(a));
                if (!string.IsNullOrWhiteSpace(s.BccAddress)) foreach (var a in Split(new[] { s.BccAddress })) msg.Bcc.Add(MailboxAddress.Parse(a));
                if (!string.IsNullOrWhiteSpace(s.ReplyTo)) msg.ReplyTo.Add(MailboxAddress.Parse(s.ReplyTo.Trim()));
                msg.Subject = m.Subject;

                var html = m.HtmlBody;
                if (!string.IsNullOrWhiteSpace(s.Signature)) html += "<br><br>" + System.Net.WebUtility.HtmlEncode(s.Signature).Replace("\n", "<br>");
                var body = new BodyBuilder { HtmlBody = html };
                foreach (var a in m.Attachments) body.Attachments.Add(a.FileName, a.Content, ContentType.Parse(a.ContentType));
                msg.Body = body.ToMessageBody();

                var opt = s.Security switch { "SslOnConnect" => SecureSocketOptions.SslOnConnect, "None" => SecureSocketOptions.None, _ => SecureSocketOptions.StartTls };
                using var client = new SmtpClient { Timeout = 20000 };
                await client.ConnectAsync(s.Host, s.Port, opt, ct);
                if (!string.IsNullOrWhiteSpace(s.Username))
                    await client.AuthenticateAsync(s.Username, Decrypt(s.PasswordEncrypted) ?? "", ct);
                await client.SendAsync(msg, ct);
                await client.DisconnectAsync(true, ct);
            }
            catch (Exception ex) { error = ex.Message; }

            try
            {
                await _uow.Repository<EmailLog>().AddAsync(new EmailLog
                {
                    CompanyId = m.CompanyId, ToAddresses = string.Join("; ", to), CcAddresses = cc.Count > 0 ? string.Join("; ", cc) : null,
                    Subject = m.Subject, RelatedType = m.RelatedType, RelatedId = m.RelatedId, Success = error == null,
                    Error = error != null && error.Length > 900 ? error[..900] : error, SentByUserId = m.SentByUserId, SentByName = m.SentByName,
                    HasAttachment = m.Attachments.Count > 0,
                }, ct);
            }
            catch { /* the log must never hide the real result */ }
            return new EmailResult(error == null, error);
        }

        public Task<EmailResult> TestAsync(int companyId, string toAddress, string? sentByName, CancellationToken ct = default)
            => SendAsync(new EmailMessage
            {
                CompanyId = companyId, To = { toAddress }, Subject = "Test email from your Dog Management portal",
                HtmlBody = "<p>This is a test message. If you can read it, your email settings work and invoices can be sent from the portal.</p>",
                RelatedType = "Test", SentByName = sentByName,
            }, ct);
    }
}
