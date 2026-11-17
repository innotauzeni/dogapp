using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>A physical site/premises belonging to a customer (a customer can have several).</summary>
public class CustomerSite : AuditableEntity
{
    public int CustomerId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
