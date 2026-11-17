using Dog.Core.Common;

namespace Dog.Core.Entities;

public class Dogtbl : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public string? Gender { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? PhotoUrl { get; set; }

    // Self-referencing FK for family tree
    public int? SireId { get; set; }
    public int? DamId { get; set; }

    // Owner (customer) — a dog belongs to at most one customer
    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    // Navigation properties
    public virtual Dogtbl? Sire { get; set; }
    public virtual Dogtbl? Dam { get; set; }
    public virtual ICollection<Dogtbl> SireOffspring { get; set; } = new List<Dogtbl>();
    public virtual ICollection<Dogtbl> DamOffspring { get; set; } = new List<Dogtbl>();

    public virtual ShowDogInfo? ShowDogInfo { get; set; }
    public virtual ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
    public virtual ICollection<MatingRecord> MatingRecordsAsSire { get; set; } = new List<MatingRecord>();
    public virtual ICollection<MatingRecord> MatingRecordsAsDam { get; set; } = new List<MatingRecord>();
    public virtual ICollection<DogAssignment> Assignments { get; set; } = new List<DogAssignment>();
    public virtual ICollection<DogCareRecord> CareRecords { get; set; } = new List<DogCareRecord>();
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
}
