using System.Text;
using System.Text.Json;
using Dog.Application.Services;
using Dog.Application.Features.Auth.Commands;
using Dog.Application.Features.Auth.DTOs;
using Dog.Application.Features.Animals.Commands;
using Dog.Application.Features.Animals.Queries;
using Dog.Application.Features.Animals.DTOs;
using Dog.Application.Features.Companies.Commands;
using Dog.Application.Features.Companies.Queries;
using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.CustomerSites.Queries;
using Dog.Application.Features.CustomerSites.DTOs;
using Dog.Application.Features.Customers.Queries;
using Dog.Application.Features.Customers.DTOs;
using Dog.Application.Features.Deployments.DTOs;
using Dog.Application.Features.Invoices.Commands;
using Dog.Application.Features.Invoices.DTOs;
using Dog.Application.Features.Expenses.Commands;
using Dog.Application.Features.Expenses.DTOs;
using Dog.Application.Common;
using Dog.Application.Common.Pdf;
using System.Net.Http.Headers;
using Dog.Application.Features.Users.Commands;
using Dog.Application.Features.Users.Queries;
using Dog.Application.Features.Users.DTOs;
using Dog.Application.Features.Roles.Queries;
using Dog.Application.Features.Roles.DTOs;
using MediatR;

namespace Dog.Api.Services;

/// <summary>WhatsApp Bot for managing dog breeding activities via WhatsApp messages.</summary>
public interface IWhatsAppBotService
{
    Task<string> ProcessMessageAsync(string phoneNumber, string message);
    Task SendMessageAsync(string phoneNumber, string message);
}

public partial class WhatsAppBotService : IWhatsAppBotService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppBotService> _logger;
    private readonly IErrorLoggingService _errorLog;
    private readonly IWhatsAppSessionStore _sessionStore;
    private readonly IMediator _mediator;
    private readonly IAppClock _clock;
    private readonly WhatsAppSettings _settings;

    public WhatsAppBotService(HttpClient httpClient, IConfiguration config, ILogger<WhatsAppBotService> logger,
        IErrorLoggingService errorLog, IWhatsAppSessionStore sessionStore, IMediator mediator, IAppClock clock)
    {
        _httpClient = httpClient;
        _logger = logger;
        _errorLog = errorLog;
        _sessionStore = sessionStore;
        _mediator = mediator;
        _clock = clock;
        _settings = new WhatsAppSettings
        {
            AccessToken = config["WhatsApp:AccessToken"],
            PhoneNumberId = config["WhatsApp:PhoneNumberId"],
            WebhookToken = config["WhatsApp:WebhookToken"],
            ApiVersion = config["WhatsApp:ApiVersion"] ?? "v25.0"
        };
    }

    /// Guided-flow states that capture arbitrary free text (a note, a vaccine name, a
    /// puppy's name...) — command interception must be skipped in these states so the
    /// user's raw words are never reinterpreted as a command like "dogs" or "vaccine".
    private static readonly HashSet<string> FreeTextCaptureMenus = new()
    {
        "record_note", "vaccine_name", "vaccine_due", "vaccine_notes", "mating_partner_name", "litter_puppy_name",
        "invoice_item_desc", "invoice_notes", "training_notes", "training_report_text", "training_pay_field"
    };

    // Sessions live in IWhatsAppSessionStore (a singleton) rather than on this
    // service, since this service is scoped — a new instance per webhook request —
    // and would otherwise lose all conversation state after every single message.
    private UserSession GetOrCreateSession(string phoneNumber) => _sessionStore.GetOrCreate(phoneNumber);

    public async Task<string> ProcessMessageAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("📱 WhatsApp Message Received - Phone: {Phone}, Message: {Message}", phoneNumber, message);

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("⚠️ Empty message received");
            return "Please send a message.";
        }

        var session = GetOrCreateSession(phoneNumber);
        _logger.LogInformation("🔑 Session Info - Authenticated: {IsAuth}, Username: {Username}, CompanyId: {CompanyId}",
            session.IsAuthenticated, session.AuthenticatedUsername ?? "N/A", session.AuthenticatedCompanyId ?? 0);

        var input = message.Trim();
        var inputLower = input.ToLower();
        var parts = inputLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // If not authenticated, handle login flow only
        if (!session.IsAuthenticated)
        {
            _logger.LogInformation("🔐 User not authenticated, showing login flow");
            return await HandleLoginFlowAsync(input, inputLower, session);
        }

        // After login, handle menu navigation
        if (inputLower == "back" || inputLower == "menu" || inputLower == "main")
        {
            _logger.LogInformation("↩️ User navigating to main menu");
            session.CurrentMenu = "main";
            return GetMainMenu();
        }

        if (inputLower == "0" || inputLower == "exit" || inputLower == "quit")
        {
            _logger.LogInformation("🚪 User exiting to main menu");
            session.CurrentMenu = "main";
            return GetMainMenu();
        }

        // Use company ID from authenticated user session
        var companyId = session.AuthenticatedCompanyId ?? 1;
        _logger.LogInformation("📊 Processing command: {Command}, Menu: {Menu}, CompanyId: {CompanyId}",
            inputLower, session.CurrentMenu, companyId);

        // Free-text power commands, available from any state (as advertised by the help
        // menu) — except while capturing arbitrary free text in a guided flow (a note, a
        // vaccine name, a puppy's name...), where the user's raw words (e.g. "dogs ate
        // well") must never be reinterpreted as a command like "dogs".
        var textCommand = FreeTextCaptureMenus.Contains(session.CurrentMenu)
            ? null
            : await TryHandleTextCommandAsync(phoneNumber, inputLower, parts, companyId, session);
        if (textCommand != null) return textCommand;

        var response = session.CurrentMenu switch
        {
            "main" => await HandleMainMenuAsync(phoneNumber, inputLower, companyId, parts, session),
            "branches" => await HandleBranchesMenuAsync(inputLower, companyId, session),
            "branch_detail" => await HandleBranchDetailAsync(inputLower, companyId, session),
            "branch_delete_confirm" => await HandleBranchDeleteConfirmAsync(inputLower, companyId, session),
            "users" => await HandleUsersMenuAsync(inputLower, companyId, session),
            "user_detail" => await HandleUserDetailAsync(inputLower, companyId, session),
            "user_delete_confirm" => await HandleUserDeleteConfirmAsync(inputLower, companyId, session),
            "dogs" => await HandleDogsMenuAsync(inputLower, companyId, session),
            "dog_detail" => await HandleDogDetailAsync(companyId, session),
            "matings" => await HandleMatingsMenuAsync(inputLower, companyId, session),
            "family" => await HandleFamilyMenuAsync(inputLower, companyId, session),
            "create_user" => await HandleCreateUserStepAsync(phoneNumber, input, companyId, session),
            "customers" => await HandleCustomersMenuAsync(inputLower, companyId, session),
            "record_dog" => await HandleRecordDogMenuAsync(inputLower, companyId, session),
            "record_event" => await HandleRecordEventMenuAsync(inputLower, session),
            "record_note" => await HandleRecordNoteMenuAsync(input, session),
            "record_attention" => await HandleRecordAttentionMenuAsync(inputLower, companyId, session),
            "vaccine_dog" => await HandleVaccineDogMenuAsync(inputLower, companyId, session),
            "vaccine_name" => HandleVaccineNameMenuAsync(input, session),
            "vaccine_due" => HandleVaccineDueMenuAsync(input, session),
            "vaccine_notes" => HandleVaccineNotesMenuAsync(input, session),
            "vaccine_date" => await HandleVaccineDateMenuAsync(input, companyId, session),
            "mating_dog" => await HandleMatingDogMenuAsync(inputLower, companyId, session),
            "mating_partner" => await HandleMatingPartnerMenuAsync(inputLower, companyId, session),
            "mating_partner_name" => await HandleMatingPartnerNameMenuAsync(input, session),
            "mating_date" => await HandleMatingDateMenuAsync(input, companyId, session),
            "litter_dog" => await HandleLitterDogMenuAsync(inputLower, companyId, session),
            "litter_mating" => HandleLitterMatingMenuAsync(inputLower, session),
            "litter_totalborn" => HandleLitterTotalBornMenuAsync(inputLower, session),
            "litter_survived" => await HandleLitterSurvivedMenuAsync(inputLower, companyId, session),
            "litter_date" => await HandleLitterDateMenuAsync(input, companyId, session),
            "litter_puppy_count" => HandleLitterPuppyCountMenuAsync(inputLower, session),
            "litter_puppy_name" => HandleLitterPuppyNameMenuAsync(input, session),
            "litter_puppy_gender" => await HandleLitterPuppyGenderMenuAsync(inputLower, companyId, session),
            "deceased_dog" => await HandleDeceasedDogMenuAsync(inputLower, companyId, session),
            "deceased_confirm" => HandleDeceasedConfirmMenuAsync(inputLower, session),
            "deceased_date" => await HandleDeceasedDateMenuAsync(input, session),
            "invoice_customer" => await HandleInvoiceCustomerMenuAsync(inputLower, companyId, session),
            "invoice_item_desc" => await HandleInvoiceItemDescMenuAsync(input, companyId, session),
            "invoice_item_pickdog" => HandleInvoiceItemPickDogMenuAsync(inputLower, session),
            "invoice_item_qty" => HandleInvoiceItemQtyMenuAsync(inputLower, session),
            "invoice_item_price" => HandleInvoiceItemPriceMenuAsync(inputLower, session),
            "invoice_item_more" => await HandleInvoiceItemMoreMenuAsync(inputLower, session),
            "invoice_notes" => HandleInvoiceNotesMenuAsync(input, session),
            "invoice_date" => HandleInvoiceDateMenuAsync(input, session),
            "invoice_duedate" => await HandleInvoiceDueDateMenuAsync(input, companyId, session),
            "expense_category" => HandleExpenseCategoryMenuAsync(inputLower, session),
            "expense_description" => HandleExpenseDescriptionMenuAsync(input, session),
            "expense_amount" => HandleExpenseAmountMenuAsync(inputLower, session),
            "expense_date" => await HandleExpenseDateMenuAsync(input, companyId, session),
            "training" => await HandleTrainingMenuAsync(inputLower, companyId, session),
            "training_dog" => await HandleTrainingDogPickAsync(inputLower, companyId, session),
            "training_plan_dog" => await HandleTrainingPlanDogAsync(inputLower, companyId, session),
            "training_report_dog" => await HandleTrainingReportDogAsync(inputLower, companyId, session),
            "training_report_text" => await HandleTrainingReportTextAsync(input, companyId, session),
            "training_slot_pick" => await HandleTrainingSlotPickAsync(inputLower, companyId, session),
            "training_slot" => await HandleTrainingSlotAsync(inputLower, companyId, session),
            "training_duration" => HandleTrainingDuration(inputLower, session),
            "training_notes" => await HandleTrainingNotesAsync(input, companyId, session),
            "training_pay_pick" => await HandleTrainingPayPickAsync(inputLower, companyId, session),
            "training_pay_amount" => HandleTrainingPayAmount(inputLower, session),
            "training_pay_method" => await HandleTrainingPayMethodAsync(inputLower, companyId, session),
            "training_pay_field" => await HandleTrainingPayFieldAsync(input, companyId, session),
            _ => GetMainMenu()
        };

        _logger.LogInformation("✅ Sending response to {Phone}", phoneNumber);
        return response;
    }

    // ── Login ────────────────────────────────────────────────────────────────

    private async Task<string> HandleLoginFlowAsync(string input, string inputLower, UserSession session)
    {
        // An explicit greeting always restarts the flow. So does anything else sent
        // before the username prompt has been shown: a logged-out phone whose last
        // memory of the bot is a numbered menu will type "1", and that must be met
        // with the welcome message, not captured as a username.
        if (inputLower == "hi" || inputLower == "hello" || inputLower == "start" || !session.LoginPromptShown)
        {
            session.LoginStep = 0;
            session.LoginUsername = null;
            session.LoginPromptShown = true;
            return GetLoginWelcome(session);
        }

        if (session.LoginStep == 0)
        {
            // Expecting username
            session.LoginUsername = input;
            session.LoginStep = 1;
            return $@"✅ Username received: *{input}*

🔐 *Step 2:* Now enter your *password*";
        }

        if (session.LoginStep == 1 && !string.IsNullOrWhiteSpace(session.LoginUsername))
        {
            // Expecting password - verify credentials
            return await VerifyLoginAsync(session.LoginUsername, input, session);
        }

        session.LoginPromptShown = true;
        return GetLoginWelcome(session);
    }

    /// The logged-out landing message. Leads with a session-expired line when the
    /// user was dropped by the idle timeout, so a sudden logout mid-task doesn't
    /// look like the bot lost their work for no reason.
    private static string GetLoginWelcome(UserSession session)
    {
        var header = session.TimedOut
            ? $"⏳ *SESSION EXPIRED*\n\nYou were logged out after {WhatsAppSessionStore.IdleTimeout.TotalMinutes:0} minutes of inactivity."
            : "👋 *WELCOME TO DOG MANAGEMENT SYSTEM*";
        session.TimedOut = false;

        return $@"{header}

🔐 *Please login with your credentials*

📝 *Step 1:* Please enter your *username*";
    }

    private async Task<string> VerifyLoginAsync(string username, string password, UserSession session)
    {
        try
        {
            _logger.LogInformation("🔐 Login attempt for username: {Username}", username);

            // Reuse the exact same auth path the mobile app / portal use — real
            // credential check, active-status check, and real company/role data.
            var result = await _mediator.Send(new LoginCommand(new LoginRequestDto { Identifier = username, Password = password }));

            if (result?.Success != true || result.Data == null)
            {
                session.LoginStep = 0;
                session.LoginUsername = null;
                // Back to square one — the next message re-shows the welcome prompt
                // rather than being read as the username of a second attempt.
                session.LoginPromptShown = false;
                return $@"❌ *Invalid credentials*

{result?.Message ?? "Please check your username and password."}

Please try again. Type *hi* to login";
            }

            var data = result.Data;
            session.IsAuthenticated = true;
            session.AuthenticatedUsername = data.User.Name;
            session.AuthenticatedUserId = data.User.Id;
            session.AuthenticatedCompanyId = int.TryParse(data.User.CompanyId, out var cid) ? cid : 1;
            session.AuthenticatedIsHeadOffice = data.IsHeadOffice;
            session.AuthenticatedRole = data.User.Role;
            session.Permissions = new HashSet<string>(data.Permissions ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            session.CurrentMenu = "main";
            session.LoginStep = 0;
            session.LoginUsername = null;
            session.LoginPromptShown = false;
            session.TimedOut = false;

            _logger.LogInformation("✅ User logged in successfully: {Username} with CompanyId: {CompanyId}", username, session.AuthenticatedCompanyId);

            return $@"✅ *LOGIN SUCCESSFUL!*

Welcome back, *{data.User.Name}*! 🎉

{GetMainMenu()}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error occurred");
            await _errorLog.LogErrorAsync(ex, $"WhatsApp login exception for username {username}");
            session.LoginStep = 0;
            session.LoginUsername = null;
            session.LoginPromptShown = false;
            return @"❌ *Login failed*

Please try again. Type *hi* to login";
        }
    }

    // ── List-selection helper ───────────────────────────────────────────────

    /// Resolves a typed number back to the real (int) id it referred to in the
    /// last list shown to this user, or null if the input isn't a valid pick.
    private static int? ResolveListSelection(UserSession session, string input) =>
        int.TryParse(input, out var idx) && idx >= 1 && idx <= session.LastListIds.Count
            ? session.LastListIds[idx - 1]
            : null;

    private static string? ResolveUserListSelection(UserSession session, string input) =>
        int.TryParse(input, out var idx) && idx >= 1 && idx <= session.LastUserListIds.Count
            ? session.LastUserListIds[idx - 1]
            : null;

    // ── Main menu ────────────────────────────────────────────────────────────

    private async Task<string> HandleMainMenuAsync(string phoneNumber, string input, int companyId, string[] parts, UserSession session)
    {
        switch (input)
        {
            case "1":
                session.CurrentMenu = "branches";
                return await GetBranchesListAsync(companyId, session);
            case "2":
                session.CurrentMenu = "dogs";
                return await GetDogsListAsync(companyId, session);
            case "3":
                return await HandleRecordCommandAsync(parts, companyId, session);
            case "4":
                session.CurrentMenu = "users";
                return await GetUsersListAsync(companyId, session);
            case "5":
                session.CurrentMenu = "matings";
                return await GetMatingsListAsync(companyId);
            case "6":
                session.CurrentMenu = "family";
                return await GetFamilyTreeAsync(companyId);
            case "7":
                return await HandleCreateUserStartAsync(session);
            case "8":
            case "help":
                return GetHelpMenu(session);
            case "10":
            case "training":
                return StartTrainingMenu(session);
            case "9":
            case "logout":
                return HandleLogout(session);
            default:
                return GetMainMenu();
        }
    }

    private string GetMainMenu()
    {
        return @"🐾 *DOG MANAGEMENT SYSTEM*

Choose an option:

1️⃣  View *Branches*
2️⃣  View *Dogs*
3️⃣  *Record Care Activity* 🩺
4️⃣  View *Users & Handlers*
5️⃣  View *Mating Records*
6️⃣  View *Dog Family Tree*
7️⃣  *Create New User*
8️⃣  Show *Help*
9️⃣  *Logout*
🔟  *Dog Training* 🎓

*Reply with number (1-10) or type your command*

Type *0* or *exit* to return to main menu";
    }

    private string GetHelpMenu(UserSession? session = null)
    {
        var adminSection = session?.IsAdminLevel == true ? @"

👑 *Admin commands:*
*customers* - List customers, then *sites [number]* for their premises
*assign [dogNumber] [userNumber]* - Assign a dog to a handler
*newbranch [name] [code]* - Register a new branch under your company
*delete* - While viewing a branch or user, permanently removes it (asks to confirm)
*certificate [dogNumber]* - Generate & send a registration certificate PDF" : "";

        return $@"🐾 Dog Management Bot Commands:

*help* - Show this menu
*dogs* - List {(session?.IsDogHandlerOnly == true ? "your assigned dogs" : "all your dogs")}
*view [dogId]* or *detail [dogId]* - View dog details
*add [name] [breed]* - Add new dog
*record* - Record a care activity, step by step (or type option *3*)
*vaccine* - Show dogs needing vaccination
*vaccinate* - Record a vaccine given, step by step
*mating* - Record a mating, step by step
*litter* - Record a litter (and puppies), step by step
*deceased* - Mark a dog as deceased
*invoice* - Create a customer invoice, step by step
*expense* - Record an operating cost, step by step
*training* - Dog Training: your dogs, log today's training, summary, record a payment
*breeding* - Show breeding records
*status* - Company dashboard summary
*back* / *menu* - Return to main menu{adminSection}

Examples:
  view 1 - View dog #1 details
  record - Start recording care for one of your dogs
  vaccinate - Record a vaccine for one of your dogs
  add Buddy Labrador

Reply with any command!";
    }

    // ── Branches ─────────────────────────────────────────────────────────────

    private async Task<string> GetBranchesListAsync(int companyId, UserSession session)
    {
        var companyResp = await _mediator.Send(new GetCompanyByIdQuery(companyId));
        if (companyResp?.Success != true || companyResp.Data == null)
            return "❌ Could not load your company. Type *back* to return.";

        var self = companyResp.Data;
        var entries = new List<(int Id, string Line)> { (self.Id, $"{self.CompanyName} — {self.CompanyType}") };

        // Only a head office (no parent) has branches under it.
        if (self.ParentCompanyId == null)
        {
            var branchesResp = await _mediator.Send(new GetBranchesByCompanyQuery(companyId));
            if (branchesResp?.Success == true && branchesResp.Data != null)
                foreach (var b in branchesResp.Data)
                    entries.Add((b.Id, $"{b.CompanyName} — Branch"));
        }

        session.LastListIds = entries.Select(e => e.Id).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("🏢 *BRANCHES*");
        sb.AppendLine();
        for (var i = 0; i < entries.Count; i++)
            sb.AppendLine($"{i + 1}. {entries[i].Line}");
        sb.AppendLine();
        sb.AppendLine("*Reply with the number for details*");
        sb.Append("Type *back* to return");
        return sb.ToString();
    }

    private async Task<string> HandleBranchesMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "main";
            return GetMainMenu();
        }

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back*.";

        session.SelectedBranchId = selectedId;
        session.CurrentMenu = "branch_detail";
        return await GetBranchDetailsAsync(selectedId.Value);
    }

    private async Task<string> HandleBranchDetailAsync(string input, int companyId, UserSession session)
    {
        if (input == "delete" && session.SelectedBranchId.HasValue)
        {
            if (!session.IsAdminLevel)
                return "❌ Only an admin can delete a branch. Type *back* to return.";

            var targetResp = await _mediator.Send(new GetCompanyByIdQuery(session.SelectedBranchId.Value));
            if (targetResp?.Success != true || targetResp.Data == null)
                return "❌ Branch not found. Type *back* to return.";
            if (targetResp.Data.ParentCompanyId == null)
                return "❌ Your head office can't be deleted from here. Type *back* to return.";

            session.CurrentMenu = "branch_delete_confirm";
            return $@"⚠️ *DELETE BRANCH*

Are you sure you want to permanently delete *{targetResp.Data.CompanyName}*? This cannot be undone.

Reply *yes* to confirm, or *no* to cancel.";
        }

        // Any other reply returns to the branches list.
        session.CurrentMenu = "branches";
        return await GetBranchesListAsync(companyId, session);
    }

    private async Task<string> HandleBranchDeleteConfirmAsync(string input, int companyId, UserSession session)
    {
        var branchId = session.SelectedBranchId;
        session.CurrentMenu = "branches";
        session.SelectedBranchId = null;

        if (input != "yes" || branchId == null)
            return $"↩️ Deletion cancelled.\n\n{await GetBranchesListAsync(companyId, session)}";

        if (!session.IsAdminLevel)
            return $"❌ Only an admin can delete a branch.\n\n{await GetBranchesListAsync(companyId, session)}";

        var resp = await _mediator.Send(new DeleteCompanyCommand(branchId.Value));
        var result = resp?.Success == true
            ? "✅ Branch deleted."
            : $"❌ Could not delete branch: {resp?.Message ?? "Unknown error"}";

        return $"{result}\n\n{await GetBranchesListAsync(companyId, session)}";
    }

    private async Task<string> GetBranchDetailsAsync(int branchId)
    {
        var companyResp = await _mediator.Send(new GetCompanyByIdQuery(branchId));
        if (companyResp?.Success != true || companyResp.Data == null)
            return "❌ Branch not found. Type *back* to return.";
        var c = companyResp.Data;

        var dogsResp = await _mediator.Send(new GetAnimalsByCompanyQuery(branchId));
        var dogs = dogsResp?.Data ?? new List<AnimalListDto>();

        var usersResp = await _mediator.Send(new GetAllUsersQuery(branchId));
        var users = usersResp?.Data ?? new List<UserDto>();

        var matingsResp = await _mediator.Send(new GetMatingRecordsByCompanyQuery(branchId));
        var matings = matingsResp?.Data ?? new List<MatingRecordDto>();

        var sb = new StringBuilder();
        sb.AppendLine($"🏢 *{(c.CompanyName ?? "BRANCH").ToUpperInvariant()}*");
        sb.AppendLine();
        sb.AppendLine($"*Type:* {c.CompanyType}");
        if (!string.IsNullOrWhiteSpace(c.Location)) sb.AppendLine($"*Location:* {c.Location}");
        if (!string.IsNullOrWhiteSpace(c.PhoneNumber)) sb.AppendLine($"*Phone:* {c.PhoneNumber}");
        sb.AppendLine($"*Status:* {(c.IsActive ? "Active ✅" : "Inactive")}");
        sb.AppendLine();
        sb.AppendLine($"🐕 *Dogs:* {dogs.Count}");
        sb.AppendLine($"👥 *Users:* {users.Count}");
        sb.AppendLine($"❤️ *Matings:* {matings.Count}");

        if (dogs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Dogs (up to 5 shown):");
            foreach (var d in dogs.Take(5))
                sb.AppendLine($"  • {d.Name} ({d.Breed})");
        }
        if (users.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Users (up to 5 shown):");
            foreach (var u in users.Take(5))
                sb.AppendLine($"  • {u.Name} {u.Surname} ({u.RoleName ?? "—"})");
        }

        sb.AppendLine();
        if (c.ParentCompanyId != null)
            sb.AppendLine("Type *delete* to remove this branch (admin only)");
        sb.Append("Type *back* to return to branches");
        return sb.ToString();
    }

    // ── Dogs ─────────────────────────────────────────────────────────────────

    private async Task<string> GetDogsListAsync(int companyId, UserSession session)
    {
        // A Dog Handler's mobile app only shows dogs assigned to them — match that here.
        List<AnimalListDto> dogs;
        var isHandlerScoped = session.IsDogHandlerOnly && !string.IsNullOrWhiteSpace(session.AuthenticatedUserId);
        if (isHandlerScoped)
        {
            var assignedResp = await _mediator.Send(new GetAssignedDogsQuery(session.AuthenticatedUserId!));
            dogs = assignedResp?.Data ?? new List<AnimalListDto>();
        }
        else
        {
            var resp = await _mediator.Send(new GetAnimalsByCompanyQuery(companyId));
            dogs = resp?.Data ?? new List<AnimalListDto>();
        }
        session.LastListIds = dogs.Select(d => d.Id).ToList();

        var title = isHandlerScoped ? "🐕 *MY ASSIGNED DOGS*" : "🐕 *DOGS*";
        if (dogs.Count == 0)
            return isHandlerScoped
                ? $"{title}\n\nNo dogs assigned to you yet.\n\nType *back* to return."
                : $"{title}\n\nNo dogs recorded yet.\n\nType *back* to return.";

        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine();
        for (var i = 0; i < dogs.Count; i++)
        {
            var d = dogs[i];
            var status = d.IsDeployed ? "Deployed 📍" : (d.IsActive ? "Active ✅" : "Inactive");
            sb.AppendLine($"{i + 1}. {d.Name} ({d.Breed}) — {status}");
        }
        sb.AppendLine();
        sb.AppendLine("*Reply with a number for details, or type* record *to log care*");
        sb.Append("Type *back* to return");
        return sb.ToString();
    }

    private async Task<string> HandleDogsMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "main";
            return GetMainMenu();
        }

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back*.";

        session.SelectedDogId = selectedId;
        session.CurrentMenu = "dog_detail";
        return await GetDogDetailAsync(selectedId.Value);
    }

    private async Task<string> HandleDogDetailAsync(int companyId, UserSession session)
    {
        session.CurrentMenu = "dogs";
        return await GetDogsListAsync(companyId, session);
    }

    private async Task<string> GetDogDetailAsync(int dogId)
    {
        var resp = await _mediator.Send(new GetDogProfileQuery(dogId));
        if (resp?.Success != true || resp.Data == null)
            return "❌ Dog not found. Type *back* to return.";

        var d = resp.Data.Dog;
        var sb = new StringBuilder();
        sb.AppendLine($"🐕 *{(d.Name ?? "DOG").ToUpperInvariant()}*");
        sb.AppendLine();
        sb.AppendLine($"*Breed:* {d.Breed}");
        sb.AppendLine($"*Gender:* {d.Gender ?? "Unknown"}");
        sb.AppendLine($"*Color:* {d.Color}");
        sb.AppendLine($"*DOB:* {d.DateOfBirth:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(d.MicrochipNumber)) sb.AppendLine($"*Microchip:* {d.MicrochipNumber}");
        sb.AppendLine($"*Status:* {(d.IsActive ? "Active ✅" : "Inactive")}");
        if (d.IsDeployed) sb.AppendLine($"*Deployed to:* {d.DeployedTo ?? d.DeployedLocation ?? "—"}");

        sb.AppendLine();
        sb.AppendLine("👨‍👩‍👧‍👦 *Family:*");
        sb.AppendLine($"  Sire: {d.SireName ?? "Unknown"}");
        sb.AppendLine($"  Dam: {d.DamName ?? "Unknown"}");

        if (resp.Data.Vaccinations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("💉 *Vaccinations (up to 5 shown):*");
            foreach (var v in resp.Data.Vaccinations.OrderByDescending(v => v.VaccinationDate).Take(5))
            {
                var due = v.NextVaccinationDate.HasValue ? $" (next due {v.NextVaccinationDate:yyyy-MM-dd})" : "";
                sb.AppendLine($"  • {v.Name ?? "Vaccine"} — {v.VaccinationDate:yyyy-MM-dd}{due}");
            }
        }

        if (resp.Data.CurrentAssignment != null)
        {
            sb.AppendLine();
            sb.AppendLine("📋 *Current Assignment:*");
            sb.AppendLine($"  Handler: {resp.Data.CurrentAssignment.HandlerName ?? "—"}");
            if (!string.IsNullOrWhiteSpace(resp.Data.CurrentAssignment.BranchName))
                sb.AppendLine($"  Branch: {resp.Data.CurrentAssignment.BranchName}");
        }

        if (resp.Data.CurrentDeployment != null)
        {
            var dep = resp.Data.CurrentDeployment;
            sb.AppendLine();
            sb.AppendLine("📍 *Current Deployment:*");
            sb.AppendLine($"  Customer: {dep.CustomerName ?? dep.CompanyDeployedTo}");
            if (!string.IsNullOrWhiteSpace(dep.CustomerSiteName))
                sb.AppendLine($"  Site: {dep.CustomerSiteName}");
            var siteAddress = dep.CustomerSiteAddress ?? dep.Location;
            if (!string.IsNullOrWhiteSpace(siteAddress))
                sb.AppendLine($"  Address: {siteAddress}");
        }

        sb.AppendLine();
        sb.AppendLine("Type *record*, *vaccinate*, *mating*, *litter* or *deceased* for this dog");
        sb.Append("Type *back* to return to dogs");
        return sb.ToString();
    }

    // ── Users ────────────────────────────────────────────────────────────────

    private async Task<string> GetUsersListAsync(int companyId, UserSession session)
    {
        var resp = await _mediator.Send(new GetAllUsersQuery(companyId));
        var users = resp?.Data ?? new List<UserDto>();
        session.LastUserListIds = users.Select(u => u.Id).ToList();

        if (users.Count == 0)
            return "👥 *USERS & HANDLERS*\n\nNo users found for your company.\n\nType *back* to return.";

        var sb = new StringBuilder();
        sb.AppendLine("👥 *USERS & HANDLERS*");
        sb.AppendLine();
        for (var i = 0; i < users.Count; i++)
        {
            var u = users[i];
            sb.AppendLine($"{i + 1}. {u.Name} {u.Surname} ({u.RoleName ?? "—"})");
        }
        sb.AppendLine();
        sb.AppendLine("*Reply with a number for details*");
        sb.Append("Type *back* to return");
        return sb.ToString();
    }

    private async Task<string> HandleUsersMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "main";
            return GetMainMenu();
        }

        var selectedId = ResolveUserListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back*.";

        session.SelectedUserId = selectedId;
        session.CurrentMenu = "user_detail";
        return await GetUserDetailAsync(selectedId, session);
    }

    private async Task<string> HandleUserDetailAsync(string input, int companyId, UserSession session)
    {
        if (input == "delete" && !string.IsNullOrEmpty(session.SelectedUserId))
        {
            if (!session.IsAdminLevel)
                return "❌ Only an admin can delete a user. Type *back* to return.";
            if (session.SelectedUserId == session.AuthenticatedUserId)
                return "❌ You can't delete your own account. Type *back* to return.";

            var targetResp = await _mediator.Send(new GetUserByIdQuery(session.SelectedUserId));
            if (targetResp?.Success != true || targetResp.Data == null)
                return "❌ User not found. Type *back* to return.";

            session.CurrentMenu = "user_delete_confirm";
            return $@"⚠️ *DELETE USER*

Are you sure you want to permanently delete *{targetResp.Data.Name} {targetResp.Data.Surname}*? This cannot be undone.

Reply *yes* to confirm, or *no* to cancel.";
        }

        session.CurrentMenu = "users";
        return await GetUsersListAsync(companyId, session);
    }

    private async Task<string> HandleUserDeleteConfirmAsync(string input, int companyId, UserSession session)
    {
        var userId = session.SelectedUserId;
        session.CurrentMenu = "users";
        session.SelectedUserId = null;

        if (input != "yes" || string.IsNullOrEmpty(userId))
            return $"↩️ Deletion cancelled.\n\n{await GetUsersListAsync(companyId, session)}";

        if (!session.IsAdminLevel)
            return $"❌ Only an admin can delete a user.\n\n{await GetUsersListAsync(companyId, session)}";

        var resp = await _mediator.Send(new DeleteUserCommand(userId));
        var result = resp?.Success == true
            ? "✅ User deleted."
            : $"❌ Could not delete user: {resp?.Message ?? "Unknown error"}";

        return $"{result}\n\n{await GetUsersListAsync(companyId, session)}";
    }

    private async Task<string> GetUserDetailAsync(string userId, UserSession session)
    {
        var resp = await _mediator.Send(new GetUserByIdQuery(userId));
        if (resp?.Success != true || resp.Data == null)
            return "❌ User not found. Type *back* to return.";
        var u = resp.Data;

        var sb = new StringBuilder();
        sb.AppendLine($"👤 *{u.Name} {u.Surname}*".ToUpperInvariant());
        sb.AppendLine();
        sb.AppendLine($"*Role:* {u.RoleName ?? "—"}");
        if (!string.IsNullOrWhiteSpace(u.Email)) sb.AppendLine($"*Email:* {u.Email}");
        if (!string.IsNullOrWhiteSpace(u.PhoneNumber)) sb.AppendLine($"*Phone:* {u.PhoneNumber}");
        sb.AppendLine($"*Status:* {u.Status ?? "—"}");

        var assignedResp = await _mediator.Send(new GetAssignedDogsQuery(userId));
        var assigned = assignedResp?.Data ?? new List<AnimalListDto>();
        sb.AppendLine();
        sb.AppendLine($"🐕 *Dogs Assigned:* {assigned.Count}");
        foreach (var d in assigned.Take(5))
            sb.AppendLine($"  • {d.Name} ({d.Breed})");

        var activitiesResp = await _mediator.Send(new GetHandlerActivitiesQuery(userId));
        var activities = activitiesResp?.Data ?? new List<HandlerActivityDto>();
        if (activities.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("📊 *Recent Activity (up to 5 shown):*");
            foreach (var a in activities.Take(5))
                sb.AppendLine($"  • {a.EventType} — {a.DogName} ({EventTime.When(a.OccurredOn)})");
        }

        sb.AppendLine();
        if (userId != session.AuthenticatedUserId)
            sb.AppendLine("Type *delete* to remove this user (admin only)");
        sb.Append("Type *back* to return to users");
        return sb.ToString();
    }

    // ── Matings ──────────────────────────────────────────────────────────────

    private async Task<string> GetMatingsListAsync(int companyId)
    {
        var resp = await _mediator.Send(new GetMatingRecordsByCompanyQuery(companyId));
        var matings = resp?.Data ?? new List<MatingRecordDto>();

        if (matings.Count == 0)
            return "❤️ *MATING RECORDS*\n\nNo mating records yet.\n\nType *back* to return.";

        var sb = new StringBuilder();
        sb.AppendLine("❤️ *MATING RECORDS*");
        sb.AppendLine();
        var i = 1;
        foreach (var m in matings.OrderByDescending(m => m.MatingDate).Take(15))
        {
            sb.AppendLine($"{i}. {m.SireName ?? "Sire"} × {m.DamName ?? "Dam"}");
            sb.AppendLine($"   Date: {m.MatingDate:yyyy-MM-dd} | Litters: {m.Litters.Count}");
            i++;
        }
        sb.AppendLine();
        sb.Append("Type *back* to return");
        return sb.ToString();
    }

    private async Task<string> HandleMatingsMenuAsync(string input, int companyId, UserSession session)
    {
        session.CurrentMenu = "main";
        return GetMainMenu();
    }

    // ── Family tree ──────────────────────────────────────────────────────────

    private async Task<string> GetFamilyTreeAsync(int companyId)
    {
        var resp = await _mediator.Send(new GetAnimalsByCompanyQuery(companyId));
        var dogs = resp?.Data ?? new List<AnimalListDto>();
        var withFamily = dogs.Where(d => d.SireId.HasValue || d.DamId.HasValue).ToList();

        if (withFamily.Count == 0)
            return "👨‍👩‍👧‍👦 *DOG FAMILY TREES*\n\nNo recorded parentage yet.\n\nType *back* to return.";

        var sb = new StringBuilder();
        sb.AppendLine("👨‍👩‍👧‍👦 *DOG FAMILY TREES*");
        sb.AppendLine();
        foreach (var d in withFamily.Take(20))
        {
            sb.AppendLine($"• {d.Name} ({d.Breed})");
            sb.AppendLine($"   Sire: {d.SireName ?? "Unknown"} | Dam: {d.DamName ?? "Unknown"}");
        }
        sb.AppendLine();
        sb.Append("Type *back* to return");
        return sb.ToString();
    }

    private async Task<string> HandleFamilyMenuAsync(string input, int companyId, UserSession session)
    {
        session.CurrentMenu = "main";
        return GetMainMenu();
    }

    // ── Logout ───────────────────────────────────────────────────────────────

    private string HandleLogout(UserSession session)
    {
        session.IsAuthenticated = false;
        session.Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ClearTrainingFlow(session);
        session.CurrentMenu = "main";
        session.CreationStep = 0;
        session.CreatingUserFirstName = null;
        session.CreatingUserLastName = null;
        session.CreatingUserEmail = null;
        session.CreatingUserPhone = null;
        session.CreatingUserRole = null;
        session.CreatingUserBranchId = null;
        session.AuthenticatedUsername = null;
        session.AuthenticatedUserId = null;
        session.AuthenticatedCompanyId = null;
        session.AuthenticatedRole = null;
        session.AuthenticatedIsHeadOffice = false;
        // Back to a clean login: the next message gets the welcome prompt rather
        // than being read as a username.
        session.LoginStep = 0;
        session.LoginUsername = null;
        session.LoginPromptShown = false;
        session.TimedOut = false;

        return @"👋 *LOGGED OUT*

You have been successfully logged out.

Type *hi* to log back in.";
    }

    // ── Create user ──────────────────────────────────────────────────────────

    private async Task<string> HandleCreateUserStartAsync(UserSession session)
    {
        // Only an admin-level user can create accounts via the bot — see
        // UserSession.IsAdminLevel for why this guard matters here specifically.
        if (!session.IsAdminLevel)
            return "❌ Only an admin can create new users. Type *back* to return.";

        session.CurrentMenu = "create_user";
        session.CreationStep = 1;
        session.CreatingUserFirstName = null;
        session.CreatingUserLastName = null;
        session.CreatingUserEmail = null;
        session.CreatingUserPhone = null;
        session.CreatingUserRole = null;
        session.CreatingUserBranchId = null;

        return @"👤 *CREATE NEW USER*

Step 1 of 6: Enter first name

Example: John

Type first name or *back* to return";
    }

    private async Task<string> HandleCreateUserStepAsync(string phoneNumber, string input, int companyId, UserSession session)
    {
        var inputLower = input.ToLowerInvariant();
        if (inputLower == "back")
        {
            session.CurrentMenu = "main";
            session.CreationStep = 0;
            return GetMainMenu();
        }

        switch (session.CreationStep)
        {
            case 1: // First name
                session.CreatingUserFirstName = input;
                session.CreationStep = 2;
                return @"👤 *CREATE NEW USER*

Step 2 of 6: Enter last name

Example: Doe

Type last name or *back* to return";

            case 2: // Last name
                session.CreatingUserLastName = input;
                session.CreationStep = 3;
                return @"👤 *CREATE NEW USER*

Step 3 of 6: Enter email address

This will be their login username.

Example: john@example.com

Type email or *back* to return";

            case 3: // Email
                session.CreatingUserEmail = input;
                session.CreationStep = 4;
                return @"👤 *CREATE NEW USER*

Step 4 of 6: Enter phone number (optional)

Example: +263 78 893 7675

Type phone or type *skip* to continue";

            case 4: // Phone
                if (inputLower != "skip")
                    session.CreatingUserPhone = input;
                session.CreationStep = 5;
                return await ShowRoleSelectionAsync(session);

            case 5: // Role selection
                if (int.TryParse(input, out var roleIdx) && roleIdx >= 1 && roleIdx <= session.LastRoleNames.Count)
                {
                    session.CreatingUserRole = session.LastRoleNames[roleIdx - 1];
                    session.CreationStep = 6;
                    return await ShowBranchSelectionAsync(companyId, session);
                }
                return await ShowRoleSelectionAsync(session);

            case 6: // Branch selection
                if (int.TryParse(input, out var branchIdx) && branchIdx >= 1 && branchIdx <= session.LastListIds.Count)
                {
                    session.CreatingUserBranchId = session.LastListIds[branchIdx - 1];
                    return await CompleteUserCreationAsync(session);
                }
                return await ShowBranchSelectionAsync(companyId, session);

            case 7: // Confirmation
                if (inputLower == "yes")
                    return await SubmitUserCreationAsync(session);

                if (inputLower == "no")
                {
                    session.CreationStep = 0;
                    session.CurrentMenu = "main";
                    return @"❌ *USER CREATION CANCELLED*

The user was not created.

Returning to main menu...

" + GetMainMenu();
                }
                return @"Please reply *YES* to confirm or *NO* to cancel.";

            default:
                session.CreationStep = 0;
                session.CurrentMenu = "main";
                return GetMainMenu();
        }
    }

    private async Task<string> ShowRoleSelectionAsync(UserSession session)
    {
        // SystemOwner is always off-limits here; Admin is too unless the caller
        // themselves is a SystemOwner — matches the same rule enforced server-side
        // in CreateUserHandler and mirrored in the Portal/mobile role pickers.
        var callerIsSystemOwner = string.Equals(session.AuthenticatedRole, "SystemOwner", StringComparison.OrdinalIgnoreCase);
        var resp = await _mediator.Send(new GetAllRolesQuery());
        var roles = (resp?.Data ?? new List<RoleDto>())
            .Where(r => !string.Equals(r.Name, "SystemOwner", StringComparison.OrdinalIgnoreCase))
            .Where(r => callerIsSystemOwner || !string.Equals(r.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            .ToList();
        session.LastRoleNames = roles.Select(r => r.Name).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("👤 *CREATE NEW USER*");
        sb.AppendLine();
        sb.AppendLine("Step 5 of 6: Select role");
        sb.AppendLine();
        for (var i = 0; i < roles.Count; i++)
            sb.AppendLine($"{i + 1}️⃣  {roles[i].Name}");
        sb.AppendLine();
        sb.Append("Reply with the role number or *back*");
        return sb.ToString();
    }

    private async Task<string> ShowBranchSelectionAsync(int companyId, UserSession session)
    {
        var companyResp = await _mediator.Send(new GetCompanyByIdQuery(companyId));
        var entries = new List<(int Id, string Line)>();
        if (companyResp?.Success == true && companyResp.Data != null)
        {
            var self = companyResp.Data;
            entries.Add((self.Id, $"{self.CompanyName} — {self.CompanyType}"));
            if (self.ParentCompanyId == null)
            {
                var branchesResp = await _mediator.Send(new GetBranchesByCompanyQuery(companyId));
                if (branchesResp?.Success == true && branchesResp.Data != null)
                    foreach (var b in branchesResp.Data)
                        entries.Add((b.Id, $"{b.CompanyName} — Branch"));
            }
        }
        session.LastListIds = entries.Select(e => e.Id).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("🏢 *CREATE NEW USER*");
        sb.AppendLine();
        sb.AppendLine("Step 6 of 6: Select branch / company");
        sb.AppendLine();
        for (var i = 0; i < entries.Count; i++)
            sb.AppendLine($"{i + 1}️⃣  {entries[i].Line}");
        sb.AppendLine();
        sb.Append("Reply with the branch number or *back*");
        return sb.ToString();
    }

    private async Task<string> CompleteUserCreationAsync(UserSession session)
    {
        if (string.IsNullOrEmpty(session.CreatingUserFirstName) ||
            string.IsNullOrEmpty(session.CreatingUserLastName) ||
            string.IsNullOrEmpty(session.CreatingUserEmail) ||
            string.IsNullOrEmpty(session.CreatingUserRole) ||
            !session.CreatingUserBranchId.HasValue)
        {
            session.CreationStep = 0;
            session.CurrentMenu = "main";
            return @"❌ *ERROR*

User creation incomplete. Missing required fields.

Please try again by selecting *6* from main menu.

" + GetMainMenu();
        }

        var branchName = session.LastListIds.Contains(session.CreatingUserBranchId.Value)
            ? (await _mediator.Send(new GetCompanyByIdQuery(session.CreatingUserBranchId.Value))).Data?.CompanyName ?? "—"
            : "—";

        var confirmMessage = $@"✅ *USER CREATION SUMMARY*

👤 *Name:* {session.CreatingUserFirstName} {session.CreatingUserLastName}
📧 *Email (login):* {session.CreatingUserEmail}
📞 *Phone:* {(string.IsNullOrEmpty(session.CreatingUserPhone) ? "Not provided" : session.CreatingUserPhone)}
🔐 *Role:* {session.CreatingUserRole}
🏢 *Branch:* {branchName}

Reply *YES* to confirm or *NO* to cancel";

        session.CreationStep = 7; // Waiting for confirmation
        return confirmMessage;
    }

    private async Task<string> SubmitUserCreationAsync(UserSession session)
    {
        var tempPassword = GenerateTempPassword();
        var dto = new CreateUserDto
        {
            FirstName = session.CreatingUserFirstName!,
            LastName = session.CreatingUserLastName!,
            Email = session.CreatingUserEmail!,
            Password = tempPassword,
            PhoneNumber = session.CreatingUserPhone,
            RoleName = session.CreatingUserRole!,
            CompanyId = session.CreatingUserBranchId!.Value,
        };

        var callerIsSystemOwner = string.Equals(session.AuthenticatedRole, "SystemOwner", StringComparison.OrdinalIgnoreCase);
        var resp = await _mediator.Send(new CreateUserCommand(dto, callerIsSystemOwner));

        session.CreationStep = 0;
        session.CurrentMenu = "main";

        if (resp?.Success != true)
        {
            var reason = resp?.Message ?? "Unknown error";
            return $@"❌ *USER CREATION FAILED*

{reason}

Returning to main menu...

{GetMainMenu()}";
        }

        return $@"✅ *USER CREATED SUCCESSFULLY*

👤 *Name:* {session.CreatingUserFirstName} {session.CreatingUserLastName}
📧 *Login:* {session.CreatingUserEmail}
🔑 *Temporary password:* {tempPassword}

Share this password with them securely — they should change it after first login.

{GetMainMenu()}";
    }

    private static string GenerateTempPassword()
    {
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        var rnd = Random.Shared;
        var chars = new[]
        {
            upper[rnd.Next(upper.Length)],
            lower[rnd.Next(lower.Length)],
            lower[rnd.Next(lower.Length)],
            lower[rnd.Next(lower.Length)],
            digits[rnd.Next(digits.Length)],
            digits[rnd.Next(digits.Length)],
            digits[rnd.Next(digits.Length)],
            upper[rnd.Next(upper.Length)],
        };
        return new string(chars);
    }

    // ── Free-text power commands (help/add/record/view/vaccine/breeding/status) ─

    private async Task<string?> TryHandleTextCommandAsync(string phoneNumber, string inputLower, string[] parts, int companyId, UserSession session)
    {
        if (parts.Length == 0) return null;

        switch (parts[0])
        {
            case "dogs":
                session.CurrentMenu = "dogs";
                return await GetDogsListAsync(companyId, session);
            case "view":
            case "detail":
                return await ViewDogDetailAsync(parts, session);
            case "add":
                return await AddDogAsync(parts, companyId);
            case "record":
            case "care":
                return await HandleRecordCommandAsync(parts, companyId, session);
            case "vaccine":
                return await GetVaccineDueAsync(companyId);
            case "vaccinate":
            case "addvaccine":
                return await StartVaccineFlowAsync(parts, companyId, session);
            case "mating":
                return await StartMatingFlowAsync(parts, companyId, session);
            case "litter":
                return await StartLitterFlowAsync(parts, companyId, session);
            case "deceased":
            case "died":
                return await StartDeceasedFlowAsync(parts, companyId, session);
            case "invoice":
                return await StartInvoiceFlowAsync(companyId, session);
            case "expense":
                return StartExpenseFlowAsync(session);
            case "training":
                return StartTrainingMenu(session);
            case "breeding":
                return await GetBreedingStatusAsync(companyId);
            case "status":
                return await GetCompanyStatusAsync(companyId);
            case "customers":
                session.CurrentMenu = "customers";
                return await GetCustomersListAsync(companyId, session);
            case "sites":
                return await ViewCustomerSitesAsync(parts, session);
            case "assign":
                return await AssignDogToHandlerAsync(parts, session);
            case "newbranch":
                return await RegisterBranchAsync(parts, session);
            case "certificate":
                return await GenerateCertificateAsync(phoneNumber, parts, session);
            default:
                return null;
        }
    }

    private async Task<string> ViewDogDetailAsync(string[] parts, UserSession session)
    {
        if (parts.Length < 2)
            return "Usage: view [number] or detail [number]\nExample: type 'dogs' first to see the list, then 'view 1'";

        var dogId = ResolveDogReference(parts[1], session);
        if (dogId == null)
            return "❌ Not a valid dog number. Type 'dogs' to see the list first, then 'view [number]'.";

        return await GetDogDetailAsync(dogId.Value);
    }

    /// A typed number in a text command (view/record) means "position N in the
    /// dogs list I last showed you", matching what the numbered menu itself uses
    /// — falling back to a raw database id only if it isn't a valid list position,
    /// so power users who already know a dog's real id can still use it directly.
    private static int? ResolveDogReference(string token, UserSession session) =>
        ResolveListSelection(session, token) ?? (int.TryParse(token, out var raw) ? raw : null);

    private async Task<string> AddDogAsync(string[] parts, int companyId)
    {
        if (parts.Length < 3)
            return "Usage: add [name] [breed]\nExample: add Buddy Labrador";

        var name = parts[1];
        var breed = string.Join(" ", parts.Skip(2));

        var resp = await _mediator.Send(new CreateAnimalCommand(new CreateAnimalDto
        {
            Name = name,
            Breed = breed,
            Color = "Unknown",
            DateOfBirth = _clock.Today,
            CompanyId = companyId,
            IsActive = true,
        }));

        if (resp?.Success != true)
            return $"❌ Could not add dog: {resp?.Message ?? "Unknown error"}";

        return $"✅ Dog '{name}' ({breed}) added successfully!\n\nOpen the app to fill in full details (date of birth, color, microchip, etc).";
    }

    // ── Record Care ─────────────────────────────────────────────────────────
    //
    // Two ways in, same save path:
    //  1. Power-user one-liner: "record [number] [activity] [note...]" — kept
    //     for anyone who already knows the syntax.
    //  2. Guided flow: plain "record" (or menu option 3) walks a handler through
    //     picking a dog, picking an activity from a numbered list, typing a note,
    //     and a yes/no for follow-up — mirroring the mobile app's Record Care
    //     form without requiring them to remember any command syntax.

    private static readonly (string Label, string EventType)[] CareEventTypes =
    {
        ("Feeding", "Feeding"),
        ("Vaccinated", "Vaccinated"),
        ("Illness", "Illness"),
        ("Injured", "Injured"),
        ("Bite someone", "Bite someone"),
        ("Heat", "Heat"),
        ("Mating", "Mating"),
        ("Birth", "Birth"),
        ("Deworming", "Deworming"),
        ("Other", "Other"),
    };

    private async Task<string> HandleRecordCommandAsync(string[] parts, int companyId, UserSession session)
    {
        // "record 1 feeding good appetite" — full one-liner, save immediately.
        if (parts.Length >= 3)
        {
            var dogId = ResolveDogReference(parts[1], session);
            if (dogId == null)
                return "❌ Not a valid dog number. Type *record* on its own to pick a dog from a list instead.";

            var activityInput = parts[2];
            var note = string.Join(" ", parts.Skip(3));
            var eventType = MapActivityWord(activityInput);
            return await SaveCareRecordAsync(dogId.Value, companyId, eventType,
                string.IsNullOrWhiteSpace(note) ? null : note, requiresAttention: false, _clock.Now, session);
        }

        // "record 1" — dog given, still need the activity type.
        if (parts.Length == 2)
        {
            var dogId = ResolveDogReference(parts[1], session);
            if (dogId != null)
                return await StartRecordEventStepAsync(dogId.Value, session);
        }

        // Plain "record" while looking at a specific dog's detail — use that dog.
        if (session.CurrentMenu == "dog_detail" && session.SelectedDogId.HasValue)
            return await StartRecordEventStepAsync(session.SelectedDogId.Value, session);

        // Plain "record" from anywhere else — start by picking a dog.
        session.CurrentMenu = "record_dog";
        return await GetRecordDogListAsync(companyId, session);
    }

    private async Task<string> GetRecordDogListAsync(int companyId, UserSession session)
    {
        List<AnimalListDto> dogs;
        if (session.IsDogHandlerOnly && !string.IsNullOrWhiteSpace(session.AuthenticatedUserId))
        {
            var assignedResp = await _mediator.Send(new GetAssignedDogsQuery(session.AuthenticatedUserId));
            dogs = assignedResp?.Data ?? new List<AnimalListDto>();
        }
        else
        {
            var resp = await _mediator.Send(new GetAnimalsByCompanyQuery(companyId));
            dogs = resp?.Data ?? new List<AnimalListDto>();
        }

        session.LastListIds = dogs.Select(d => d.Id).ToList();

        if (dogs.Count == 0)
            return "🩺 *RECORD CARE*\n\nNo dogs available to record care for.\n\nType *back* to return.";

        var sb = new StringBuilder();
        sb.AppendLine("🩺 *RECORD CARE — Select a dog*");
        sb.AppendLine();
        for (var i = 0; i < dogs.Count; i++)
            sb.AppendLine($"{i + 1}. {dogs[i].Name} ({dogs[i].Breed})");
        sb.AppendLine();
        sb.AppendLine("*Reply with the dog's number*");
        sb.Append("Type *back* to cancel");
        return sb.ToString();
    }

    private async Task<string> HandleRecordDogMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "main";
            return GetMainMenu();
        }

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to cancel.";

        return await StartRecordEventStepAsync(selectedId.Value, session);
    }

    private Task<string> StartRecordEventStepAsync(int dogId, UserSession session)
    {
        session.RecordFlowDogId = dogId;
        session.CurrentMenu = "record_event";
        var sb = new StringBuilder();
        sb.AppendLine("🩺 *What happened?*");
        sb.AppendLine();
        for (var i = 0; i < CareEventTypes.Length; i++)
            sb.AppendLine($"{i + 1}. {CareEventTypes[i].Label}");
        sb.AppendLine();
        sb.AppendLine("*Reply with a number*");
        sb.Append("Type *back* to cancel");
        return Task.FromResult(sb.ToString());
    }

    private Task<string> HandleRecordEventMenuAsync(string input, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "main";
            session.RecordFlowDogId = null;
            return Task.FromResult(GetMainMenu());
        }

        if (!int.TryParse(input, out var idx) || idx < 1 || idx > CareEventTypes.Length)
            return Task.FromResult("❌ Not a valid option. Reply with a number from the list above, or type *back* to cancel.");

        session.RecordFlowEventType = CareEventTypes[idx - 1].EventType;
        session.CurrentMenu = "record_note";
        return Task.FromResult(
            $"📝 *{session.RecordFlowEventType}* — add a short note (or type *skip*)\n\nExample: \"good appetite\" or \"treated with antiseptic\"");
    }

    private Task<string> HandleRecordNoteMenuAsync(string input, UserSession session)
    {
        var trimmed = input.Trim();
        session.RecordFlowNote = string.Equals(trimmed, "skip", StringComparison.OrdinalIgnoreCase) || trimmed.Length == 0
            ? null
            : trimmed;
        session.CurrentMenu = "record_attention";
        return Task.FromResult("⚠️ Does this need attention or follow-up? Reply *yes* or *no*");
    }

    /// Last step of the guided care flow. Care is logged as it happens, so the date
    /// is taken silently from the clock rather than asked for — handlers were being
    /// made to confirm "today" on every single feeding. Back-dating a care record is
    /// still available from the portal.
    private async Task<string> HandleRecordAttentionMenuAsync(string input, int companyId, UserSession session)
    {
        if (input != "yes" && input != "y" && input != "no" && input != "n")
            return "❌ Please reply *yes* or *no*.";

        var requiresAttention = input == "yes" || input == "y";

        var dogId = session.RecordFlowDogId;
        var eventType = session.RecordFlowEventType;
        var note = session.RecordFlowNote;

        session.RecordFlowDogId = null;
        session.RecordFlowEventType = null;
        session.RecordFlowNote = null;
        session.CurrentMenu = "main";

        if (dogId == null || eventType == null)
            return "❌ Something went wrong — please type *record* to start again.\n\n" + GetMainMenu();

        // Full timestamp, not just the date — a care record stamped 00:00 reads as
        // if the dog was fed at midnight, and the portal treats a 00:00 record as
        // "time unknown" and hides the clock time entirely.
        return await SaveCareRecordAsync(dogId.Value, companyId, eventType, note, requiresAttention, _clock.Now, session);
    }

    private static string MapActivityWord(string activityInput) => activityInput.ToLowerInvariant() switch
    {
        "feeding" or "feed" => "Feeding",
        "vaccine" or "vaccinated" or "vaccination" => "Vaccinated",
        "illness" or "ill" or "sick" => "Illness",
        "injured" or "injury" => "Injured",
        "bite" => "Bite someone",
        "heat" => "Heat",
        "mating" or "mate" => "Mating",
        "birth" => "Birth",
        "litter" => "Litter",
        "deworming" or "deworm" => "Deworming",
        _ => "Other"
    };

    private async Task<string> SaveCareRecordAsync(int dogId, int companyId, string eventType, string? note, bool requiresAttention, DateTime occurredOn, UserSession session)
    {
        var resp = await _mediator.Send(new AddDogCareRecordCommand(new AddDogCareRecordDto
        {
            DogId = dogId,
            CompanyId = companyId,
            EventType = eventType,
            OccurredOn = occurredOn,
            Details = note,
            RequiresAttention = requiresAttention,
            RecordedByUserId = session.AuthenticatedUserId,
        }));

        if (resp?.Success != true)
            return $"❌ Could not record activity: {resp?.Message ?? "Unknown error"}\n\n" + GetMainMenu();

        var sb = new StringBuilder();
        // Same wording the portal timeline uses, so the handler sees one answer to
        // "when did I log that?" whichever screen they check it on.
        sb.AppendLine($"✅ *{eventType}* recorded for dog #{dogId} on {EventTime.When(occurredOn)}");
        if (!string.IsNullOrWhiteSpace(note)) sb.AppendLine($"Note: {note}");
        if (requiresAttention) sb.AppendLine("⚠️ Flagged for attention/follow-up");
        sb.AppendLine();
        sb.Append(GetMainMenu());
        return sb.ToString();
    }

    // ── Shared: pick a dog from a numbered list ────────────────────────────────

    private async Task<string> GetGenericDogListAsync(string title, int companyId, UserSession session)
    {
        var resp = await _mediator.Send(new GetAnimalsByCompanyQuery(companyId));
        var dogs = resp?.Data ?? new List<AnimalListDto>();
        session.LastListIds = dogs.Select(d => d.Id).ToList();

        if (dogs.Count == 0)
            return $"{title}\n\nNo dogs available.\n\nType *back* to cancel.";

        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine();
        for (var i = 0; i < dogs.Count; i++)
            sb.AppendLine($"{i + 1}. {dogs[i].Name} ({dogs[i].Breed})");
        sb.AppendLine();
        sb.AppendLine("*Reply with the dog's number*");
        sb.Append("Type *back* to cancel");
        return sb.ToString();
    }

    // ── Shared: "when did this happen?" date step ───────────────────────────────
    // The flows that record a dated fact rather than a live one (vaccine, mating,
    // litter, deceased, invoice, expense) still ask, since those are routinely
    // entered after the event and back-dating matters. Care records don't — they
    // are logged as they happen, so the clock is read silently instead.

    private string DatePromptText(string label = "Date") =>
        $"📅 *{label}?* Type a date as YYYY-MM-DD to back-date it, or reply *ok* to use today ({_clock.Today:yyyy-MM-dd}).";

    private bool TryParseFlexibleDate(string input, out DateTime date) => TryParseFlexibleDate(input, _clock.Today, out date);

    private static bool TryParseFlexibleDate(string input, DateTime fallback, out DateTime date)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0 || trimmed.Equals("ok", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            date = fallback;
            return true;
        }
        return DateTime.TryParse(trimmed, out date);
    }

    private string CancelToMainMenu(UserSession session)
    {
        session.CurrentMenu = "main";
        session.RecordFlowDogId = null;
        session.RecordFlowEventType = null;
        session.RecordFlowNote = null;
        session.VaccineFlowDogId = null;
        session.VaccineFlowName = null;
        session.VaccineFlowNextDue = null;
        session.VaccineFlowNote = null;
        session.MatingFlowSubjectId = null;
        session.MatingFlowPartnerId = null;
        session.MatingFlowPartnerName = null;
        session.LitterFlowMatingId = null;
        session.LitterFlowLitterId = null;
        session.LitterFlowPuppiesTarget = 0;
        session.LitterFlowPuppiesAdded = 0;
        session.DeceasedFlowDogId = null;
        session.InvoiceFlowCustomerId = null;
        session.InvoiceFlowItems = new();
        session.InvoiceFlowCurrentDescription = null;
        session.InvoiceFlowCurrentQuantity = 0;
        session.InvoiceFlowNotes = null;
        session.InvoiceFlowInvoiceDate = null;
        session.ExpenseFlowCategory = null;
        session.ExpenseFlowDescription = null;
        session.ExpenseFlowAmount = 0;
        return GetMainMenu();
    }

    // ── Add Vaccine ──────────────────────────────────────────────────────────

    private async Task<string> StartVaccineFlowAsync(string[] parts, int companyId, UserSession session)
    {
        int? dogId = parts.Length >= 2 ? ResolveDogReference(parts[1], session) : null;
        dogId ??= session.CurrentMenu == "dog_detail" ? session.SelectedDogId : null;

        if (dogId != null)
        {
            session.VaccineFlowDogId = dogId;
            session.CurrentMenu = "vaccine_name";
            return "💉 *What vaccine was given?* (e.g. Rabies, DHPP)";
        }

        session.CurrentMenu = "vaccine_dog";
        return await GetGenericDogListAsync("💉 *ADD VACCINE — Select a dog*", companyId, session);
    }

    private async Task<string> HandleVaccineDogMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to cancel.";

        session.VaccineFlowDogId = selectedId;
        session.CurrentMenu = "vaccine_name";
        return "💉 *What vaccine was given?* (e.g. Rabies, DHPP)";
    }

    private string HandleVaccineNameMenuAsync(string input, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);

        session.VaccineFlowName = input.Trim();
        session.CurrentMenu = "vaccine_due";
        return "📅 *Next due date?* Reply YYYY-MM-DD, or type *skip*";
    }

    private string HandleVaccineDueMenuAsync(string input, UserSession session)
    {
        var trimmed = input.Trim();
        if (trimmed.Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);

        if (!trimmed.Equals("skip", StringComparison.OrdinalIgnoreCase))
        {
            if (!DateTime.TryParse(trimmed, out var dueDate))
                return "❌ Not a valid date. Reply YYYY-MM-DD (e.g. 2026-08-15), or type *skip*.";
            session.VaccineFlowNextDue = dueDate;
        }

        session.CurrentMenu = "vaccine_notes";
        return "📝 Any notes? (reaction, dosage, route) — or type *skip*";
    }

    private string HandleVaccineNotesMenuAsync(string input, UserSession session)
    {
        var trimmed = input.Trim();
        session.VaccineFlowNote = trimmed.Equals("skip", StringComparison.OrdinalIgnoreCase) || trimmed.Length == 0 ? null : trimmed;
        session.CurrentMenu = "vaccine_date";
        return DatePromptText("Date given");
    }

    private async Task<string> HandleVaccineDateMenuAsync(string input, int companyId, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (!TryParseFlexibleDate(input, out var vaccinationDate))
            return "❌ Not a valid date. Type YYYY-MM-DD, or reply *ok* to use today.";

        var dogId = session.VaccineFlowDogId;
        var name = session.VaccineFlowName;
        var nextDue = session.VaccineFlowNextDue;
        var note = session.VaccineFlowNote;

        session.VaccineFlowDogId = null;
        session.VaccineFlowName = null;
        session.VaccineFlowNextDue = null;
        session.VaccineFlowNote = null;
        session.CurrentMenu = "main";

        if (dogId == null)
            return "❌ Something went wrong — please type *vaccinate* to start again.\n\n" + GetMainMenu();

        var resp = await _mediator.Send(new AddVaccinationCommand(new AddVaccinationDto
        {
            DogId = dogId,
            CompanyId = companyId,
            Name = name,
            VaccinationDate = vaccinationDate,
            NextVaccinationDate = nextDue,
            Notes = note,
        }));

        if (resp?.Success != true)
            return $"❌ Could not save vaccination: {resp?.Message ?? "Unknown error"}\n\n" + GetMainMenu();

        var sb = new StringBuilder();
        sb.AppendLine($"✅ *{name}* vaccination recorded for dog #{dogId} on {vaccinationDate:yyyy-MM-dd}");
        if (nextDue.HasValue) sb.AppendLine($"Next due: {nextDue.Value:yyyy-MM-dd}");
        sb.AppendLine();
        sb.Append(GetMainMenu());
        return sb.ToString();
    }

    // ── Add Mating Record ────────────────────────────────────────────────────

    private async Task<string> StartMatingFlowAsync(string[] parts, int companyId, UserSession session)
    {
        int? dogId = parts.Length >= 2 ? ResolveDogReference(parts[1], session) : null;
        dogId ??= session.CurrentMenu == "dog_detail" ? session.SelectedDogId : null;

        if (dogId != null)
        {
            session.MatingFlowSubjectId = dogId;
            session.CurrentMenu = "mating_partner";
            return await GetGenericDogListAsync(
                "❤️ *ADD MATING — Select the partner dog*\n(or type *other* to enter an outside dog's name)",
                companyId, session);
        }

        session.CurrentMenu = "mating_dog";
        return await GetGenericDogListAsync("❤️ *ADD MATING — Select one of your dogs*", companyId, session);
    }

    private async Task<string> HandleMatingDogMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to cancel.";

        session.MatingFlowSubjectId = selectedId;
        session.CurrentMenu = "mating_partner";
        return await GetGenericDogListAsync(
            "❤️ *Select the partner dog*\n(or type *other* to enter an outside dog's name)",
            companyId, session);
    }

    private async Task<string> HandleMatingPartnerMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        if (input == "other")
        {
            session.CurrentMenu = "mating_partner_name";
            return "✍️ Type the partner dog's name";
        }

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, type *other*, or type *back* to cancel.";

        session.MatingFlowPartnerId = selectedId;
        session.CurrentMenu = "mating_date";
        return DatePromptText("Mating date");
    }

    private Task<string> HandleMatingPartnerNameMenuAsync(string input, UserSession session)
    {
        var trimmed = input.Trim();
        if (trimmed.Equals("back", StringComparison.OrdinalIgnoreCase)) return Task.FromResult(CancelToMainMenu(session));
        if (trimmed.Length == 0)
            return Task.FromResult("❌ Please type the partner dog's name, or *back* to cancel.");

        session.MatingFlowPartnerName = trimmed;
        session.CurrentMenu = "mating_date";
        return Task.FromResult(DatePromptText("Mating date"));
    }

    private async Task<string> HandleMatingDateMenuAsync(string input, int companyId, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (!TryParseFlexibleDate(input, out var matingDate))
            return "❌ Not a valid date. Type YYYY-MM-DD, or reply *ok* to use today.";

        return await SaveMatingAsync(companyId, matingDate, session);
    }

    private async Task<string> SaveMatingAsync(int companyId, DateTime matingDate, UserSession session)
    {
        var subjectId = session.MatingFlowSubjectId;
        var partnerId = session.MatingFlowPartnerId;
        var partnerName = session.MatingFlowPartnerName;

        session.MatingFlowSubjectId = null;
        session.MatingFlowPartnerId = null;
        session.MatingFlowPartnerName = null;
        session.CurrentMenu = "main";

        if (subjectId == null)
            return "❌ Something went wrong — please type *mating* to start again.\n\n" + GetMainMenu();

        var profileResp = await _mediator.Send(new GetDogProfileQuery(subjectId.Value));
        var subjectDog = profileResp?.Data?.Dog;
        if (subjectDog == null)
            return "❌ Could not find that dog. Please try again.\n\n" + GetMainMenu();

        var subjectIsMale = subjectDog.Gender?.Equals("Male", StringComparison.OrdinalIgnoreCase) == true;
        var dto = new AddMatingRecordDto { CompanyId = companyId, MatingDate = matingDate };
        if (subjectIsMale)
        {
            dto.SireId = subjectId;
            dto.DamId = partnerId;
            dto.DamName = partnerId == null ? partnerName : null;
        }
        else
        {
            dto.DamId = subjectId;
            dto.SireId = partnerId;
            dto.SireName = partnerId == null ? partnerName : null;
        }

        var resp = await _mediator.Send(new AddMatingRecordCommand(dto));
        if (resp?.Success != true)
            return $"❌ Could not save mating record: {resp?.Message ?? "Unknown error"}\n\n" + GetMainMenu();

        var partnerLabel = partnerId != null ? $"dog #{partnerId}" : partnerName;
        return $"✅ Mating record saved: {subjectDog.Name} × {partnerLabel}\n\n" + GetMainMenu();
    }

    // ── Add Litter (+ Puppies) ───────────────────────────────────────────────

    private async Task<string> StartLitterFlowAsync(string[] parts, int companyId, UserSession session)
    {
        int? dogId = parts.Length >= 2 ? ResolveDogReference(parts[1], session) : null;
        dogId ??= session.CurrentMenu == "dog_detail" ? session.SelectedDogId : null;

        if (dogId != null)
            return await ShowLitterMatingListAsync(dogId.Value, session);

        session.CurrentMenu = "litter_dog";
        return await GetGenericDogListAsync("👶 *ADD LITTER — Select the sire or dam*", companyId, session);
    }

    private async Task<string> HandleLitterDogMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to cancel.";

        return await ShowLitterMatingListAsync(selectedId.Value, session);
    }

    /// A dog's own mating records (from its profile) become the numbered list here —
    /// reusing session.LastListIds the same way every other numbered list does, just
    /// keyed to MatingRecord ids instead of dog ids.
    private async Task<string> ShowLitterMatingListAsync(int dogId, UserSession session)
    {
        var profileResp = await _mediator.Send(new GetDogProfileQuery(dogId));
        var matings = profileResp?.Data?.MatingRecords ?? new List<MatingRecordDto>();

        session.CurrentMenu = "litter_mating";
        session.LastListIds = matings.Select(m => m.Id).ToList();

        if (matings.Count == 0)
            return "👶 *ADD LITTER*\n\nThis dog has no mating records yet. Type *mating* to add one first.\n\nType *back* to cancel.";

        var sb = new StringBuilder();
        sb.AppendLine("👶 *Select the mating record for this litter*");
        sb.AppendLine();
        for (var i = 0; i < matings.Count; i++)
            sb.AppendLine($"{i + 1}. {matings[i].SireName ?? "Sire"} × {matings[i].DamName ?? "Dam"} ({matings[i].MatingDate:yyyy-MM-dd})");
        sb.AppendLine();
        sb.AppendLine("*Reply with a number*");
        sb.Append("Type *back* to cancel");
        return sb.ToString();
    }

    private string HandleLitterMatingMenuAsync(string input, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to cancel.";

        session.LitterFlowMatingId = selectedId;
        session.CurrentMenu = "litter_totalborn";
        return "🔢 *How many puppies were born in total?* Reply with a number";
    }

    private string HandleLitterTotalBornMenuAsync(string input, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        if (!int.TryParse(input, out var totalBorn) || totalBorn < 0)
            return "❌ Please reply with a whole number (0 or more), or type *back* to cancel.";

        session.LitterFlowTotalBorn = totalBorn;
        session.CurrentMenu = "litter_survived";
        return $"🔢 *How many survived?* (out of {totalBorn}) Reply with a number";
    }

    private async Task<string> HandleLitterSurvivedMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        if (!int.TryParse(input, out var survived) || survived < 0 || survived > session.LitterFlowTotalBorn)
            return $"❌ Please reply with a number from 0 to {session.LitterFlowTotalBorn}, or type *back* to cancel.";

        session.LitterFlowSurvivedCount = survived;
        session.CurrentMenu = "litter_date";
        return DatePromptText("Birth date");
    }

    private async Task<string> HandleLitterDateMenuAsync(string input, int companyId, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (!TryParseFlexibleDate(input, out var birthDate))
            return "❌ Not a valid date. Type YYYY-MM-DD, or reply *ok* to use today.";

        return await SaveLitterAsync(companyId, birthDate, session);
    }

    private async Task<string> SaveLitterAsync(int companyId, DateTime birthDate, UserSession session)
    {
        var matingId = session.LitterFlowMatingId;
        var totalBorn = session.LitterFlowTotalBorn;
        var survived = session.LitterFlowSurvivedCount;
        var died = totalBorn - survived;

        if (matingId == null)
        {
            CancelToMainMenu(session);
            return "❌ Something went wrong — please type *litter* to start again.\n\n" + GetMainMenu();
        }

        var addResp = await _mediator.Send(new AddLitterCommand(new AddLitterDto
        {
            MatingRecordId = matingId.Value,
            CompanyId = companyId,
            BirthDate = birthDate,
            TotalBorn = totalBorn,
            SurvivedCount = survived,
            DiedCount = died,
        }));

        if (addResp?.Success != true)
        {
            CancelToMainMenu(session);
            return $"❌ Could not save litter: {addResp?.Message ?? "Unknown error"}\n\n" + GetMainMenu();
        }

        // AddLitterCommand doesn't hand back the new litter's id, so find it by
        // matching the exact values just submitted (most recently created wins).
        var littersResp = await _mediator.Send(new GetLittersByCompanyQuery(companyId));
        var createdLitter = (littersResp?.Data ?? new List<LitterDto>())
            .Where(l => l.BirthDate.Date == birthDate && l.TotalBorn == totalBorn && l.SurvivedCount == survived && l.DiedCount == died)
            .OrderByDescending(l => l.Id)
            .FirstOrDefault();

        session.LitterFlowMatingId = null;

        if (createdLitter == null || survived == 0)
        {
            session.CurrentMenu = "main";
            session.LitterFlowTotalBorn = 0;
            session.LitterFlowSurvivedCount = 0;
            return $"✅ Litter recorded ({totalBorn} born, {survived} survived).\n\n" + GetMainMenu();
        }

        session.LitterFlowLitterId = createdLitter.Id;
        session.LitterFlowPuppiesAdded = 0;
        session.CurrentMenu = "litter_puppy_count";
        return $"✅ Litter recorded ({totalBorn} born, {survived} survived).\n\n" +
               $"🐶 How many puppies would you like to name now? Reply a number (0 to {survived}), or *0* to skip and add them later via the app";
    }

    private string HandleLitterPuppyCountMenuAsync(string input, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "main";
            session.LitterFlowLitterId = null;
            session.LitterFlowPuppiesTarget = 0;
            session.LitterFlowPuppiesAdded = 0;
            return GetMainMenu();
        }

        if (!int.TryParse(input, out var count) || count < 0 || count > session.LitterFlowSurvivedCount)
            return $"❌ Please reply with a number from 0 to {session.LitterFlowSurvivedCount}.";

        if (count == 0)
        {
            session.CurrentMenu = "main";
            session.LitterFlowLitterId = null;
            session.LitterFlowPuppiesTarget = 0;
            session.LitterFlowPuppiesAdded = 0;
            return "👍 Okay, no puppies added now.\n\n" + GetMainMenu();
        }

        session.LitterFlowPuppiesTarget = count;
        session.CurrentMenu = "litter_puppy_name";
        return "✍️ *Puppy 1 — name?*";
    }

    private string HandleLitterPuppyNameMenuAsync(string input, UserSession session)
    {
        var trimmed = input.Trim();
        if (trimmed.Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (trimmed.Length == 0)
            return "❌ Please type a name for this puppy.";

        session.LitterFlowCurrentPuppyName = trimmed;
        session.CurrentMenu = "litter_puppy_gender";
        return "1️⃣ Male\n2️⃣ Female\n\n*Reply with a number*";
    }

    private async Task<string> HandleLitterPuppyGenderMenuAsync(string input, int companyId, UserSession session)
    {
        var gender = input switch { "1" => "Male", "2" => "Female", _ => null };
        if (gender == null)
            return "❌ Reply *1* for Male or *2* for Female.";

        var litterId = session.LitterFlowLitterId;
        var name = session.LitterFlowCurrentPuppyName;
        session.LitterFlowCurrentPuppyName = null;

        if (litterId == null || name == null)
        {
            CancelToMainMenu(session);
            return "❌ Something went wrong — please type *litter* to start again.\n\n" + GetMainMenu();
        }

        var resp = await _mediator.Send(new AddPuppyCommand(new AddPuppyDto
        {
            LitterId = litterId.Value,
            CompanyId = companyId,
            Name = name,
            Gender = gender,
            Color = "Unknown",
            DateOfBirth = _clock.Today,
        }));

        var sb = new StringBuilder();
        sb.AppendLine(resp?.Success == true
            ? $"✅ Puppy *{name}* ({gender}) added."
            : $"❌ Could not add puppy {name}: {resp?.Message ?? "Unknown error"}");

        session.LitterFlowPuppiesAdded++;
        if (session.LitterFlowPuppiesAdded >= session.LitterFlowPuppiesTarget)
        {
            session.CurrentMenu = "main";
            session.LitterFlowLitterId = null;
            session.LitterFlowPuppiesTarget = 0;
            session.LitterFlowPuppiesAdded = 0;
            sb.AppendLine();
            sb.AppendLine("🎉 All puppies added! (Open the app to set color/microchip details.)");
            sb.AppendLine();
            sb.Append(GetMainMenu());
            return sb.ToString();
        }

        session.CurrentMenu = "litter_puppy_name";
        sb.AppendLine();
        sb.Append($"✍️ *Puppy {session.LitterFlowPuppiesAdded + 1} — name?*");
        return sb.ToString();
    }

    // ── Mark Dog Deceased ────────────────────────────────────────────────────

    private async Task<string> StartDeceasedFlowAsync(string[] parts, int companyId, UserSession session)
    {
        int? dogId = parts.Length >= 2 ? ResolveDogReference(parts[1], session) : null;
        dogId ??= session.CurrentMenu == "dog_detail" ? session.SelectedDogId : null;

        if (dogId != null)
            return await ShowDeceasedConfirmAsync(dogId.Value, session);

        session.CurrentMenu = "deceased_dog";
        return await GetGenericDogListAsync("🕊️ *MARK DECEASED — Select a dog*", companyId, session);
    }

    private async Task<string> HandleDeceasedDogMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to cancel.";

        return await ShowDeceasedConfirmAsync(selectedId.Value, session);
    }

    private async Task<string> ShowDeceasedConfirmAsync(int dogId, UserSession session)
    {
        var profileResp = await _mediator.Send(new GetDogProfileQuery(dogId));
        var dog = profileResp?.Data?.Dog;
        if (dog == null)
        {
            session.CurrentMenu = "main";
            return "❌ Could not find that dog.\n\n" + GetMainMenu();
        }

        if (dog.DateOfDeath.HasValue)
        {
            session.CurrentMenu = "main";
            return $"ℹ️ {dog.Name} is already marked as deceased.\n\n" + GetMainMenu();
        }

        session.DeceasedFlowDogId = dogId;
        session.CurrentMenu = "deceased_confirm";
        return $"🕊️ Mark *{dog.Name}* as deceased (today)? Reply *yes* or *no*";
    }

    private string HandleDeceasedConfirmMenuAsync(string input, UserSession session)
    {
        if (input != "yes" && input != "y" && input != "no" && input != "n")
            return "❌ Please reply *yes* or *no*.";

        if (input == "no" || input == "n")
        {
            session.DeceasedFlowDogId = null;
            session.CurrentMenu = "main";
            return "👍 Cancelled.\n\n" + GetMainMenu();
        }

        session.CurrentMenu = "deceased_date";
        return DatePromptText("Date of death");
    }

    private async Task<string> HandleDeceasedDateMenuAsync(string input, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (!TryParseFlexibleDate(input, out var dateOfDeath))
            return "❌ Not a valid date. Type YYYY-MM-DD, or reply *ok* to use today.";

        var dogId = session.DeceasedFlowDogId;
        session.DeceasedFlowDogId = null;
        session.CurrentMenu = "main";

        if (dogId == null)
            return "❌ Something went wrong — please type *deceased* to start again.\n\n" + GetMainMenu();

        var profileResp = await _mediator.Send(new GetDogProfileQuery(dogId.Value));
        var dog = profileResp?.Data?.Dog;
        if (dog == null)
            return "❌ Could not find that dog.\n\n" + GetMainMenu();

        // UpdateAnimalCommand overwrites every field, so the dog's current values must
        // be round-tripped — only DateOfDeath/IsActive actually change here.
        var resp = await _mediator.Send(new UpdateAnimalCommand(new UpdateAnimalDto
        {
            Id = dog.Id,
            Name = dog.Name,
            Breed = dog.Breed,
            Color = dog.Color,
            DateOfBirth = dog.DateOfBirth,
            DateOfDeath = dateOfDeath,
            Gender = dog.Gender,
            MicrochipNumber = dog.MicrochipNumber,
            PhotoUrl = dog.PhotoUrl,
            SireId = dog.SireId,
            DamId = dog.DamId,
            CompanyId = dog.CompanyId,
            CustomerId = dog.CustomerId,
            IsActive = false,
        }));

        if (resp?.Success != true)
            return $"❌ Could not update dog: {resp?.Message ?? "Unknown error"}\n\n" + GetMainMenu();

        return $"🕊️ *{dog.Name}* has been marked as deceased on {dateOfDeath:yyyy-MM-dd}.\n\n" + GetMainMenu();
    }

    // ── Add Invoice ──────────────────────────────────────────────────────────

    private async Task<string> StartInvoiceFlowAsync(int companyId, UserSession session)
    {
        session.InvoiceFlowItems = new();
        session.InvoiceFlowCustomerId = null;
        session.CurrentMenu = "invoice_customer";
        return await GetInvoiceCustomerListAsync(companyId, session);
    }

    private async Task<string> GetInvoiceCustomerListAsync(int companyId, UserSession session)
    {
        var resp = await _mediator.Send(new GetCustomerByCompanyQuery(companyId));
        var customers = resp?.Data ?? new List<CustomerDto>();
        session.LastListIds = customers.Select(c => c.Id).ToList();

        if (customers.Count == 0)
            return "🧾 *NEW INVOICE*\n\nNo customers yet. Add one via the app or portal first.\n\nType *back* to return.";

        var sb = new StringBuilder();
        sb.AppendLine("🧾 *NEW INVOICE — Select a customer*");
        sb.AppendLine();
        for (var i = 0; i < customers.Count; i++)
        {
            var c = customers[i];
            var name = string.IsNullOrWhiteSpace(c.FullName) ? $"{c.FirstName} {c.LastName}".Trim() : c.FullName;
            sb.AppendLine($"{i + 1}. {name}");
        }
        sb.AppendLine();
        sb.AppendLine("*Reply with the customer's number*");
        sb.Append("Type *back* to cancel");
        return sb.ToString();
    }

    private async Task<string> HandleInvoiceCustomerMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to cancel.";

        session.InvoiceFlowCustomerId = selectedId;
        session.CurrentMenu = "invoice_item_desc";
        return StartInvoiceItemDescPrompt(session);
    }

    private static string StartInvoiceItemDescPrompt(UserSession session)
    {
        var n = session.InvoiceFlowItems.Count + 1;
        return $"📝 *Item {n} — description?*\n(e.g. \"Boarding fee\", \"Training session\")\nType *dog* to bill for one of your dogs.";
    }

    private async Task<string> HandleInvoiceItemDescMenuAsync(string input, int companyId, UserSession session)
    {
        var trimmed = input.Trim();
        if (trimmed.Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);

        if (trimmed.Equals("dog", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("dogs", StringComparison.OrdinalIgnoreCase))
        {
            session.CurrentMenu = "invoice_item_pickdog";
            return await GetGenericDogListAsync("🐕 *Select a dog to bill for*", companyId, session);
        }

        if (trimmed.Length == 0)
            return "❌ Please type a description, or type *dog* to pick one of your dogs.";

        session.InvoiceFlowCurrentDescription = trimmed;
        session.CurrentMenu = "invoice_item_qty";
        return "🔢 *Quantity?* Reply with a number (e.g. 1)";
    }

    private string HandleInvoiceItemPickDogMenuAsync(string input, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "invoice_item_desc";
            return StartInvoiceItemDescPrompt(session);
        }

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back* to go back.";

        session.InvoiceFlowCurrentDescription = $"Dog #{selectedId}";
        session.CurrentMenu = "invoice_item_qty";
        return "🔢 *Quantity?* Reply with a number (e.g. 1)";
    }

    private string HandleInvoiceItemQtyMenuAsync(string input, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        if (!int.TryParse(input, out var qty) || qty < 1)
            return "❌ Please reply with a whole number of 1 or more, or type *back* to cancel.";

        session.InvoiceFlowCurrentQuantity = qty;
        session.CurrentMenu = "invoice_item_price";
        return "💰 *Unit price?* Reply with a number (e.g. 25 or 25.50)";
    }

    private string HandleInvoiceItemPriceMenuAsync(string input, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        if (!decimal.TryParse(input, out var price) || price < 0)
            return "❌ Please reply with a valid price (e.g. 25 or 25.50), or type *back* to cancel.";

        session.InvoiceFlowItems.Add(new CreateInvoiceItemDto
        {
            Description = session.InvoiceFlowCurrentDescription ?? "Item",
            Quantity = session.InvoiceFlowCurrentQuantity,
            UnitPrice = price,
        });
        session.InvoiceFlowCurrentDescription = null;
        session.InvoiceFlowCurrentQuantity = 0;
        session.CurrentMenu = "invoice_item_more";

        var sb = new StringBuilder();
        sb.AppendLine($"✅ Added: {session.InvoiceFlowItems[^1].Description} x{session.InvoiceFlowItems[^1].Quantity} @ {price:0.00}");
        sb.AppendLine();
        sb.Append("➕ Add another item? Reply *yes* or *no*");
        return sb.ToString();
    }

    private Task<string> HandleInvoiceItemMoreMenuAsync(string input, UserSession session)
    {
        if (input == "yes" || input == "y")
        {
            session.CurrentMenu = "invoice_item_desc";
            return Task.FromResult(StartInvoiceItemDescPrompt(session));
        }
        if (input == "no" || input == "n")
        {
            session.CurrentMenu = "invoice_notes";
            return Task.FromResult("📝 Any notes for this invoice? Type them now, or type *skip*");
        }
        return Task.FromResult("❌ Please reply *yes* or *no*.");
    }

    private string HandleInvoiceNotesMenuAsync(string input, UserSession session)
    {
        var trimmed = input.Trim();
        session.InvoiceFlowNotes = trimmed.Equals("skip", StringComparison.OrdinalIgnoreCase) || trimmed.Length == 0 ? null : trimmed;
        session.CurrentMenu = "invoice_date";
        return DatePromptText("Invoice date");
    }

    private string HandleInvoiceDateMenuAsync(string input, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (!TryParseFlexibleDate(input, out var invoiceDate))
            return "❌ Not a valid date. Type YYYY-MM-DD, or reply *ok* to use today.";

        session.InvoiceFlowInvoiceDate = invoiceDate;
        session.CurrentMenu = "invoice_duedate";
        var defaultDue = invoiceDate.AddDays(30);
        return $"📅 *Due date?* Type a date as YYYY-MM-DD, or reply *ok* to use {defaultDue:yyyy-MM-dd} (30 days after the invoice date).";
    }

    private async Task<string> HandleInvoiceDueDateMenuAsync(string input, int companyId, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);

        var invoiceDate = session.InvoiceFlowInvoiceDate ?? _clock.Today;
        if (!TryParseFlexibleDate(input, invoiceDate.AddDays(30), out var dueDate))
            return "❌ Not a valid date. Type YYYY-MM-DD, or reply *ok* to use the default.";

        var customerId = session.InvoiceFlowCustomerId;
        var items = session.InvoiceFlowItems;
        var notes = session.InvoiceFlowNotes;

        session.InvoiceFlowCustomerId = null;
        session.InvoiceFlowItems = new();
        session.InvoiceFlowNotes = null;
        session.InvoiceFlowInvoiceDate = null;
        session.CurrentMenu = "main";

        if (customerId == null || items.Count == 0)
            return "❌ Something went wrong — please type *invoice* to start again.\n\n" + GetMainMenu();

        var custResp = await _mediator.Send(new GetCustomerByIdQuery(customerId.Value));
        var customer = custResp?.Data;
        var customerName = customer == null
            ? null
            : (string.IsNullOrWhiteSpace(customer.FullName) ? $"{customer.FirstName} {customer.LastName}".Trim() : customer.FullName);

        var resp = await _mediator.Send(new CreateInvoiceCommand(new CreateInvoiceDto
        {
            CompanyId = companyId,
            CustomerId = customerId.Value.ToString(),
            CustomerName = customerName,
            CustomerEmail = customer?.Email,
            CustomerPhone = customer?.Cellphone,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            Notes = notes,
            Items = items,
        }));

        if (resp?.Success != true || resp.Data == null)
            return $"❌ Could not create invoice: {resp?.Message ?? "Unknown error"}\n\n" + GetMainMenu();

        var inv = resp.Data;
        var sb = new StringBuilder();
        sb.AppendLine($"✅ *Invoice {inv.InvoiceNumber} created!*");
        sb.AppendLine($"Customer: {customerName}");
        sb.AppendLine($"Items: {items.Count}");
        sb.AppendLine($"Total: {inv.TotalAmount:0.00}");
        sb.AppendLine($"Invoice date: {invoiceDate:yyyy-MM-dd}");
        sb.AppendLine($"Due: {inv.DueDate:yyyy-MM-dd}");
        sb.AppendLine();
        sb.Append(GetMainMenu());
        return sb.ToString();
    }

    // ── Add Expense ──────────────────────────────────────────────────────────

    private static readonly string[] ExpenseCategories = { "Food", "Vet", "Fuel", "Salaries", "Equipment", "Training", "Other" };

    private string StartExpenseFlowAsync(UserSession session)
    {
        session.CurrentMenu = "expense_category";
        var sb = new StringBuilder();
        sb.AppendLine("💰 *ADD EXPENSE — Category?*");
        sb.AppendLine();
        for (var i = 0; i < ExpenseCategories.Length; i++)
            sb.AppendLine($"{i + 1}. {ExpenseCategories[i]}");
        sb.AppendLine();
        sb.AppendLine("*Reply with a number*");
        sb.Append("Type *back* to cancel");
        return sb.ToString();
    }

    private string HandleExpenseCategoryMenuAsync(string input, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        if (!int.TryParse(input, out var idx) || idx < 1 || idx > ExpenseCategories.Length)
            return "❌ Not a valid option. Reply with a number from the list above, or type *back* to cancel.";

        session.ExpenseFlowCategory = ExpenseCategories[idx - 1];
        session.CurrentMenu = "expense_description";
        return "📝 *What was it for?* (e.g. \"Dog food restock\", \"Vaccination supplies\")";
    }

    private string HandleExpenseDescriptionMenuAsync(string input, UserSession session)
    {
        var trimmed = input.Trim();
        if (trimmed.Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (trimmed.Length == 0)
            return "❌ Please type a short description.";

        session.ExpenseFlowDescription = trimmed;
        session.CurrentMenu = "expense_amount";
        return "💵 *Amount?* Reply with a number (e.g. 45.50)";
    }

    private string HandleExpenseAmountMenuAsync(string input, UserSession session)
    {
        if (input == "back") return CancelToMainMenu(session);

        if (!decimal.TryParse(input, out var amount) || amount <= 0)
            return "❌ Please reply with a valid amount, or type *back* to cancel.";

        session.ExpenseFlowAmount = amount;
        session.CurrentMenu = "expense_date";
        return DatePromptText();
    }

    private async Task<string> HandleExpenseDateMenuAsync(string input, int companyId, UserSession session)
    {
        if (input.Trim().Equals("back", StringComparison.OrdinalIgnoreCase)) return CancelToMainMenu(session);
        if (!TryParseFlexibleDate(input, out var expenseDate))
            return "❌ Not a valid date. Type YYYY-MM-DD, or reply *ok* to use today.";

        var category = session.ExpenseFlowCategory;
        var description = session.ExpenseFlowDescription;
        var amount = session.ExpenseFlowAmount;

        session.ExpenseFlowCategory = null;
        session.ExpenseFlowDescription = null;
        session.ExpenseFlowAmount = 0;
        session.CurrentMenu = "main";

        if (category == null || description == null)
            return "❌ Something went wrong — please type *expense* to start again.\n\n" + GetMainMenu();

        var resp = await _mediator.Send(new CreateExpenseCommand(new CreateExpenseDto
        {
            CompanyId = companyId,
            Category = category,
            Description = description,
            Amount = amount,
            ExpenseDate = expenseDate,
        }));

        if (resp?.Success != true)
            return $"❌ Could not save expense: {resp?.Message ?? "Unknown error"}\n\n" + GetMainMenu();

        return $"✅ *{category}* expense recorded: {description} — {amount:0.00} on {expenseDate:yyyy-MM-dd}\n\n" + GetMainMenu();
    }

    private async Task<string> GetVaccineDueAsync(int companyId)
    {
        var resp = await _mediator.Send(new GetVaccineDueQuery(companyId, 30));
        var due = resp?.Data ?? new List<VaccineDueDto>();

        if (due.Count == 0)
            return "💉 *Vaccination Due Soon*\n\nNothing due in the next 30 days. 🎉";

        var sb = new StringBuilder();
        sb.AppendLine("💉 *Vaccination Due Soon:*");
        sb.AppendLine();
        foreach (var v in due.Take(15))
        {
            var flag = v.IsOverdue ? "🔴" : "🟡";
            var when = v.IsOverdue ? $"overdue by {Math.Abs(v.DaysUntilDue)}d" : $"due in {v.DaysUntilDue}d";
            sb.AppendLine($"{flag} {v.DogName} — {v.VaccineName} ({when})");
        }
        sb.AppendLine();
        sb.Append("Reply 'record [dogId] vaccine' to record a vaccination");
        return sb.ToString();
    }

    private async Task<string> GetBreedingStatusAsync(int companyId)
    {
        var matingsResp = await _mediator.Send(new GetMatingRecordsCountQuery(companyId));
        var littersResp = await _mediator.Send(new GetLittersCountQuery(companyId));

        var matings = ExtractCount(matingsResp?.Data);
        var litters = ExtractCount(littersResp?.Data);

        return $@"❤️ *Breeding Overview:*
📍 Matings: {matings}
👶 Litters: {litters}

Use the *4* menu option for the full mating list.";
    }

    private async Task<string> GetCompanyStatusAsync(int companyId)
    {
        var dogsResp = await _mediator.Send(new GetAnimalsByCompanyQuery(companyId));
        var dogs = dogsResp?.Data ?? new List<AnimalListDto>();

        var vaccinatedResp = await _mediator.Send(new GetVaccinatedDogsCountQuery(companyId));
        var littersResp = await _mediator.Send(new GetLittersCountQuery(companyId));
        var matingsResp = await _mediator.Send(new GetMatingRecordsCountQuery(companyId));

        return $@"📊 *Dashboard Summary:*
🐾 Total Dogs: {dogs.Count}
✅ Active: {dogs.Count(d => d.IsActive)}
📍 Deployed: {dogs.Count(d => d.IsDeployed)}
💉 Vaccinated: {ExtractCount(vaccinatedResp?.Data)}
👶 Litters: {ExtractCount(littersResp?.Data)}
❤️ Matings: {ExtractCount(matingsResp?.Data)}

Type a command for more details!";
    }

    private static int ExtractCount(object? data)
    {
        if (data == null) return 0;
        var json = JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
    }

    // ── WhatsApp send ────────────────────────────────────────────────────────

    public async Task SendMessageAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("📤 SendMessage called - Phone: {Phone}, Message length: {Length}", phoneNumber, message?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(_settings.AccessToken) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            _logger.LogWarning("⚠️ WhatsApp credentials not configured - AccessToken: {AccessToken}, PhoneNumberId: {PhoneId}",
                string.IsNullOrWhiteSpace(_settings.AccessToken) ? "MISSING" : "SET",
                string.IsNullOrWhiteSpace(_settings.PhoneNumberId) ? "MISSING" : "SET");
            await _errorLog.LogWarningAsync(
                $"WhatsApp send skipped — credentials not configured (AccessToken {(string.IsNullOrWhiteSpace(_settings.AccessToken) ? "MISSING" : "set")}, PhoneNumberId {(string.IsNullOrWhiteSpace(_settings.PhoneNumberId) ? "MISSING" : "set")})",
                "WhatsAppBotService");
            return;
        }

        try
        {
            var version = string.IsNullOrWhiteSpace(_settings.ApiVersion) ? "v25.0" : _settings.ApiVersion;
            var url = $"https://graph.facebook.com/{version}/{_settings.PhoneNumberId}/messages";
            _logger.LogInformation("🌐 WhatsApp API URL: {Url}", url);

            var normalizedPhone = NormalizePhone(phoneNumber);
            _logger.LogInformation("📱 Normalized phone: {Phone}", normalizedPhone);

            // Try sending as regular text message first (works for active conversations and test numbers)
            var success = await TrySendTextMessageAsync(url, normalizedPhone, message);
            
            // If text message fails, try template message fallback (for non-opted-in users)
            if (!success)
            {
                _logger.LogInformation("⚠️ Text message failed for {Phone}, attempting template message fallback", phoneNumber);
                await TrySendTemplateMessageAsync(url, normalizedPhone, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending WhatsApp message to {Phone}", phoneNumber);
            await _errorLog.LogErrorAsync(ex, $"WhatsApp send exception to {phoneNumber}");
        }
    }

    private async Task<bool> TrySendTextMessageAsync(string url, string normalizedPhone, string message)
    {
        try
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = normalizedPhone,
                type = "text",
                text = new { preview_url = false, body = message }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogInformation("📦 Sending text payload: {Payload}", jsonPayload);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content,
                Headers = { { "Authorization", $"Bearer {_settings.AccessToken}" } }
            };

            _logger.LogInformation("🚀 Posting text message to WhatsApp API...");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ WhatsApp text message sent successfully to {Phone}", normalizedPhone);
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ WhatsApp text message failed - StatusCode: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                
                // Check if error is due to opt-in requirement (error code 131047 or 131026)
                if (responseContent.Contains("131047") || responseContent.Contains("131026") || 
                    responseContent.Contains("user's number is part of an experiment") ||
                    responseContent.Contains("Receiver is not on allowed list"))
                {
                    _logger.LogInformation("📋 Phone {Phone} requires template message (not opted in)", normalizedPhone);
                    return false; // Will trigger template fallback
                }
                
                // Log other errors
                await _errorLog.LogErrorAsync(
                    $"WhatsApp text send failed to {normalizedPhone} — {response.StatusCode}: {responseContent}",
                    source: "WhatsAppBotService");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending text message to {Phone}", normalizedPhone);
            return false;
        }
    }

    private async Task<bool> TrySendTemplateMessageAsync(string url, string normalizedPhone, string message)
    {
        try
        {
            // Use a generic notification template
            // You need to create this template in Meta Business Manager first
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = normalizedPhone,
                type = "template",
                template = new
                {
                    name = "dog_notification", // Template name - must be created in Meta Business Manager
                    language = new { code = "en" },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new[]
                            {
                                new { type = "text", text = message }
                            }
                        }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogInformation("📦 Sending template payload to {Phone}", normalizedPhone);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content,
                Headers = { { "Authorization", $"Bearer {_settings.AccessToken}" } }
            };

            _logger.LogInformation("🚀 Posting template message to WhatsApp API...");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ WhatsApp template message sent successfully to {Phone}", normalizedPhone);
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ WhatsApp template message failed - StatusCode: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                await _errorLog.LogErrorAsync(
                    $"WhatsApp template send failed to {normalizedPhone} — {response.StatusCode}: {responseContent}",
                    source: "WhatsAppBotService");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending template message to {Phone}", normalizedPhone);
            await _errorLog.LogErrorAsync(ex, $"WhatsApp template exception to {normalizedPhone}");
            return false;
        }
    }

    // WhatsApp expects the number in international format without a leading '+'.
    private static string NormalizePhone(string phone)
    {
        var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
        return digits;
    }

    /// Uploads binary media (e.g. a generated PDF) to Meta's Cloud API and returns
    /// the resulting media id, or null on failure (logged either way).
    private async Task<string?> UploadMediaAsync(byte[] bytes, string fileName, string mimeType)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
            return null;

        try
        {
            var version = string.IsNullOrWhiteSpace(_settings.ApiVersion) ? "v25.0" : _settings.ApiVersion;
            var url = $"https://graph.facebook.com/{version}/{_settings.PhoneNumberId}/media";

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("whatsapp"), "messaging_product");
            content.Add(new StringContent(mimeType), "type");
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(fileContent, "file", fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            request.Headers.Add("Authorization", $"Bearer {_settings.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ WhatsApp media upload failed - StatusCode: {StatusCode}, Response: {Response}", response.StatusCode, body);
                await _errorLog.LogErrorAsync($"WhatsApp media upload failed — {response.StatusCode}: {body}", source: "WhatsAppBotService");
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception uploading WhatsApp media");
            await _errorLog.LogErrorAsync(ex, "WhatsApp media upload exception");
            return null;
        }
    }

    /// Sends a previously-uploaded media item as a WhatsApp "document" message.
    private async Task<bool> SendDocumentAsync(string phoneNumber, string mediaId, string fileName, string? caption)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
            return false;

        try
        {
            var version = string.IsNullOrWhiteSpace(_settings.ApiVersion) ? "v25.0" : _settings.ApiVersion;
            var url = $"https://graph.facebook.com/{version}/{_settings.PhoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = NormalizePhone(phoneNumber),
                type = "document",
                document = new { id = mediaId, filename = fileName, caption }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                Headers = { { "Authorization", $"Bearer {_settings.AccessToken}" } }
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ WhatsApp document send failed - StatusCode: {StatusCode}, Response: {Response}", response.StatusCode, body);
                await _errorLog.LogErrorAsync($"WhatsApp document send failed — {response.StatusCode}: {body}", source: "WhatsAppBotService");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending WhatsApp document to {Phone}", phoneNumber);
            await _errorLog.LogErrorAsync(ex, $"WhatsApp document send exception to {phoneNumber}");
            return false;
        }
    }

    private async Task<byte[]?> TryDownloadAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;
        try { return await _httpClient.GetByteArrayAsync(uri); }
        catch { return null; }
    }

    private async Task<string> GenerateCertificateAsync(string phoneNumber, string[] parts, UserSession session)
    {
        if (parts.Length < 2)
            return "Usage: certificate [number]\nExample: type 'dogs' first to see the list, then 'certificate 1'";

        var dogId = ResolveDogReference(parts[1], session);
        if (dogId == null)
            return "❌ Not a valid dog number. Type 'dogs' first to see the list.";

        var profileResp = await _mediator.Send(new GetDogProfileQuery(dogId.Value));
        if (profileResp?.Success != true || profileResp.Data == null)
            return "❌ Dog not found.";

        CompanyDto? company = null;
        var companyResp = await _mediator.Send(new GetCompanyByIdQuery(profileResp.Data.Dog.CompanyId));
        if (companyResp?.Success == true) company = companyResp.Data;

        var logo = await TryDownloadAsync(company?.Logo);
        var photo = await TryDownloadAsync(profileResp.Data.Dog.PhotoUrl);

        var pdfBytes = CertificatePdfBuilder.Build(profileResp.Data, company, logo, photo);
        var fileName = $"Certificate_{profileResp.Data.Dog.Name}.pdf";

        var mediaId = await UploadMediaAsync(pdfBytes, fileName, "application/pdf");
        if (mediaId == null)
            return "❌ Could not generate the certificate right now. Please try again shortly.";

        var sent = await SendDocumentAsync(phoneNumber, mediaId, fileName, $"Registration certificate for {profileResp.Data.Dog.Name}");
        return sent
            ? $"✅ Certificate for {profileResp.Data.Dog.Name} sent above! 📄"
            : "❌ Certificate generated but could not be delivered. Please try again.";
    }

    // ── Admin actions: assign dog, register branch, view customers/sites ───────

    private async Task<string> AssignDogToHandlerAsync(string[] parts, UserSession session)
    {
        if (!session.IsAdminLevel)
            return "❌ Only an admin can assign dogs to handlers.";
        if (parts.Length < 3)
            return "Usage: assign [dogNumber] [userNumber]\nType 'dogs' then 'users' first to see the lists, then e.g. 'assign 1 2'";

        var dogId = ResolveDogReference(parts[1], session);
        if (dogId == null)
            return "❌ Not a valid dog number. Type 'dogs' first to see the list.";

        var handlerUserId = ResolveUserListSelection(session, parts[2]);
        if (handlerUserId == null)
            return "❌ Not a valid user number. Type 'users' first to see the list.";

        var resp = await _mediator.Send(new AssignDogCommand(new AssignDogDto { DogId = dogId.Value, HandlerUserId = handlerUserId }));
        return resp?.Success == true
            ? "✅ Dog assigned to handler successfully!"
            : $"❌ Could not assign dog: {resp?.Message ?? "Unknown error"}";
    }

    private async Task<string> RegisterBranchAsync(string[] parts, UserSession session)
    {
        if (!session.IsAdminLevel)
            return "❌ Only an admin can register a new branch.";
        if (parts.Length < 3)
            return "Usage: newbranch [name] [code]\nExample: newbranch northbranch NB2";

        var name = parts[1];
        var code = parts[2].ToUpperInvariant();

        var companyId = session.AuthenticatedCompanyId ?? 1;
        var selfResp = await _mediator.Send(new GetCompanyByIdQuery(companyId));
        if (selfResp?.Success != true || selfResp.Data == null)
            return "❌ Could not load your own company to copy defaults from.";

        var self = selfResp.Data;
        // A branch's parent is always the ultimate head office, not another branch.
        var headOfficeId = self.ParentCompanyId ?? self.Id;

        var resp = await _mediator.Send(new CreateCompanyCommand(new CreateCompanyDto
        {
            CompanyName = name,
            CompanyCode = code,
            CountryId = self.CountryId,
            ProvinceId = self.ProvinceId,
            CityId = self.CityId,
            DefaultCurrencyId = self.DefaultCurrencyId,
            ParentCompanyId = headOfficeId,
            IsActive = true,
        }));

        return resp?.Success == true
            ? $"✅ Branch '{name}' ({code}) registered under your head office!\n\nCountry/province/city/currency were copied from your own company — edit them in the app if this branch is elsewhere."
            : $"❌ Could not register branch: {resp?.Message ?? "Unknown error"}";
    }

    private async Task<string> GetCustomersListAsync(int companyId, UserSession session)
    {
        var resp = await _mediator.Send(new GetCustomerByCompanyQuery(companyId));
        var customers = resp?.Data ?? new List<CustomerDto>();
        session.LastListIds = customers.Select(c => c.Id).ToList();

        if (customers.Count == 0)
            return "👤 *CUSTOMERS*\n\nNo customers yet.\n\nType *back* to return.";

        var sb = new StringBuilder();
        sb.AppendLine("👤 *CUSTOMERS*");
        sb.AppendLine();
        for (var i = 0; i < customers.Count; i++)
        {
            var c = customers[i];
            var name = string.IsNullOrWhiteSpace(c.FullName) ? $"{c.FirstName} {c.LastName}".Trim() : c.FullName;
            sb.AppendLine($"{i + 1}. {name}");
        }
        sb.AppendLine();
        sb.AppendLine("*Reply with a number to see their sites*");
        sb.Append("Type *back* to return");
        return sb.ToString();
    }

    private async Task<string> HandleCustomersMenuAsync(string input, int companyId, UserSession session)
    {
        if (input == "back")
        {
            session.CurrentMenu = "main";
            return GetMainMenu();
        }

        var selectedId = ResolveListSelection(session, input);
        if (selectedId == null)
            return "❌ Not a valid number from the list above. Reply with a number, or type *back*.";

        return await GetCustomerSitesTextAsync(selectedId.Value);
    }

    private async Task<string> ViewCustomerSitesAsync(string[] parts, UserSession session)
    {
        if (parts.Length < 2)
            return "Usage: sites [number]\nExample: type 'customers' first to see the list, then 'sites 1'";

        var customerId = ResolveListSelection(session, parts[1]) ?? (int.TryParse(parts[1], out var raw) ? raw : (int?)null);
        if (customerId == null)
            return "❌ Not a valid customer number. Type 'customers' first to see the list.";

        return await GetCustomerSitesTextAsync(customerId.Value);
    }

    private async Task<string> GetCustomerSitesTextAsync(int customerId)
    {
        var resp = await _mediator.Send(new GetSitesByCustomerQuery(customerId));
        var sites = resp?.Data ?? new List<CustomerSiteDto>();

        var sb = new StringBuilder();
        sb.AppendLine("📍 *SITES*");
        sb.AppendLine();
        if (sites.Count == 0)
        {
            sb.AppendLine("No sites recorded for this customer yet.");
        }
        else
        {
            foreach (var s in sites)
            {
                sb.AppendLine($"• {s.SiteName}");
                if (!string.IsNullOrWhiteSpace(s.Address)) sb.AppendLine($"  {s.Address}");
                if (s.Latitude.HasValue && s.Longitude.HasValue) sb.AppendLine($"  📌 {s.Latitude}, {s.Longitude}");
                if (!string.IsNullOrWhiteSpace(s.ContactPerson)) sb.AppendLine($"  Contact: {s.ContactPerson} {s.ContactPhone}");
            }
        }
        sb.AppendLine();
        sb.Append("Type *back* to return to customers");
        return sb.ToString();
    }
}

/// <summary>WhatsApp API configuration.</summary>
public class WhatsAppSettings
{
    public string? AccessToken { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? WebhookToken { get; set; }
    public string? ApiVersion { get; set; } = "v25.0";
}
