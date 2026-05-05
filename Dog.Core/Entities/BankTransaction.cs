
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class BankTransaction : AuditableEntity
    {
        public string? Name { get; set; }
        public int BankId { get; set; }
        public int CustomerId { get; set; }
        public int CurrencyId { get; set; }
        public int ProofofPaymentId { get; set; }
        public string? StatementReference { get; set; }
        public string? SourceReference { get; set; }
        public string? Description { get; set; }
        public string? Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string Status { get; set; } = "PENDING";

        [ForeignKey(nameof(BankId))]
        public Bank? Bank { get; set; }

        [ForeignKey(nameof(CurrencyId))]
        public Currency? Currency { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [ForeignKey(nameof(ProofofPaymentId))]
        public ProofofPayment? ProofofPayment { get; set; }
    }

}