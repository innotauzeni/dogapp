using Dog.Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Common
{
    public abstract class AuditableEntity : AuditableBaseEntity
    {
        public int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company? Company { get; set; }
    }
}
