using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>A handler's dated care, health, breeding, or general observation entry.</summary>
public class DogCareRecord : AuditableEntity
{
    public int DogId { get; set; }
    public string EventType { get; set; } = string.Empty; // Feeding, Illness, Heat, Mating, Birth, Other
    public DateTime OccurredOn { get; set; }
    public string? Details { get; set; }
    public string? QuantityOrOutcome { get; set; }
    public string? Location { get; set; } // Kennel/location where event occurred (for feeding, etc)
    public bool RequiresAttention { get; set; }
    public string? RecordedByUserId { get; set; }

    // Vaccination fields
    public string? VaccineName { get; set; }
    public string? VaccineBatch { get; set; }
    public DateTime? VaccineNextDue { get; set; }
    public string? VaccineNotes { get; set; }

    // Mating fields
    public string? MatingPartnerName { get; set; }
    public string? MatingDuration { get; set; }

    // Injury fields
    public string? InjurySeverity { get; set; }
    public string? InjuryDetails { get; set; }
    public bool InjuryTreated { get; set; }

    // Bite fields
    public string? BiteSeverity { get; set; }
    public string? BiteDetails { get; set; }
    public string? BiteReportedTo { get; set; }

    // Deworming fields
    public string? DewormingProduct { get; set; }
    public DateTime? DewormingNextDue { get; set; }

    public Dogtbl Dog { get; set; } = null!;
    public ApplicationUser? RecordedByUser { get; set; }
}
