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

    // Navigation
    public virtual Litter Litter { get; set; } = null!;
}
