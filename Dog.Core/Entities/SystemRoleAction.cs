
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class SystemRoleAction : AuditableBaseEntity
    {
        public string RoleId { get; set; } = string.Empty;
        public int SystemActionId { get; set; }

        [ForeignKey(nameof(SystemActionId))]
        public SystemAction? SystemAction { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }
    }
}
