using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities;

/// <summary>Status values for <see cref="DogTrainingEnrollment.Status"/> (strings, like Invoice.Status).</summary>
public static class TrainingStatus
{
    public const string AwaitingHealthAssessment = "AwaitingHealthAssessment";
    public const string HealthFailed = "HealthFailed";
    public const string HealthPassed = "HealthPassed";            // passed, invoice not raised yet
    public const string AwaitingPayment = "AwaitingPayment";      // invoice raised, not fully paid
    public const string AwaitingHandler = "AwaitingHandler";      // paid, no handler yet
    public const string Ready = "Ready";                          // paid + handler, awaiting activation
    public const string InTraining = "InTraining";
    public const string AwaitingFinalAssessment = "AwaitingFinalAssessment";
    public const string AwaitingApproval = "AwaitingApproval";    // assessed Passed, management approval pending
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
    {
        AwaitingHealthAssessment, HealthFailed, HealthPassed, AwaitingPayment, AwaitingHandler, Ready,
        InTraining, AwaitingFinalAssessment, AwaitingApproval, Completed, Cancelled
    };
}

/// <summary>A dog's enrolment on a training programme. This is the central record of the workflow:
/// health assessment → invoice/payment → handler → activation → training → final assessment → certificate.</summary>
public class DogTrainingEnrollment : AuditableEntity
{
    public string EnrollmentNumber { get; set; } = string.Empty;   // TRN-2026-0001
    public int DogId { get; set; }
    public int? CustomerId { get; set; }
    public int ProgrammeId { get; set; }

    // Snapshot of the programme terms at enrolment (the programme may change later)
    public int DurationDays { get; set; } = 21;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fee { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal AdditionalCharges { get; set; }
    public string? AdditionalChargesNote { get; set; }

    public string Status { get; set; } = TrainingStatus.AwaitingHealthAssessment;

    /// <summary>The training invoice (Dog Training keeps its own invoices and payments).</summary>
    public int? InvoiceId { get; set; }

    /// <summary>Current training handler (denormalised from the latest DogHandlerAssignment for fast filtering).</summary>
    public string? HandlerUserId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    /// <summary>Extra days added after a failed / "requires additional training" assessment.</summary>
    public int ExtensionDays { get; set; }
    public string? ActivatedByUserId { get; set; }
    public DateTime? ActivatedOn { get; set; }

    public string? Notes { get; set; }
    public string? CancelReason { get; set; }

    public virtual DogTrainingDog Dog { get; set; } = null!;
    public virtual DogTrainingCustomer? Customer { get; set; }
    public virtual DogTrainingProgramme Programme { get; set; } = null!;
    public virtual DogTrainingInvoice? Invoice { get; set; }
    public virtual ApplicationUser? Handler { get; set; }

    public virtual ICollection<DogHealthAssessment> HealthAssessments { get; set; } = new List<DogHealthAssessment>();
    public virtual ICollection<DogHandlerAssignment> HandlerAssignments { get; set; } = new List<DogHandlerAssignment>();
    public virtual ICollection<DogTrainingWeeklyPlan> WeeklyPlans { get; set; } = new List<DogTrainingWeeklyPlan>();
    public virtual ICollection<DogTrainingDailyLog> DailyLogs { get; set; } = new List<DogTrainingDailyLog>();
    public virtual ICollection<DogTrainingProgressReport> ProgressReports { get; set; } = new List<DogTrainingProgressReport>();
    public virtual ICollection<DogTrainingFinalAssessment> FinalAssessments { get; set; } = new List<DogTrainingFinalAssessment>();
}
