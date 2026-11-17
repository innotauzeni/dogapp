using Dog.Core.Common;

namespace Dog.Core.Entities;

public class MatingRecord : AuditableEntity
{
    // Nullable so a partner that is NOT registered in the system can be recorded by name only.
    public int? SireId { get; set; }
    public int? DamId { get; set; }

    // Fallback names used when the corresponding dog is external / unregistered (id is null).
    public string? SireName { get; set; }
    public string? DamName { get; set; }

    public DateTime MatingDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }

    // Navigation (optional — an external partner has no registered dog record)
    public virtual Dogtbl? Sire { get; set; }
    public virtual Dogtbl? Dam { get; set; }
    public virtual ICollection<Litter> Litters { get; set; } = new List<Litter>();
}
