

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Country : AuditableBaseEntity  // Global lookup with audit - no InstitutionId
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public List<Province>? Provinces { get; set; }
    }
}