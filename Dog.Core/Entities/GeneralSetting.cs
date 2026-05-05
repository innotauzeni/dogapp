

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class GeneralSetting : AuditableEntity
    {
        public string? Name { get; set; }
        public string? TemplateHtml { get; set; }
        public string? PrimaryColor { get; set; }
        public string? PrimarybuttonColors { get; set; }
        public string? InfobuttonColors { get; set; }
        public string? SecondarybuttonColors { get; set; }
        public string? Logo { get; set; }
        public string? Smalllogo { get; set; }
        public string? Copyright { get; set; }
        //public bool isActive { get; set; } = true;
        public bool isTempateEnabled { get; set; } = true;
    }
}