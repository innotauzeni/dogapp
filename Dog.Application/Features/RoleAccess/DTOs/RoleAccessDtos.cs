namespace Dog.Application.Features.RoleAccess.DTOs
{
    public class AssignMenuToRoleDto
    {
        public string RoleId { get; set; } = string.Empty;
        public List<int> ModuleIds { get; set; } = new();
    }

    public class AssignPermissionsToRoleDto
    {
        public string RoleId { get; set; } = string.Empty;
        public List<int> PermissionIds { get; set; } = new();
    }

    public class RoleMenusDto
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<int> AssignedModuleIds { get; set; } = new();
    }

    public class RolePermissionsDto
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<int> AssignedPermissionIds { get; set; } = new();
    }
}
