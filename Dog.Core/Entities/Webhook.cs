

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    /// <summary>
    /// A registered webhook endpoint that receives event notifications.
    /// </summary>
    public class Webhook : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Secret { get; set; }          // HMAC signing secret
        public string Events { get; set; } = string.Empty; // comma-separated: "shipment.created,invoice.paid"
        public string? Description { get; set; }
        public int RetryCount { get; set; } = 3;
        public ICollection<WebhookLog> Logs { get; set; } = new List<WebhookLog>();
    }
}
