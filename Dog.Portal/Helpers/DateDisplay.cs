using Dog.Application.Common;

namespace Dog.Portal.Helpers;

/// <summary>
/// Formatting for event timestamps shown in the portal. The rule itself lives in
/// <see cref="EventTime"/> so the portal, the WhatsApp bot and the mobile app all
/// write a timestamp the same way; this stays as the name the views already call.
/// </summary>
public static class DateDisplay
{
    /// <inheritdoc cref="EventTime.When"/>
    public static string EventWhen(DateTime value, string dateFormat = "MMM dd, yyyy", string separator = " · ")
        => EventTime.When(value, dateFormat, separator);
}
