using Dog.Application.Features.BreedGroups.DTOs;

namespace Dog.Portal.Models
{
    public class BreedGroupsListViewModel
    {
        public List<BreedGroupDto> BreedGroupList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    // ── AJAX request models ───────────────────────────────────
    public class CreateBreedGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateBreedGroupRequest : CreateBreedGroupRequest
    {
        public int Id { get; set; }
    }

    // ── Breed request models ───────────────────────────────────
    public class CreateBreedRequest
    {
        public int BreedGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}