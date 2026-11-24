using System.Text;
using Dog.Application.Features.Animals.FeedAll;
using Dog.Application.Features.Animals.Queries;

namespace Dog.Api.Services;

/// <summary>
/// "Feed all dogs" on WhatsApp: when the whole kennel is fed at once the handler does not have to record dog by dog -
/// type <c>feedall</c> (or reply <c>all</c> in the Record Care dog list), confirm, and every dog gets today's feeding.
/// </summary>
public partial class WhatsAppBotService
{
    private async Task<string> StartFeedAllAsync(int companyId, UserSession session)
    {
        var dogs = (await _mediator.Send(new GetAnimalsByCompanyQuery(companyId)))?.Data;
        var count = dogs?.Count ?? 0;
        if (count == 0) return "🐕 There are no dogs to feed.\n\nType *0* for the main menu.";

        session.CurrentMenu = "feedall_confirm";
        return $"🍖 *FEED ALL DOGS*\n\nRecord today's feeding for *all {count} dogs* with the *standard portion*?\n\n" +
               "✅ Reply *yes* to confirm\n" +
               "✏️ or type a different portion (for example *2 cups*)\n" +
               "❌ or *no* to cancel\n\n" +
               "_Dogs that already have today's feeding are skipped, dogs on deployment are not included, and a WhatsApp notice goes out for each dog._";
    }

    private async Task<string> HandleFeedAllConfirmAsync(string input, int companyId, UserSession session)
    {
        var t = (input ?? "").Trim();
        var lower = t.ToLowerInvariant();
        if (lower is "no" or "n" or "cancel" or "back")
        {
            session.CurrentMenu = "main";
            return "Cancelled — nothing was recorded.\n\n" + GetMainMenu();
        }

        var portion = lower is "yes" or "y" or "ok" or "confirm" ? "Standard portion" : t;
        if (portion.Length > 100) return "❌ That portion description is too long. Reply *yes*, a short portion like *2 cups*, or *no*.";

        var resp = await _mediator.Send(new FeedAllDogsCommand(new FeedAllDogsDto
        {
            CompanyId = companyId,
            Portion = portion,
            OccurredOn = _clock.Now,
            RecordedByUserId = session.AuthenticatedUserId,
        }));
        session.CurrentMenu = "main";
        if (resp?.Success != true || resp.Data == null)
            return $"❌ {resp?.Message ?? "Could not record the feeding."}\n\n" + GetMainMenu();

        var r = resp.Data;
        var sb = new StringBuilder();
        sb.AppendLine($"✅ *{r.Fed} dog(s) fed* — {r.Portion}");
        if (r.AlreadyFed.Count > 0) sb.AppendLine($"ℹ️ Already fed today: {string.Join(", ", r.AlreadyFed.Take(15))}{(r.AlreadyFed.Count > 15 ? $" and {r.AlreadyFed.Count - 15} more" : "")}");
        if (r.Deployed.Count > 0) sb.AppendLine($"ℹ️ On deployment (not included): {r.Deployed.Count}");
        if (r.NotifyingWhatsApp) sb.AppendLine("📲 WhatsApp notices are going out for each dog — it takes a few minutes.");
        sb.AppendLine();
        sb.Append(GetMainMenu());
        return sb.ToString();
    }
}
