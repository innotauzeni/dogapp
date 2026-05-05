

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class SystemDocument : AuditableEntity
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        //public bool Status { get; set; } = true;
    }
}
