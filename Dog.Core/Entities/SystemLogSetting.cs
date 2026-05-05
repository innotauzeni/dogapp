

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class SystemLogSetting : AuditableEntity, IAuditable
    {
        public string? LogFolder { get; set; }
        public string? AlertEmail { get; set; }
        public bool EnableFileLog { get; set; } = false;
        public bool EnableDbLog { get; set; } = false;
        public bool EnableEmailAlert { get; set; } = false;
        public bool EnableTelegramAlert { get; set; } = false;
    }
}
