using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>Tracks the current and historical handler/branch responsibility for a dog.</summary>
public class DogAssignment : AuditableEntity
{
    public int DogId { get; set; }
    public string? HandlerUserId { get; set; }
    public int? BranchId { get; set; }
    public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
    public DateTime? EndedOn { get; set; }
    public string? Notes { get; set; }

    public Dogtbl Dog { get; set; } = null!;
    public ApplicationUser? HandlerUser { get; set; }
    public Company? Branch { get; set; }
}
