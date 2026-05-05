using Dog.Core.Common;

namespace Dog.Core.Entities;

public class ShowDogInfo : AuditableBaseEntity
{
    public int DogId { get; set; }
    public string ShowName { get; set; } = string.Empty;
    public string? Titles { get; set; }
    public string? CompetitionHistory { get; set; }

    // Navigation
    public virtual Dogtbl Dog { get; set; } = null!;
}
