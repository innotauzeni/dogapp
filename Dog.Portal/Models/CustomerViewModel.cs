using Dog.Application.Features.Customers.DTOs;
using Dog.Application.Features.CustomerContracts.DTOs;
using Dog.Application.Features.CustomerSites.DTOs;
using Dog.Application.Features.Deployments.DTOs;

namespace Dog.Portal.Models
{
    // ── Customer ViewModels ──────────────────────────────────────────────

    public class CustomerListViewModel
    {
        public List<CustomerDto> CustomerList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    /// <summary>Full customer profile page: contact details plus everything
    /// related to them — sites/premises, dogs they own, and dogs currently
    /// (or previously) deployed to one of their premises.</summary>
    public class CustomerDetailsViewModel
    {
        public CustomerDto Customer { get; set; } = null!;
        public List<CustomerSiteDto> Sites { get; set; } = new();
        public List<CustomerContractDto> Contracts { get; set; } = new();
        public List<Dog.Application.Features.Animals.DTOs.AnimalListDto> OwnedDogs { get; set; } = new();
        public List<DeploymentDto> Deployments { get; set; } = new();
    }

    public class CreateCustomerRequest
    {
        public string? Profile { get; set; }
        public string? Gender { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Dob { get; set; }
        public string? IDnumber { get; set; }
        public string? Cellphone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public string? JobTitle { get; set; }
        public int? CustomerCompanyId { get; set; }
        public string? ContractDate { get; set; }
        public string? ContractDetails { get; set; }
    }

    public class UpdateCustomerRequest : CreateCustomerRequest
    {
        public int Id { get; set; }
    }

    public class CustomerSiteRequest
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
    }

    public class CustomerContractRequest
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime ContractDate { get; set; }
        public string? ContractDetails { get; set; }
    }
}