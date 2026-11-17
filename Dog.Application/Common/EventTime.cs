namespace Dog.Application.Common;

/// <summary>
/// One way of writing an event timestamp, shared by every surface that shows one.
/// </summary>
/// <remarks>
/// The portal, the WhatsApp bot and the mobile app all render the same care records,
/// and they used to disagree about it — the portal printed "Jul 29, 2026 · 14:03"
/// while the bot's confirmation said only "2026-07-29", so a handler saw two
/// different answers to "when did I log that?". This is the single definition the
/// server-side surfaces use; the mobile app mirrors it in <c>Fmt.eventWhen</c>.
/// </remarks>
public static class EventTime
{
    /// <summary>
    /// Renders an event timestamp, appending the clock time only when one was
    /// actually captured. Records created from date-only sources (back-dated
    /// entries, imports) land on midnight, and printing a literal "00:00" next to
    /// them reads as "recorded at midnight" rather than "time unknown".
    /// </summary>
    public static string When(DateTime value, string dateFormat = "MMM dd, yyyy", string separator = " · ")
        => value.TimeOfDay == TimeSpan.Zero
            ? value.ToString(dateFormat)
            : value.ToString(dateFormat) + separator + value.ToString("HH:mm");
}
