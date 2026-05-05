

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class QueSetting : AuditableBaseEntity, IAuditable
    {
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        //public bool isActive { get; set; } = true;
    }

}
