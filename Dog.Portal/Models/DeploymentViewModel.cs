using Dog.Application.Features.Customers.DTOs;
using Dog.Application.Features.Deployments.DTOs;

namespace Dog.Portal.Models
{
    public class DeploymentListViewModel
    {
        public List<DeploymentDto> DeploymentList { get; set; } = new();
        public List<DogDropdownItem> Dogs { get; set; } = new();
        public List<CustomerDto> Customers { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class DogDropdownItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? MicrochipNumber { get; set; }
    }

    public class AnimalPageResult
    {
        public List<DogDropdownItem> Data { get; set; } = new();
        public int Total { get; set; }
    }

    public class CreateDeploymentRequest
    {
        public int DogId { get; set; }
        public string? Location { get; set; }
        public string? CompanyDeployedTo { get; set; }
        public string? SecurityGuardAssigned { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CustomerId { get; set; }
        public int? CustomerSiteId { get; set; }
    }

    public class UpdateDeploymentRequest : CreateDeploymentRequest
    {
        public int Id { get; set; }
    }
}
