using Dog.Core.Common;

namespace Dog.Core.Entities;

public class Litter : AuditableEntity
{
    public int MatingRecordId { get; set; }
    public DateTime BirthDate { get; set; }
    public int TotalBorn { get; set; }
    public int SurvivedCount { get; set; }
    public int DiedCount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual MatingRecord MatingRecord { get; set; } = null!;
    public virtual ICollection<Puppy> Puppies { get; set; } = new List<Puppy>();
}
