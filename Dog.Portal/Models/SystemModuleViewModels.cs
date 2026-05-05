namespace Dog.Portal.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    // ── System Module ────────────────────────────────────────
    public class SystemModuleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccessName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public List<SubmoduleSummaryDto> Children { get; set; } = new();
    }

    public class SubmoduleSummaryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public string? AccessName { get; set; }
        public int Order { get; set; }
    }

    // ── Submodule ────────────────────────────────────────────
    public class SubmoduleDetailDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int ModuleId { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public string? AccessName { get; set; }
        public int Order { get; set; }
    }

    // ── Permission ───────────────────────────────────────────
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }

    // ── View Models ──────────────────────────────────────────
    public class ModulesListViewModel
    {
        public List<SystemModuleDto> Modules { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class ModuleDetailViewModel
    {
        public SystemModuleDto Module { get; set; } = new();
        public List<SubmoduleDetailDto> Submodules { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class SubmodulePermissionsViewModel
    {
        public SubmoduleDetailDto Submodule { get; set; } = new();
        public SystemModuleDto? Module { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }
}
