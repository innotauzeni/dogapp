namespace Dog.Application.Features.Webhooks.DTOs
{
    public class WebhookDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Events { get; set; } = string.Empty;
        public List<string> EventList => Events.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList();
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public int RetryCount { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
    }

    public class CreateWebhookDto
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Secret { get; set; }
        public List<string> Events { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public int RetryCount { get; set; } = 3;
        public int CompanyId { get; set; }
    }

    public class UpdateWebhookDto : CreateWebhookDto
    {
        public int Id { get; set; }
    }

    public class WebhookLogDto
    {
        public int Id { get; set; }
        public int WebhookId { get; set; }
        public string? WebhookName { get; set; }
        public string Event { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public bool Success { get; set; }
        public int AttemptNumber { get; set; }
        public string? ErrorMessage { get; set; }
        public long DurationMs { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WebhookDeliverDto
    {
        public int WebhookId { get; set; }
        public string Event { get; set; } = string.Empty;
        /// <summary>JSON string payload — serialize your object before assigning.</summary>
        public string Payload { get; set; } = "{}";
    }

    // Available system events
    public static class WebhookEvents
    {
        public const string ShipmentCreated   = "shipment.created";
        public const string ShipmentDelivered = "shipment.delivered";
        public const string ShipmentCancelled = "shipment.cancelled";
        public const string InvoiceCreated    = "invoice.created";
        public const string InvoicePaid       = "invoice.paid";
        public const string CustomerCreated   = "customer.created";
        public const string UserCreated       = "user.created";
        public const string CompanyCreated    = "company.created";

        public static readonly string[] All =
        {
            ShipmentCreated, ShipmentDelivered, ShipmentCancelled,
            InvoiceCreated, InvoicePaid,
            CustomerCreated, UserCreated, CompanyCreated,
        };
    }
}
