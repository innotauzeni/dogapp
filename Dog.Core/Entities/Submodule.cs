
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class Submodule : AuditableBaseEntity  // Global system submodule - no InstitutionId
    {
        [Required]
        public string? Name { get; set; }

        public int ModuleId { get; set; }

        [Required]
        public string? Icon { get; set; }

        [Required]
        public string? Route { get; set; }

        [Required]
        public string? AccessName { get; set; }
        public int Order { get; set; }

        //public List<Permission>? Permissions { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public SystemModule? module { get; set; }
    }
}
