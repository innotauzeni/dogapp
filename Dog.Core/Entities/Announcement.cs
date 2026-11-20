using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>
/// A notice the system pushes out for a limited time: WhatsApp to every staff user (once a day for
/// <see cref="WhatsAppDays"/> days) and a banner in the web portal (for <see cref="PortalDays"/> days).
/// </summary>
public class AppAnnouncement : AuditableEntity
{
    /// <summary>Stable identifier so the seeder never creates the same notice twice.</summary>
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>The WhatsApp text (WhatsApp *bold* formatting allowed).</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Short plain text shown in the portal banner.</summary>
    public string PortalMessage { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
    /// <summary>First day (local date).</summary>
    public DateTime StartDate { get; set; }
    public int WhatsAppDays { get; set; } = 4;
    public int PortalDays { get; set; } = 3;
    /// <summary>Local hour window in which the WhatsApp message may go out.</summary>
    public int SendFromHour { get; set; } = 9;
    public int SendToHour { get; set; } = 18;
}

/// <summary>One WhatsApp message sent for an announcement (so nobody gets two on the same day).</summary>
public class AppAnnouncementDelivery : AuditableEntity
{
    public int AnnouncementId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime SentOn { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
