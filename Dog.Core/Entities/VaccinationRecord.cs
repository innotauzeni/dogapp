using Dog.Core.Common;

namespace Dog.Core.Entities;

public class VaccinationRecord : AuditableEntity
{
    public int DogId { get; set; }
    public string? Name { get; set; }
    public string? BatchNumber { get; set; }
    public string? VetclinicName { get; set; }
    public string? Notes { get; set; }
    public string? VaccType { get; set; }
    public DateTime VaccinationDate { get; set; }
    public DateTime? NextVaccinationDate { get; set; }

    // Navigation
    public virtual Dogtbl dog { get; set; } = null!;
}
