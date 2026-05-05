using Dog.Core.Common;

namespace Dog.Core.Entities;

public class BreedGroup : AuditableEntity
{
    public string? Name { get; set; }
    
    // Navigation
    public virtual ICollection<Breed> Breeds { get; set; } = new List<Breed>();
}
