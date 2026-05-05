namespace Dog.Portal.Models
{
    public class SystemActionsListViewModel
    {
        public List<SystemActionItemDto> SystemActionList { get; set; } = new();
        public List<RoleItemDto> RolesList { get; set; } = new();
        public List<SystemRoleActionItemDto> RoleActionsList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    // ── DTOs matching API responses ───────────────────────────────────
    public class SystemActionItemDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
    }

    public class SystemRoleActionItemDto
    {
        public int Id { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public string? RoleName { get; set; }
        public int SystemActionId { get; set; }
        public string? ActionName { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
    }

    public class RoleItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    // ── AJAX request models ───────────────────────────────────
    public class CreateSystemActionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class UpdateSystemActionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class AssignRoleActionRequest
    {
        public string RoleId { get; set; } = string.Empty;
        public int SystemActionId { get; set; }
    }
}