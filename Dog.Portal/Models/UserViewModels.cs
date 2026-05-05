namespace Dog.Portal.Models
{
    // ── User DTOs ─────────────────────────────────────────────
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Status { get; set; }
        public string? RoleName { get; set; }
        public int CompanyId { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public string FullName => $"{Name} {Surname}".Trim();
        public string Initials
        {
            get
            {
                var n = Name?.Length > 0 ? Name[0].ToString().ToUpper() : "";
                var s = Surname?.Length > 0 ? Surname[0].ToString().ToUpper() : "";
                return (n + s).Length > 0 ? n + s : "U";
            }
        }
    }

    public class CompanyDropdownDto
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public int? ParentCompanyId { get; set; }
        public string CompanyType => ParentCompanyId == null ? "Head Office" : "Branch";
    }

    // ── AJAX request models ───────────────────────────────────
    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Status { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangeRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
    }

    public class ChangeCompanyRequest
    {
        public int CompanyId { get; set; }
    }

    // ── View Models ───────────────────────────────────────────
    public class UsersListViewModel
    {
        public List<UserDto> Users { get; set; } = new();
        public List<RoleDto> AllRoles { get; set; } = new();
        public List<CompanyDropdownDto> AllCompanies { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? RoleFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }
}
