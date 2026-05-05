using Dog.Core.Common;

namespace Dog.Core.Entities;

public class Breed : AuditableEntity
{
    public int BreedGroupId { get; set; }
    public string? Name { get; set; }
    
    // Navigation
    public virtual BreedGroup BreedGroup { get; set; } = null!;
}