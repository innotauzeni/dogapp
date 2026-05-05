

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class SystemModule : AuditableBaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccessName { get; set; }
        public ICollection<Submodule> childrens { get; set; } = new List<Submodule>();
        public int SortOrder { get; set; }
        //public bool IsActive { get; set; } = true;
        public ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
    }

}
