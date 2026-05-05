
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class ExchangeRate : AuditableEntity
    {
        public int PrimaryCurrencyId { get; set; }
        public int SecondaryCurrencyId { get; set; }
        //public bool Status { get; set; } = true;
        public decimal Rate { get; set; }

        [ForeignKey(nameof(PrimaryCurrencyId))]
        public Currency? PrimaryCurrency { get; set; }

        [ForeignKey(nameof(SecondaryCurrencyId))]
        public Currency? SecondaryCurrency { get; set; }

    }
}