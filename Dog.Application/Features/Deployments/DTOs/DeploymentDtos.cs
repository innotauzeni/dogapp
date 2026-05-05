namespace Dog.Application.Features.Deployments.DTOs
{
    public class DeploymentDto
    {
        public int Id { get; set; }
        public int DogId { get; set; }
        public int CompanyId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CompanyDeployedTo { get; set; } = string.Empty;
        public string SecurityGuardAssigned { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
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
    }


    public class UpdateDeploymentDto:CreateDeploymentDto
    {
        public int Id { get; set; }
    }






}
