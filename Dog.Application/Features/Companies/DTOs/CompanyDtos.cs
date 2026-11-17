namespace Dog.Application.Features.Companies.DTOs
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
        public string? Logo { get; set; }
        public string? BrandColor { get; set; }
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
        public string CompanyType => ParentCompanyId == null ? "Head Office" : "Branch";
        public int BranchCount { get; set; }
        public int UserCount { get; set; }
    }

    public class CreateCompanyDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? Logo { get; set; }
        public string? BrandColor { get; set; }
        public string? Location { get; set; }
        public int CountryId { get; set; }
        public int ProvinceId { get; set; }
        public int CityId { get; set; }
        public int DefaultCurrencyId { get; set; }
        public int? ParentCompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCompanyDto : CreateCompanyDto
    {
        public int Id { get; set; }
    }

    public class UpdateBrandColorDto
    {
        /// Hex color, e.g. "#1D4ED8". Pass null to reset to the app default.
        public string? BrandColor { get; set; }
    }
}
