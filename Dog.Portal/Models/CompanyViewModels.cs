namespace Dog.Portal.Models
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyCode { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? Location { get; set; }
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
        public int ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
        public int CityId { get; set; }
        public string? CityName { get; set; }
        public int DefaultCurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public int? ParentCompanyId { get; set; }
        public string? ParentCompanyName { get; set; }
        public bool IsActive { get; set; }
        public string? CompanyType { get; set; }
        public int BranchCount { get; set; }
        public int UserCount { get; set; }
    }

    // ── Lightweight dropdown options ──────────────────────────
    public class CountryOption
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
    }

    public class ProvinceOption
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CountryId { get; set; }
    }

    public class CityOption
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int ProvinceId { get; set; }
    }

    public class CurrencyOption
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
    }

    // ── AJAX request models ───────────────────────────────────
    public class CreateCompanyRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? Location { get; set; }
        public int CountryId { get; set; }
        public int ProvinceId { get; set; }
        public int CityId { get; set; }
        public int DefaultCurrencyId { get; set; }
        public int? ParentCompanyId { get; set; }
        public bool IsActive { get; set; } = true;

        // Optional — when creating a head office, the system owner can also create
        // the company's first user (Admin). If AdminEmail is filled, we create the user.
        public string? AdminFirstName { get; set; }
        public string? AdminLastName  { get; set; }
        public string? AdminEmail     { get; set; }
        public string? AdminPhone     { get; set; }
        public string? AdminPassword  { get; set; }
    }

    public class UpdateCompanyRequest : CreateCompanyRequest
    {
        public int Id { get; set; }
    }

    // ── View Models ───────────────────────────────────────────
    public class CompaniesListViewModel
    {
        public List<CompanyDto> Companies { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string FilterType { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);

        // For the "New" modal dropdowns (preloaded)
        public List<CountryOption> Countries { get; set; } = new();
        public List<CurrencyOption> Currencies { get; set; } = new();
        public List<CompanyDto> HeadOffices { get; set; } = new();

        // Summary counts
        public int HeadOfficeCount { get; set; }
        public int BranchCount { get; set; }
    }

    public class CompanyDetailViewModel
    {
        public CompanyDto Company { get; set; } = new();
        public List<CompanyDto> Branches { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }
}
