
using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class InvoiceItem : BaseEntity
    {
        public int InvoiceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal Amount { get; set; }

        public Invoice Invoice { get; set; } = null!;
    }
}