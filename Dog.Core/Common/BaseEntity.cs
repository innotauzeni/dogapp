using System.ComponentModel.DataAnnotations;

namespace Dog.Core.Common
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string? Uuid { get; set; } = Guid.NewGuid().ToString();
    }
}
