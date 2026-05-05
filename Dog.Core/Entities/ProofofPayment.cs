
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class ProofofPayment : AuditableEntity
    {
        public int InvoiceId { get; set; }
        public string? file { get; set; }
        public string? Status { get; set; } = "PENDING";

        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }
    }
}