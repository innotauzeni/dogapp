using Dog.Core.Common;

namespace Dog.Core.Entities;

public class Puppy : AuditableEntity
{
    public int LitterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }

    // Set when the puppy has been promoted to a full registered dog (at 6 weeks).
    // Null = still tracked under the mother's litter.
    public int? RegisteredDogId { get; set; }

    // Navigation
    public virtual Litter Litter { get; set; } = null!;
}
