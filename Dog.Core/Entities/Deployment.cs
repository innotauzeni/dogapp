using Dog.Core.Common;

namespace Dog.Core.Entities;

public class Deployment : AuditableEntity
{
    public int DogId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string CompanyDeployedTo { get; set; } = string.Empty;
    public string SecurityGuardAssigned { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Dogtbl Dog { get; set; } = null!;
}
