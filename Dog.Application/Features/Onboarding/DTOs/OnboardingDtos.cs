namespace Dog.Application.Features.Onboarding.DTOs
{
    public class OnboardRequestDto
    {
        public OnboardCompanyDto Company { get; set; } = new();
        public OnboardBranchDto? MainBranch { get; set; }
        public OnboardAdminDto Admin { get; set; } = new();
    }

    public class OnboardCompanyDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? Address { get; set; }
        public int CountryId { get; set; }
        public int ProvinceId { get; set; }
        public int CityId { get; set; }
        public int DefaultCurrencyId { get; set; }
    }

    public class OnboardBranchDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailAddress { get; set; }
    }

    public class OnboardAdminDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class OnboardResultDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
    }
}
