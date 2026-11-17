using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities;

/// <summary>Pre-training veterinary / inspector health assessment (mirrors the BES "Health Check Form – Dog").
/// An enrolment may have several (re-assessment); only a Passed one lets the dog proceed.</summary>
public class DogHealthAssessment : AuditableEntity
{
    public int EnrollmentId { get; set; }
    public int DogId { get; set; }
    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;

    // Inspector
    public string? ExaminerUserId { get; set; }
    public string? ExaminerName { get; set; }
    public string? ForceNumber { get; set; }
    public string? ExaminerPhone { get; set; }
    public string? DogHandlerName { get; set; }

    // Pet details captured at the time of the exam
    public string? Species { get; set; } = "Dog";
    [Column(TypeName = "decimal(8,2)")]
    public decimal? WeightKg { get; set; }
    public string? PhysicalCondition { get; set; }
    public string? VaccinationStatus { get; set; }   // Up to date / Overdue / Unknown
    public string? DewormingStatus { get; set; }
    public string? MedicalConditions { get; set; }
    public DateTime? FleaTickTreatmentDate { get; set; }

    // Certification statements on the form
    public bool CertGoodHealth { get; set; }
    public bool CertVaccinationsCurrent { get; set; }
    public bool CertNoParasites { get; set; }
    public bool CertNoBiteHistory { get; set; }
    /// <summary>None / Warning / PositiveChange / Withdrawal — the three "I recommend…" lines.</summary>
    public string Recommendation { get; set; } = "None";

    public string? Comments { get; set; }
    public string? InspectorSignatureName { get; set; }
    public DateTime? SignedOn { get; set; }

    /// <summary>Pending / Passed / Failed / RequiresFurtherExamination.</summary>
    public string Result { get; set; } = "Pending";
    public DateTime? ResultDate { get; set; }
    public string? DecidedByUserId { get; set; }

    public virtual DogTrainingEnrollment Enrollment { get; set; } = null!;
    public virtual DogTrainingDog Dog { get; set; } = null!;
    public virtual ICollection<DogHealthAssessmentItem> Items { get; set; } = new List<DogHealthAssessmentItem>();
}

/// <summary>One line of the form: an examination finding (Normal/Abnormal) or a treatment record.</summary>
public class DogHealthAssessmentItem : AuditableEntity
{
    public int AssessmentId { get; set; }
    /// <summary>"Exam" or "Treatment".</summary>
    public string Category { get; set; } = "Exam";
    public string Name { get; set; } = string.Empty;
    /// <summary>Exam lines: Normal / Abnormal (empty = not examined).</summary>
    public string? Status { get; set; }
    public string? Notes { get; set; }
    // Treatment lines
    public bool Administered { get; set; }
    public DateTime? TreatmentDate { get; set; }
    public string? Reason { get; set; }
    public bool BesNotified { get; set; }
    public int SortOrder { get; set; }

    public virtual DogHealthAssessment Assessment { get; set; } = null!;
}
