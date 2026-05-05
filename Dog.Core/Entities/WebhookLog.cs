

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    /// <summary>
    /// Delivery log for each webhook invocation attempt.
    /// </summary>
    public class WebhookLog: BaseEntity
    {
        public int WebhookId { get; set; }
        public Webhook? Webhook { get; set; }
        public string Event { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public bool Success { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public DateTime? NextRetryAt { get; set; }
        public string? ErrorMessage { get; set; }
        public long DurationMs { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
