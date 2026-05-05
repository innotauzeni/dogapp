
using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Invoice : AuditableEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        //public int CompanyId { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = "DRAFT";
        public PaymentMethod? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }

        // Navigation
        //public Company Company { get; set; } = null!;
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}