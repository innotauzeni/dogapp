namespace Dog.Portal.Models
{
    // ── AJAX request body for the signup form ─────────────────
    public class OnboardSubmitRequest
    {
        public OnboardCompanyPayload Company { get; set; } = new();
        public OnboardAdminPayload   Admin   { get; set; } = new();
    }

    public class OnboardCompanyPayload
    {
        public string  CompanyName       { get; set; } = string.Empty;
        public string  CompanyCode       { get; set; } = string.Empty;
        public string  EmailAddress      { get; set; } = string.Empty;
        public string  PhoneNumber       { get; set; } = string.Empty;
        public string? Website           { get; set; }
        public string? Address           { get; set; }
        public int     CountryId         { get; set; }
        public int     ProvinceId        { get; set; }
        public int     CityId            { get; set; }
        public int     DefaultCurrencyId { get; set; }
    }

    public class OnboardAdminPayload
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName  { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string Password  { get; set; } = string.Empty;
    }

    public class OnboardResultDto
    {
        public int    CompanyId   { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string AdminEmail  { get; set; } = string.Empty;
    }
}
