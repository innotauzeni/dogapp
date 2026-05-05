using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class Suspense : AuditableEntity
    {
        public int CustomerId { get; set; }
        public int CurrencyId { get; set; }
        public int SourceId { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; } = "PENDING";

        [ForeignKey(nameof(CurrencyId))]
        public Currency? Currency { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }
    }

}
