using Dog.Core.Common;

namespace Dog.Core.Entities;

public class Deployment : AuditableEntity
{
    public int DogId { get; set; }
    // Legacy free-text fields — kept so existing data/UI keep working. New code
    // should prefer CustomerId/CustomerSiteId, which structurally link a
    // deployment to who and where a dog actually is.
    public string Location { get; set; } = string.Empty;
    public string CompanyDeployedTo { get; set; } = string.Empty;
    public string SecurityGuardAssigned { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }

    public int? CustomerId { get; set; }
    public int? CustomerSiteId { get; set; }

    // Navigation
    public virtual Dogtbl Dog { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual CustomerSite? CustomerSite { get; set; }
}
