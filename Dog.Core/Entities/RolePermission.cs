
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class RolePermission : AuditableBaseEntity
    {
        /// <summary>String FK matching IdentityRole.Id (GUID string)</summary>
        public string RoleId { get; set; } = string.Empty;

        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;

        public int PermissionId { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public Permission Permission { get; set; } = null!;
    }
}
