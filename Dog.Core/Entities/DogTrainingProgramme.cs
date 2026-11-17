using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities;

/// <summary>A reusable training programme / plan template (e.g. "21-Day Foundation"). Enrolments take a
/// snapshot of its weeks and daily schedule when training is activated, so editing the template later
/// never changes a dog's running plan.</summary>
public class DogTrainingProgramme : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Default 21; programmes may run 30, 45, 60 … days.</summary>
    public int DurationDays { get; set; } = 21;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fee { get; set; }

    public virtual ICollection<DogTrainingProgrammeWeek> Weeks { get; set; } = new List<DogTrainingProgrammeWeek>();
    public virtual ICollection<DogTrainingProgrammeActivity> Activities { get; set; } = new List<DogTrainingProgrammeActivity>();
}

/// <summary>One week of a programme: its title, focus (e.g. "Low Distraction") and the weekly targets.</summary>
public class DogTrainingProgrammeWeek : AuditableEntity
{
    public int ProgrammeId { get; set; }
    public int WeekNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Focus { get; set; }
    /// <summary>One target per line, as "Name: description".</summary>
    public string? Targets { get; set; }

    public virtual DogTrainingProgramme Programme { get; set; } = null!;
}

/// <summary>A slot in the daily schedule (07:00 Morning Walk …). WeekNumber 0 = every week; otherwise only that week.</summary>
public class DogTrainingProgrammeActivity : AuditableEntity
{
    public int ProgrammeId { get; set; }
    public int WeekNumber { get; set; }
    public string TimeOfDay { get; set; } = string.Empty;   // "07:00"
    public string Title { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public int SortOrder { get; set; }

    public virtual DogTrainingProgramme Programme { get; set; } = null!;
}
