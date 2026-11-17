using Dog.Core.Common;

namespace Dog.Core.Entities
{
    /// <summary>
    /// Stores application-wide settings as key-value pairs in the database.
    /// Used for dynamic content like landing page configuration, feature flags, etc.
    /// </summary>
    public class KaAppSetting: AuditableBaseEntity
    {
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
    }
}

