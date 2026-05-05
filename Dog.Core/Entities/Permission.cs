

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Permission: AuditableBaseEntity
    {
        public string Name { get; set; } = string.Empty;       // e.g. "events.create"
        public string DisplayName { get; set; } = string.Empty; // e.g. "Create Events"
        public string Module { get; set; } = string.Empty;      // e.g. "Events"
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}