using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>Training-specific details for a user who is a dog handler: capacity, availability and specialisation.
/// Kept separate so the Identity user table is not touched.</summary>
public class DogTrainingHandlerProfile : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    /// <summary>Maximum number of dogs the handler may train at the same time.</summary>
    public int MaxDogs { get; set; } = 5;
    public string? Specialisation { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? Notes { get; set; }

    public virtual ApplicationUser? User { get; set; }
}

/// <summary>Allocation of an enrolment (dog) to a handler. Reallocating ends the previous row.</summary>
public class DogHandlerAssignment : AuditableEntity
{
    public int EnrollmentId { get; set; }
    public string HandlerUserId { get; set; } = string.Empty;
    public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
    public string? AssignedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime? EndedOn { get; set; }
    public bool IsCurrent { get; set; } = true;

    public virtual DogTrainingEnrollment Enrollment { get; set; } = null!;
    public virtual ApplicationUser? HandlerUser { get; set; }
}
