

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Passwordreset : AuditableBaseEntity
    {
        public string? Email { get; set; }

        public string? Token { get; set; }

        public DateTime? ExpireDate { get; set; }


    }
}
