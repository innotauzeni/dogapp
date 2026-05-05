
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class BankAccount : AuditableEntity
    {
        public string? Name { get; set; }
        public int BankId { get; set; }
        public int CurrencyId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }
        public string? SwiftCode { get; set; }
        public string? AccountNumber { get; set; }


        [ForeignKey(nameof(BankId))]
        public Bank? Bank { get; set; }

        [ForeignKey(nameof(CurrencyId))]
        public Currency? Currency { get; set; }
    }

}