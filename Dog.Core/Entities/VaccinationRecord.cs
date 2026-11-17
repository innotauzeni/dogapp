using Dog.Core.Common;

namespace Dog.Core.Entities;

public class VaccinationRecord : AuditableEntity
{
    // A vaccination belongs to EITHER a registered dog (DogId) OR an unregistered
    // puppy still tracked under its litter/mother (PuppyId). When a puppy is
    // promoted to a full dog at 6 weeks, its puppy vaccinations are re-linked to
    // the new dog (DogId set, PuppyId cleared).
    public int? DogId { get; set; }
    public int? PuppyId { get; set; }

    public string? Name { get; set; }
    public string? BatchNumber { get; set; }
    public string? VetclinicName { get; set; }
    public string? Notes { get; set; }
    public string? VaccType { get; set; }
    public DateTime VaccinationDate { get; set; }
    public DateTime? NextVaccinationDate { get; set; }

    // Navigation — the relationship itself is configured via Fluent API in
    // ApplicationDbContext (a redundant [ForeignKey] attribute here previously
    // caused EF Core to build a second, conflicting relationship and invent a
    // phantom "DogtblId" shadow column that was never actually migrated).
    public virtual Dogtbl? Dog { get; set; }
    public virtual Puppy? Puppy { get; set; }
}
