using System.Collections.Concurrent;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.Invoices.DTOs;

namespace Dog.Api.Services;

/// <summary>Per-phone-number conversation state for the WhatsApp bot.</summary>
public class UserSession
{
    public string CurrentMenu { get; set; } = "login"; // Start with login
    public bool IsAuthenticated { get; set; } = false;
    public string? AuthenticatedUsername { get; set; }
    public string? AuthenticatedUserId { get; set; } // Identity user id (string/GUID)
    public int? AuthenticatedCompanyId { get; set; } // Store user's company ID
    public bool AuthenticatedIsHeadOffice { get; set; }
    public string? AuthenticatedRole { get; set; }

    /// Permissions from the login (e.g. training.log.record). The bot calls MediatR handlers directly, so it must
    /// check these itself for anything that is permission-gated in the API.
    public HashSet<string> Permissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// Admin-level actions (assign dog, register branch, create user) bypass the
    /// API's own [HasPermission] checks since MediatR handlers are called directly
    /// here rather than through the authorized controller layer — this is the only
    /// gate standing between a logged-in bot user and those actions.
    public bool IsAdminLevel => AuthenticatedIsHeadOffice
        || string.Equals(AuthenticatedRole, "Admin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(AuthenticatedRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(AuthenticatedRole, "SystemOwner", StringComparison.OrdinalIgnoreCase);
    public int? SelectedBranchId { get; set; }
    public string? SelectedUserId { get; set; }
    public int? SelectedDogId { get; set; }
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;

    /// Set by the store when this session replaced one that went idle past the
    /// timeout, so the bot can tell the user they were logged out rather than
    /// silently dropping them back at the login prompt. Cleared once shown.
    public bool TimedOut { get; set; }

    /// True while in a "Dog Handler" role and not head-office/admin — used to
    /// scope the dogs list and dashboard to only this handler's assigned dogs,
    /// matching the mobile app's handler-only screens.
    public bool IsDogHandlerOnly => !IsAdminLevel && string.Equals(AuthenticatedRole, "Dog Handler", StringComparison.OrdinalIgnoreCase);

    // Guided "Record Care" flow tracking (mirrors the mobile app's Record Care
    // form, but as a step-by-step conversation instead of a single form screen).
    public int? RecordFlowDogId { get; set; }
    public string? RecordFlowEventType { get; set; }
    public string? RecordFlowNote { get; set; }

    // Guided "Add Vaccine" flow tracking.
    public int? VaccineFlowDogId { get; set; }
    public string? VaccineFlowName { get; set; }
    public DateTime? VaccineFlowNextDue { get; set; }
    public string? VaccineFlowNote { get; set; }

    // Guided "Add Mating Record" flow tracking.
    public int? MatingFlowSubjectId { get; set; }
    public int? MatingFlowPartnerId { get; set; }
    public string? MatingFlowPartnerName { get; set; }

    // Guided "Add Litter" flow tracking.
    public int? LitterFlowMatingId { get; set; }
    public int LitterFlowTotalBorn { get; set; }
    public int LitterFlowSurvivedCount { get; set; }
    public int? LitterFlowLitterId { get; set; }
    public int LitterFlowPuppiesTarget { get; set; }
    public int LitterFlowPuppiesAdded { get; set; }
    public string? LitterFlowCurrentPuppyName { get; set; }
    public string? LitterFlowCurrentPuppyGender { get; set; }

    // Guided "Mark Deceased" flow tracking.
    public int? DeceasedFlowDogId { get; set; }

    // Guided "Add Invoice" flow tracking.
    public int? InvoiceFlowCustomerId { get; set; }
    public List<CreateInvoiceItemDto> InvoiceFlowItems { get; set; } = new();
    public string? InvoiceFlowCurrentDescription { get; set; }
    public int InvoiceFlowCurrentQuantity { get; set; }
    public string? InvoiceFlowNotes { get; set; }
    public DateTime? InvoiceFlowInvoiceDate { get; set; }

    // Guided "Add Expense" flow tracking.
    public string? ExpenseFlowCategory { get; set; }
    public string? ExpenseFlowDescription { get; set; }
    public decimal ExpenseFlowAmount { get; set; }

    // Dog Training flows (log today's training / record a payment).
    public List<string> TrainingMenuOptions { get; set; } = new();
    public List<string> TrainingDogNames { get; set; } = new();
    public DailyLogDto? TrainingLogDraft { get; set; }
    public List<LogSlotDto> TrainingSlots { get; set; } = new();
    public int TrainingSlotIndex { get; set; }
    public int TrainingMinutes { get; set; }
    public string? TrainingDogName { get; set; }
    public Dog.Application.Features.DogTraining.DTOs.ProgressReportDto? TrainingReportDraft { get; set; }
    public List<int> TrainingReportWeeks { get; set; } = new();
    public int? TrainingInvoiceId { get; set; }
    public decimal TrainingPayAmount { get; set; }
    public Dog.Application.Features.DogTraining.DTOs.RecordTrainingPaymentDto? TrainingPayDraft { get; set; }
    public List<string> TrainingPayQueue { get; set; } = new();
    public List<int> TrainingPayBankIds { get; set; } = new();

    // Login flow tracking
    public string? LoginUsername { get; set; }
    public int LoginStep { get; set; } = 0; // 0=username, 1=password

    /// False until the "enter your username" prompt has actually been sent in this
    /// login attempt. Without it the very first message from a logged-out phone is
    /// swallowed as a username — so a stale "1" meant for a menu came back as
    /// "Username received: 1" and then failed the login.
    public bool LoginPromptShown { get; set; }

    // Maps a displayed numbered list (1, 2, 3...) back to the real database id
    // the user picked, refreshed every time a list is shown. Separate lists for
    // int-keyed (dogs/branches/matings) vs string-keyed (users) entities.
    public List<int> LastListIds { get; set; } = new();
    public List<string> LastUserListIds { get; set; } = new();
    public List<string> LastRoleNames { get; set; } = new();

    // User creation flow tracking
    public string? CreatingUserFirstName { get; set; }
    public string? CreatingUserLastName { get; set; }
    public string? CreatingUserEmail { get; set; }
    public string? CreatingUserPhone { get; set; }
    public string? CreatingUserRole { get; set; }
    public int? CreatingUserBranchId { get; set; }
    public int CreationStep { get; set; } = 0; // Track which step of creation we're in
}

/// <summary>
/// Holds WhatsApp bot conversation state across requests. Must be registered as a
/// singleton — WhatsAppBotService itself is scoped (a fresh instance per webhook
/// request), so storing sessions as an instance field on the service would reset
/// to empty on every single incoming message, losing all login/menu state.
/// </summary>
public interface IWhatsAppSessionStore
{
    UserSession GetOrCreate(string phoneNumber);
}

public class WhatsAppSessionStore : IWhatsAppSessionStore
{
    /// How long a phone can go quiet before it is logged out. WhatsApp threads stay
    /// open indefinitely on the handset, so without this a session lived forever:
    /// someone could pick the phone up hours later and still be inside another
    /// person's authenticated session, half-way through an abandoned guided flow.
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, UserSession> _sessions = new();

    public UserSession GetOrCreate(string phoneNumber)
    {
        var now = DateTime.UtcNow;
        var session = _sessions.GetOrAdd(phoneNumber, _ => new UserSession());

        if (now - session.LastInteraction > IdleTimeout)
        {
            // Replace rather than reset in place: a concurrent webhook still
            // holding the old instance keeps talking to the expired session
            // instead of racing us through a half-cleared one.
            session = new UserSession { TimedOut = true };
            _sessions[phoneNumber] = session;
        }

        session.LastInteraction = now;
        return session;
    }
}
