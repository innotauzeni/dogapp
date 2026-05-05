namespace Dog.Portal.Models
{
    // ── Role DTOs ────────────────────────────────────────────
    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Level { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    // ── Role Access DTOs ─────────────────────────────────────
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

    // ── AJAX request models for portal proxy endpoints ───────
    public class SavePermissionsRequest
    {
        public string RoleId { get; set; } = string.Empty;
        public List<int> PermissionIds { get; set; } = new();
    }

    public class SaveMenusRequest
    {
        public string RoleId { get; set; } = string.Empty;
        public List<int> ModuleIds { get; set; } = new();
    }

    // ── View Models ──────────────────────────────────────────
    public class RolesListViewModel
    {
        public List<RoleDto> Roles { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class PermissionGroupModel
    {
        public string Module { get; set; } = string.Empty;
        public List<PermissionDto> Permissions { get; set; } = new();
    }

    public class RoleAssignViewModel
    {
        public RoleDto Role { get; set; } = new();
        public List<PermissionGroupModel> PermissionGroups { get; set; } = new();
        public List<SystemModuleDto> AllModules { get; set; } = new();
        public List<int> AssignedPermissionIds { get; set; } = new();
        public List<int> AssignedModuleIds { get; set; } = new();
    }
}
