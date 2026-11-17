namespace Dog.Application.Features.CustomerSites.DTOs
{
    public class CustomerSiteDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCustomerSiteDto
    {
        public int CustomerId { get; set; }
        public int CompanyId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCustomerSiteDto : CreateCustomerSiteDto
    {
        public int Id { get; set; }
    }
}
