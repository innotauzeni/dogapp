
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class City : AuditableBaseEntity  // Global lookup with audit - no InstitutionId
    {
        public string? Name { get; set; }
        public int ProvinceId { get; set; }

        [ForeignKey(nameof(ProvinceId))]
        public Province? Province { get; set; }
    }


}