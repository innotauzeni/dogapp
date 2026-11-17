namespace Dog.Application.Contracts
{
    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    public class EmailMessage
    {
        public int CompanyId { get; set; }
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public List<EmailAttachment> Attachments { get; set; } = new();
        public string? RelatedType { get; set; }
        public int? RelatedId { get; set; }
        public string? SentByUserId { get; set; }
        public string? SentByName { get; set; }
    }

    public record EmailResult(bool Success, string? Error);

    /// <summary>Sends mail through the company's own SMTP account (Email Settings) and keeps an EmailLog row for every attempt.</summary>
    public interface IEmailService
    {
        Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct = default);
        /// <summary>Sends a test message with the given settings (nothing is saved except the log).</summary>
        Task<EmailResult> TestAsync(int companyId, string toAddress, string? sentByName, CancellationToken ct = default);
        string Encrypt(string plain);
        string? Decrypt(string? cipher);
    }
}
