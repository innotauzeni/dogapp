
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

        // Billed company (a snapshot, so the invoice never changes if the company record is edited later)
        public int? CustomerCompanyId { get; set; }
        public string? BillToName { get; set; }
        public string? BillToAddress { get; set; }
        public string? BillToTaxNumber { get; set; }
        public string? BillToVatNumber { get; set; }
        public string? BillToEmail { get; set; }
        public string? BillToPhone { get; set; }
        public string? ContactPerson { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string CurrencyCode { get; set; } = "USD";

        // Fiscal invoice details (recorded from the fiscal device; real-time fiscal device integration is not built in)
        public bool IsFiscal { get; set; }
        public string? FiscalInvoiceNumber { get; set; }
        public string? FiscalDeviceSerial { get; set; }
        public string? FiscalVerificationCode { get; set; }
        public DateTime? FiscalDate { get; set; }

        public DateTime? EmailedAt { get; set; }
        public string? EmailedTo { get; set; }

        // Navigation
        //public Company Company { get; set; } = null!;
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        /// <summary>The payment ledger (how each payment was made, by whom, and where the money came from).</summary>
        public ICollection<InvoicePayment> Ledger { get; set; } = new List<InvoicePayment>();
    }
}