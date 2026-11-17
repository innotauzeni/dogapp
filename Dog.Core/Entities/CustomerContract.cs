using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>A signed contract/agreement with a customer (a customer can have several, e.g. renewals).</summary>
public class CustomerContract : AuditableEntity
{
    public int CustomerId { get; set; }
    public DateTime ContractDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ContractDetails { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
