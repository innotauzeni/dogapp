using Dog.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace Dog.Core.Common
{
    // Implementing IAuditable here means every entity that extends AuditableBaseEntity
    // is automatically tracked in the audit log — no need to add IAuditable to each entity.
    public abstract class AuditableBaseEntity : BaseEntity, IAuditable
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
