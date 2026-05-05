
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class Province : AuditableBaseEntity  // Global lookup with audit - no InstitutionId
    {
        public int CountryId { get; set; }
        public string? Name { get; set; }

        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }
        public List<City>? Cities { get; set; }
    }
}