

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Emailqueue : AuditableEntity
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? HtmlBody { get; set; }
        public string? Recepiants { get; set; }
        public bool Status { get; set; } = false;
    }
}