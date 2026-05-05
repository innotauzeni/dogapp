using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class Receipt : AuditableEntity
    {
        public string? ReceiptNumber { get; set; }
        public int CustomerId { get; set; }
        public int InvoiceId { get; set; }
        public int CurrencyId { get; set; }
        public int SuspenseId { get; set; }
        public int ExchangeRateId { get; set; }
        public int SourceId { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; } = "PENDING";

        [ForeignKey(nameof(CurrencyId))]
        public Currency? Currency { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [ForeignKey(nameof(SuspenseId))]
        public Suspense? Suspense { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }


        [ForeignKey(nameof(ExchangeRateId))]
        public ExchangeRate? ExchangeRate { get; set; }




    }

}
