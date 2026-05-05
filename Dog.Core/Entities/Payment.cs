
using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Payment : AuditableEntity
    {
        public int InvoiceId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public decimal Amount { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string Status { get; set; } = "PENDING";
        public string? ReferenceNumber { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }

        // Online payment details
        public string? PaymentGateway { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? GatewayResponse { get; set; }

        // Navigation
        public Invoice Invoice { get; set; } = null!;
    }
}