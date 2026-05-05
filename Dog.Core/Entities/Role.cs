using Microsoft.AspNetCore.Identity;

namespace Dog.Core.Entities
{
    public class Role : IdentityRole
    {
        public string? Type { get; set; }
        public string? Level { get; set; }
        public ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    

}
