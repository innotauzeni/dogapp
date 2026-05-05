
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class RoleMenu : AuditableBaseEntity
    {
        /// <summary>String FK matching IdentityRole.Id (GUID string)</summary>
        public string RoleId { get; set; } = string.Empty;

        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;

        public int SystemModuleId { get; set; }

        [ForeignKey(nameof(SystemModuleId))]
        public SystemModule Menu { get; set; } = null!;
    }
}
