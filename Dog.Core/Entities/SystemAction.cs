

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class SystemAction : AuditableBaseEntity
    {
        public string? Controller { get; set; } //Submodule
        public string? Action { get; set; } //Create
        public string? Name { get; set; } //
    }

}
