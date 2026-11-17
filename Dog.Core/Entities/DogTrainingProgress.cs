using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities;

/// <summary>This dog's own copy of one week of the programme, created when training is activated.
/// Management can edit it per dog (add / change targets and the daily schedule).</summary>
public class DogTrainingWeeklyPlan : AuditableEntity
{
    public int EnrollmentId { get; set; }
    public int WeekNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Focus { get; set; }
    /// <summary>One target per line, "Name: description".</summary>
    public string? Targets { get; set; }
    /// <summary>JSON array of daily schedule slots: [{"time":"07:00","title":"Morning Walk","purpose":"…"}].</summary>
    public string? ScheduleJson { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public virtual DogTrainingEnrollment Enrollment { get; set; } = null!;
}

/// <summary>The handler's daily training record for one dog (one per dog per day; re-saving updates it).</summary>
public class DogTrainingDailyLog : AuditableEntity
{
    public int EnrollmentId { get; set; }
    public int DogId { get; set; }
    public string HandlerUserId { get; set; } = string.Empty;
    public DateTime TrainingDate { get; set; }
    public int WeekNumber { get; set; }
    public int DayNumber { get; set; }

    public string? ActivitiesPerformed { get; set; }
    public string? CommandsPracticed { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>JSON array of per-slot results: [{"time":"07:00","title":"Morning Walk","done":true,"rating":8,"notes":""}].</summary>
    public string? SlotsJson { get; set; }
    [Column(TypeName = "decimal(4,1)")]
    public decimal? AverageSlotRating { get; set; }
    /// <summary>Excellent / Very Good / Good / Needs Improvement / Poor.</summary>
    public string Performance { get; set; } = "Good";

    public string? BehaviourObservations { get; set; }
    public string? Challenges { get; set; }
    public string? Improvements { get; set; }
    public string? Recommendations { get; set; }
    public string? NextObjectives { get; set; }

    public virtual DogTrainingEnrollment Enrollment { get; set; } = null!;
    public virtual ApplicationUser? Handler { get; set; }
}

/// <summary>Weekly progress report submitted by the handler and reviewed / approved by the Training Manager.</summary>
public class DogTrainingProgressReport : AuditableEntity
{
    public int EnrollmentId { get; set; }
    public int WeekNumber { get; set; }
    public string HandlerUserId { get; set; } = string.Empty;

    public string? Objectives { get; set; }
    public string? ActivitiesCompleted { get; set; }
    public string? ActivitiesOutstanding { get; set; }
    public string? PerformanceSummary { get; set; }
    public string? Challenges { get; set; }
    public string? Improvements { get; set; }
    public string? Recommendations { get; set; }

    /// <summary>Draft / Submitted / Approved / Returned.</summary>
    public string Status { get; set; } = "Draft";
    public DateTime? SubmittedOn { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? ReviewComments { get; set; }

    public virtual DogTrainingEnrollment Enrollment { get; set; } = null!;
    public virtual ApplicationUser? Handler { get; set; }
}
