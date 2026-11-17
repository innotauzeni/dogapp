namespace Dog.Application.Features.Deployments.DTOs
{
    public class DeploymentDto
    {
        public int Id { get; set; }
        public int DogId { get; set; }
        public string? DogName { get; set; }
        public int CompanyId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CompanyDeployedTo { get; set; } = string.Empty;
        public string SecurityGuardAssigned { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? CustomerSiteId { get; set; }
        public string? CustomerSiteName { get; set; }
        public string? CustomerSiteAddress { get; set; }
    }

    public class CreateDeploymentDto
    {
        public int DogId { get; set; }
        public int CompanyId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CompanyDeployedTo { get; set; } = string.Empty;
        public string SecurityGuardAssigned { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CustomerId { get; set; }
        public int? CustomerSiteId { get; set; }
    }


    public class UpdateDeploymentDto:CreateDeploymentDto
    {
        public int Id { get; set; }
    }






}
