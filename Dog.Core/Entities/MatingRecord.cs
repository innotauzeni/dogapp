using Dog.Core.Common;

namespace Dog.Core.Entities;

public class MatingRecord : AuditableEntity
{
    public int SireId { get; set; }
    public int DamId { get; set; }
    public DateTime MatingDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Dogtbl Sire { get; set; } = null!;
    public virtual Dogtbl Dam { get; set; } = null!;
    public virtual ICollection<Litter> Litters { get; set; } = new List<Litter>();
}
