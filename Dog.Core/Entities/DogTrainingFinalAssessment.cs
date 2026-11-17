using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities;

/// <summary>End-of-training assessment. A dog may be assessed more than once (extended training).</summary>
public class DogTrainingFinalAssessment : AuditableEntity
{
    public int EnrollmentId { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
    public string? AssessorUserId { get; set; }
    public string? AssessorName { get; set; }

    /// <summary>JSON array: [{"area":"Sit","score":8,"comments":""}] for Obedience, Sit, Stay, Recall, Heel,
    /// Leash control, Socialisation, Behaviour, Responsiveness, Specialised skills.</summary>
    public string? ItemsJson { get; set; }
    [Column(TypeName = "decimal(4,1)")]
    public decimal? OverallScore { get; set; }

    /// <summary>Passed / Failed / RequiresAdditionalTraining.</summary>
    public string Result { get; set; } = "Passed";
    public string? Comments { get; set; }

    // When the dog needs more training
    public int ExtensionDays { get; set; }
    public string? AdditionalActivities { get; set; }

    /// <summary>Management approval of a Passed result: Pending / Approved / Rejected.</summary>
    public string ApprovalStatus { get; set; } = "Pending";
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? ApprovalComments { get; set; }

    public virtual DogTrainingEnrollment Enrollment { get; set; } = null!;
    public virtual DogTrainingCertificate? Certificate { get; set; }
}

/// <summary>Certificate of completion. Only created from an approved, successful final assessment.</summary>
public class DogTrainingCertificate : AuditableEntity
{
    public int EnrollmentId { get; set; }
    public int FinalAssessmentId { get; set; }
    /// <summary>Unique, human readable: DTC-2026-000123.</summary>
    public string CertificateNumber { get; set; } = string.Empty;
    /// <summary>Random token used in the public verification link / QR code.</summary>
    public string VerificationToken { get; set; } = string.Empty;
    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public string? IssuedByUserId { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }

    public virtual DogTrainingEnrollment Enrollment { get; set; } = null!;
    public virtual DogTrainingFinalAssessment FinalAssessment { get; set; } = null!;
}
