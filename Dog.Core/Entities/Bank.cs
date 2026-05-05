

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Bank : AuditableEntity
    {
        public string? Name { get; set; }
        public string? AccountName { get; set; }
    }

}