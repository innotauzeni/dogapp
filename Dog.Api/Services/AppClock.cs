namespace Dog.Api.Services;

/// <summary>
/// Wall-clock time in the timezone the business actually operates in.
/// </summary>
/// <remarks>
/// Care records, invoices and expenses store a naive <see cref="DateTime"/> and every
/// client renders it back verbatim — no offset is carried in the JSON and none is
/// applied on display. The mobile app and the portal both fill those fields from the
/// user's own device clock, so the stored values are local wall-clock times.
///
/// The WhatsApp bot is the one place where the server picks the timestamp itself. Using
/// <c>DateTime.UtcNow</c> there put it on a different clock from the other two: a feeding
/// logged over WhatsApp at 14:00 in Harare was filed as 12:00 and sorted into the wrong
/// place in the same timeline as a feeding logged on the phone. This converts to the
/// configured zone so all three sources agree.
///
/// Set <c>App:TimeZone</c> to an IANA id (<c>Africa/Harare</c>) or a Windows id
/// (<c>South Africa Standard Time</c>) — .NET accepts either on either platform.
/// </remarks>
public interface IAppClock
{
    /// <summary>Current date and time, including the clock time.</summary>
    DateTime Now { get; }

    /// <summary>Current date at midnight, for fields that carry no time of day.</summary>
    DateTime Today { get; }
}

public class AppClock : IAppClock
{
    /// Zimbabwe (CAT, UTC+2, no DST) — where the deployment operates. Overridable so a
    /// second deployment elsewhere doesn't need a code change.
    public const string DefaultTimeZoneId = "Africa/Harare";

    private readonly TimeZoneInfo _timeZone;

    public AppClock(IConfiguration config, ILogger<AppClock> logger)
    {
        var configured = config["App:TimeZone"];
        var id = string.IsNullOrWhiteSpace(configured) ? DefaultTimeZoneId : configured.Trim();

        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Falling back to the server's own zone keeps the bot answering rather than
            // failing every request over a config typo — but it does mean timestamps may
            // drift from the phone/portal ones, so it is worth shouting about.
            logger.LogError(ex, "⏰ App:TimeZone '{TimeZoneId}' not found on this machine. " +
                "Falling back to the server timezone ({Fallback}) — WhatsApp timestamps may not " +
                "match those recorded from the mobile app or the portal.", id, TimeZoneInfo.Local.Id);
            _timeZone = TimeZoneInfo.Local;
        }
    }

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

    public DateTime Today => Now.Date;
}
