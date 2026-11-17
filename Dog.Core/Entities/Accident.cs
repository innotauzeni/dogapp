using Dog.Core.Common;

namespace Dog.Core.Entities;

public enum AccidentType
{
    BitingIncident = 1,
    SicknessRecord = 2,
    PropertyDamage = 3
}

public enum AccidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class Accident : AuditableEntity
{
    public int DogId { get; set; }
    public DateTime AccidentDate { get; set; }
    public AccidentType AccidentType { get; set; }
    public AccidentSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Resolution { get; set; }
    public bool IsResolved { get; set; }

    // For biting incidents
    public string? PersonName { get; set; }
    public string? PersonPhoneNumber { get; set; }
    public string? PersonEmail { get; set; }
    public string? MedicalTreatmentProvided { get; set; }
    public bool HospitalVisitRequired { get; set; }

    // For sickness records
    public string? Symptoms { get; set; }
    public string? Treatment { get; set; }
    public string? VeterinarianName { get; set; }
    public DateTime? RecoveryDate { get; set; }

    // For property damage
    public decimal? EstimatedDamageCost { get; set; }
    public string? DamageDescription { get; set; }
    public bool InsuranceClaimFiled { get; set; }
    public string? InsuranceClaimNumber { get; set; }

    // Navigation
    public virtual Dogtbl Dog { get; set; } = null!;
}
