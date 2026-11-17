using System.Text;
using Dog.Application.Features.DogTraining.Commands;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.DogTraining.Queries;

namespace Dog.Api.Services;

/// <summary>
/// Dog Training on WhatsApp: handlers see their dogs and log today's training step by step; managers get the
/// pipeline summary; finance can record a payment. Every action is checked against the signed-in user's
/// training.* permissions (the bot calls the handlers directly, so this is the only gate).
/// </summary>
public partial class WhatsAppBotService
{
    // Same rule as the API: anyone holding one of these manages the whole operation, everyone else only their own dogs.
    private static readonly string[] TrainingManagerPermissions =
    {
        "training.manage.all", "training.handler.assign", "training.enrolment.create", "training.health.approve",
        "training.assessment.approve", "training.programme.manage"
    };

    private static bool Can(UserSession s, params string[] anyOf) => anyOf.Any(s.Permissions.Contains);

    private TrainingActor TrainingActorFor(UserSession s, int companyId)
        => new(s.AuthenticatedUserId ?? "", s.AuthenticatedUsername ?? "", companyId, TrainingManagerPermissions.Any(s.Permissions.Contains));

    private static readonly string[] TrainingPerms =
    {
        "training.log.view", "training.log.record", "training.dashboard.view", "training.invoice.view", "training.payment.record"
    };

    // ── Entry point: "training" command / main-menu option 10 ────────────────

    private string StartTrainingMenu(UserSession session)
    {
        if (!Can(session, TrainingPerms))
            return "🔒 You don't have access to *Dog Training*. Ask an administrator to grant your role the training permissions.\n\n" + GetMainMenu();

        var opts = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine("🎓 *DOG TRAINING*");
        sb.AppendLine();
        void Add(string key, string text) { opts.Add(key); sb.AppendLine($"{opts.Count}️⃣  {text}"); }
        if (Can(session, "training.log.view", "training.log.record")) Add("dogs", "My *training dogs* (week & day)");
        if (Can(session, "training.log.record")) Add("log", "*Log today's training* 📝");
        if (Can(session, "training.dashboard.view")) Add("summary", "Training *summary*");
        if (Can(session, "training.invoice.view")) Add(Can(session, "training.payment.record") ? "pay" : "invoices", Can(session, "training.payment.record") ? "*Record a payment* 💵" : "Unpaid *invoices*");
        sb.AppendLine();
        sb.AppendLine("*Reply with a number*, or type *0* for the main menu.");

        session.TrainingMenuOptions = opts;
        session.CurrentMenu = "training";
        return sb.ToString();
    }

    private async Task<string> HandleTrainingMenuAsync(string input, int companyId, UserSession session)
    {
        if (!int.TryParse(input, out var n) || n < 1 || n > session.TrainingMenuOptions.Count)
            return StartTrainingMenu(session);

        var actor = TrainingActorFor(session, companyId);
        switch (session.TrainingMenuOptions[n - 1])
        {
            case "dogs": return await TrainingDogsTextAsync(actor, session) + "\n\nType *training* for the training menu, or *0* for the main menu.";
            case "log": return await StartTrainingLogAsync(actor, session);
            case "summary": return await TrainingSummaryTextAsync(actor);
            case "pay":
            case "invoices": return await StartTrainingPaymentAsync(actor, session, canPay: session.TrainingMenuOptions[n - 1] == "pay");
            default: return StartTrainingMenu(session);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// Dogs that are actually in training now (handlers: only their own).
    private async Task<List<EnrollmentListDto>> ActiveTrainingDogsAsync(TrainingActor actor)
    {
        if (actor.CanManageAll)
        {
            var all = await _mediator.Send(new GetEnrollmentsQuery(actor, new EnrollmentFilter(Status: "InTraining,AwaitingFinalAssessment")));
            return all?.Data ?? new List<EnrollmentListDto>();
        }
        var mine = await _mediator.Send(new GetMyTrainingDogsQuery(actor));
        return (mine?.Data ?? new List<EnrollmentListDto>()).Where(d => d.Status is "InTraining" or "AwaitingFinalAssessment").ToList();
    }

    private async Task<string> TrainingDogsTextAsync(TrainingActor actor, UserSession session)
    {
        var dogs = await ActiveTrainingDogsAsync(actor);
        if (dogs.Count == 0) return "🐕 No dogs are in training for you right now.";

        var logged = (await _mediator.Send(new GetDailyLogsQuery(actor, null, _clock.Today, null, null)))?.Data?.Select(l => l.EnrollmentId).ToHashSet() ?? new HashSet<int>();
        var sb = new StringBuilder("🎓 *DOGS IN TRAINING*\n\n");
        for (var i = 0; i < dogs.Count; i++)
        {
            var d = dogs[i];
            sb.AppendLine($"{i + 1}. *{d.DogName}*{(string.IsNullOrWhiteSpace(d.DogBreed) ? "" : $" ({d.DogBreed})")}");
            sb.AppendLine($"   Week {d.CurrentWeek} · Day {d.DayNumber} of {d.DurationDays + d.ExtensionDays}" + (logged.Contains(d.Id) ? "  ✅ logged today" : "  ⏳ not logged yet"));
            if (actor.CanManageAll && !string.IsNullOrWhiteSpace(d.HandlerName)) sb.AppendLine($"   Handler: {d.HandlerName}");
        }
        return sb.ToString().TrimEnd();
    }

    // ── Log today's training (guided) ────────────────────────────────────────

    private async Task<string> StartTrainingLogAsync(TrainingActor actor, UserSession session)
    {
        var dogs = await ActiveTrainingDogsAsync(actor);
        if (dogs.Count == 0) return "🐕 No dogs are in training for you right now.\n\nType *0* for the main menu.";

        session.LastListIds = dogs.Select(d => d.Id).ToList();
        session.TrainingDogNames = dogs.Select(d => d.DogName).ToList();
        session.CurrentMenu = "training_dog";

        var sb = new StringBuilder("📝 *LOG TODAY'S TRAINING*\n\nWhich dog?\n\n");
        for (var i = 0; i < dogs.Count; i++)
            sb.AppendLine($"{i + 1}. *{dogs[i].DogName}* — Week {dogs[i].CurrentWeek}, Day {dogs[i].DayNumber}");
        sb.AppendLine("\nReply with the dog's number, or *back* to cancel.");
        return sb.ToString();
    }

    private async Task<string> HandleTrainingDogPickAsync(string input, int companyId, UserSession session)
    {
        var enrollmentId = ResolveListSelection(session, input);
        if (enrollmentId == null) return "❌ Please reply with a number from the list, or type *back* to cancel.";

        var actor = TrainingActorFor(session, companyId);
        var resp = await _mediator.Send(new GetDailyLogForDayQuery(actor, enrollmentId.Value, _clock.Today));
        if (resp?.Success != true || resp.Data == null)
            return $"❌ {resp?.Message ?? "Could not open that dog."}\n\nType *back* for the main menu.";

        session.TrainingLogDraft = resp.Data;
        session.TrainingSlots = resp.Data.Slots.Select(s => new LogSlotDto { Time = s.Time, Title = s.Title, Done = s.Done, Rating = s.Rating, Notes = s.Notes }).ToList();
        session.TrainingSlotIndex = 0;
        session.TrainingDogName = resp.Data.DogName;

        var head = $"🐕 *{resp.Data.DogName}* — Week {resp.Data.WeekNumber}, Day {resp.Data.DayNumber}\n" +
                   (resp.Data.Id > 0 ? "_Today already has a record — your answers will update it._\n" : "") + "\n";
        if (session.TrainingSlots.Count == 0)
        {
            session.CurrentMenu = "training_duration";
            return head + "No schedule is set for today.\n\n⏱ How many *minutes* of training today? (number, or *skip*)";
        }
        session.CurrentMenu = "training_slot";
        return head + "For each activity reply a *rating 0-10* if it was done, or *skip* if it wasn't.\n\n" + SlotPrompt(session);
    }

    private static string SlotPrompt(UserSession s)
    {
        var slot = s.TrainingSlots[s.TrainingSlotIndex];
        return $"⏰ *{slot.Time} — {slot.Title}*  ({s.TrainingSlotIndex + 1}/{s.TrainingSlots.Count})\nRating 0-10, or *skip*";
    }

    private string HandleTrainingSlot(string input, UserSession session)
    {
        var slot = session.TrainingSlots[session.TrainingSlotIndex];
        if (input is "skip" or "s" or "no" or "n")
        {
            slot.Done = false; slot.Rating = null;
        }
        else if (int.TryParse(input, out var r) && r is >= 0 and <= 10)
        {
            slot.Done = true; slot.Rating = r;
        }
        else return "❌ Reply a number from *0 to 10* (rating), or *skip*.\n\n" + SlotPrompt(session);

        session.TrainingSlotIndex++;
        if (session.TrainingSlotIndex < session.TrainingSlots.Count) return SlotPrompt(session);

        session.CurrentMenu = "training_duration";
        return "⏱ How many *minutes* of training today? (number, or *skip*)";
    }

    private string HandleTrainingDuration(string input, UserSession session)
    {
        if (input is "skip" or "s") session.TrainingMinutes = session.TrainingLogDraft?.DurationMinutes ?? 0;
        else if (int.TryParse(input, out var m) && m is >= 0 and <= 1440) session.TrainingMinutes = m;
        else return "❌ Reply the number of minutes (0-1440), or *skip*.";

        session.CurrentMenu = "training_notes";
        return "🗒 Any *notes* on the dog's behaviour or progress? Type your note, or *skip*.";
    }

    private async Task<string> HandleTrainingNotesAsync(string input, int companyId, UserSession session)
    {
        var d = session.TrainingLogDraft;
        if (d == null) { session.CurrentMenu = "main"; return GetMainMenu(); }

        var note = input.Trim().Equals("skip", StringComparison.OrdinalIgnoreCase) ? null : input.Trim();
        var resp = await _mediator.Send(new SaveDailyLogCommand(TrainingActorFor(session, companyId), new SaveDailyLogDto
        {
            EnrollmentId = d.EnrollmentId,
            TrainingDate = _clock.Today,
            DurationMinutes = session.TrainingMinutes,
            Slots = session.TrainingSlots,
            // Keep what was already on today's record; only the ratings, minutes and note come from WhatsApp.
            ActivitiesPerformed = d.ActivitiesPerformed,
            CommandsPracticed = d.CommandsPracticed,
            BehaviourObservations = note ?? d.BehaviourObservations,
            Challenges = d.Challenges,
            Improvements = d.Improvements,
            Recommendations = d.Recommendations,
            NextObjectives = d.NextObjectives,
        }));

        ClearTrainingFlow(session);
        if (resp?.Success != true || resp.Data == null)
            return $"❌ {resp?.Message ?? "Could not save today's training."}\n\n{StartTrainingMenu(session)}";

        var r = resp.Data;
        return $"✅ *Training recorded for {r.DogName}*\n" +
               $"Week {r.WeekNumber} · Day {r.DayNumber} · {r.DurationMinutes} min\n" +
               (r.AverageSlotRating.HasValue ? $"Average rating: *{r.AverageSlotRating}/10* — {r.Performance}\n" : $"Performance: {r.Performance}\n") +
               "\n📸 Add photos or video from the *mobile app* or the web portal.\n\n" + StartTrainingMenu(session);
    }

    private static void ClearTrainingFlow(UserSession s)
    {
        s.TrainingLogDraft = null; s.TrainingSlots = new(); s.TrainingSlotIndex = 0; s.TrainingMinutes = 0; s.TrainingDogName = null;
        s.TrainingInvoiceId = null; s.TrainingPayAmount = 0; s.TrainingPayDraft = null; s.TrainingPayQueue = new(); s.TrainingPayBankIds = new();
    }

    // ── Summary ──────────────────────────────────────────────────────────────

    private async Task<string> TrainingSummaryTextAsync(TrainingActor actor)
    {
        var resp = await _mediator.Send(new GetTrainingDashboardQuery(actor, null, null, null, null));
        if (resp?.Success != true || resp.Data == null) return "❌ Could not load the training summary.";
        var d = resp.Data;
        return $@"📊 *TRAINING SUMMARY*

🩺 Awaiting health assessment: *{d.AwaitingHealthAssessment}*
✅ Passed health assessment: *{d.HealthPassed}*
💵 Awaiting payment: *{d.AwaitingPayment}*
👤 Awaiting handler: *{d.AwaitingHandler}*
▶️ Ready to start: *{d.ReadyToActivate}*

🐕 In training now: *{d.InTraining}*
   Week 1: {d.Week1} · Week 2: {d.Week2} · Week 3: {d.Week3}
📋 Awaiting final assessment: *{d.AwaitingFinalAssessment}*
🔁 Needing additional training: *{d.RequiringAdditionalTraining}*
⏳ Awaiting approval: *{d.AwaitingApproval}*

🏆 Completed: *{d.Completed}*
🎓 Certificates issued: *{d.CertificatesIssued}*
📝 Weekly reports to review: *{d.ReportsAwaitingReview}*

Type *training* for the training menu, or *0* for the main menu.";
    }

    // ── Unpaid invoices / record a payment ───────────────────────────────────

    private async Task<string> StartTrainingPaymentAsync(TrainingActor actor, UserSession session, bool canPay)
    {
        var resp = await _mediator.Send(new GetTrainingInvoicesQuery(actor, null, null));
        var open = (resp?.Data ?? new List<TrainingInvoiceDto>()).Where(i => i.Status is "SENT" or "PARTIAL" && i.BalanceAmount > 0).ToList();
        if (open.Count == 0) return "✅ There are no unpaid training invoices.\n\nType *training* for the training menu.";

        var sb = new StringBuilder("💵 *UNPAID TRAINING INVOICES*\n\n");
        for (var i = 0; i < open.Count && i < 15; i++)
        {
            var inv = open[i];
            sb.AppendLine($"{i + 1}. *{inv.InvoiceNumber}* — {inv.CustomerName}");
            sb.AppendLine($"   Dog: {inv.DogName ?? "—"} · Total {inv.TotalAmount:0.00} · Paid {inv.PaidAmount:0.00} · *Due {inv.BalanceAmount:0.00}*");
        }
        if (!canPay) return sb.ToString().TrimEnd() + "\n\nType *training* for the training menu.";

        session.LastListIds = open.Take(15).Select(i => i.Id).ToList();
        session.CurrentMenu = "training_pay_pick";
        return sb.ToString() + "\nReply with the invoice number to record a payment, or *back* to cancel.";
    }

    private async Task<string> HandleTrainingPayPickAsync(string input, int companyId, UserSession session)
    {
        var id = ResolveListSelection(session, input);
        if (id == null) return "❌ Reply with a number from the list, or type *back* to cancel.";
        if (!Can(session, "training.payment.record")) return "🔒 You don't have permission to record payments.";

        var inv = (await _mediator.Send(new GetTrainingInvoiceByIdQuery(id.Value)))?.Data;
        if (inv == null) return "❌ Could not open that invoice.";
        session.TrainingInvoiceId = inv.Id;
        session.TrainingPayAmount = inv.BalanceAmount;
        session.CurrentMenu = "training_pay_amount";
        return $"💵 *{inv.InvoiceNumber}* — {inv.CustomerName}\nBalance due: *{inv.BalanceAmount:0.00}*\n\nReply the *amount received*, or *full* for the whole balance.";
    }

    private string HandleTrainingPayAmount(string input, UserSession session)
    {
        decimal amount;
        if (input == "full") amount = session.TrainingPayAmount;
        else if (!decimal.TryParse(input, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out amount) || amount <= 0)
            return "❌ Reply an amount like *50* or *25.50*, or *full*.";
        if (amount > session.TrainingPayAmount) return $"❌ That is more than the balance due ({session.TrainingPayAmount:0.00}). Reply a smaller amount.";

        session.TrainingPayAmount = amount;
        session.CurrentMenu = "training_pay_method";
        return $"Amount: *{amount:0.00}*\n\nHow was it paid?\n1️⃣ Cash\n2️⃣ EcoCash\n3️⃣ OneMoney\n4️⃣ Bank transfer\n5️⃣ Card / POS\n6️⃣ Cheque";
    }

    private async Task<string> HandleTrainingPayMethodAsync(string input, int companyId, UserSession session)
    {
        var method = input switch { "1" => "CASH", "2" => "ECOCASH", "3" => "ONEMONEY", "4" => "BANK", "5" => "CARD", "6" => "CHEQUE", _ => null };
        if (method == null) return "❌ Reply a number from *1* to *6*.";
        if (session.TrainingInvoiceId == null) { session.CurrentMenu = "main"; return GetMainMenu(); }

        var info = Dog.Application.Common.Payments.PaymentRules.Find(method)!;
        session.TrainingPayDraft = new RecordTrainingPaymentDto { InvoiceId = session.TrainingInvoiceId.Value, Amount = session.TrainingPayAmount, Method = method, Notes = "Recorded via WhatsApp" };
        session.TrainingPayQueue = info.Required.Where(k => k != "notes").ToList();
        if (method == "BANK")
        {
            var pdf = (await _mediator.Send(new Dog.Application.Features.BillingSetup.GetPdfInfoQuery(companyId)))?.Data;
            session.TrainingPayBankIds = pdf?.BankAccounts.Select(b => b.Id).ToList() ?? new();
            if (session.TrainingPayBankIds.Count == 0) { ClearTrainingFlow(session); return "❌ No bank accounts are set up for this company yet. Add one in the portal first.\n\n" + StartTrainingMenu(session); }
        }
        session.CurrentMenu = "training_pay_field";
        return await NextTrainingPayPromptAsync(companyId, session, $"*{info.Label}* payment — I need a few details so the money can be traced.\n\n");
    }

    private async Task<string> NextTrainingPayPromptAsync(int companyId, UserSession session, string prefix = "")
    {
        if (session.TrainingPayQueue.Count == 0) return await FinishTrainingPaymentAsync(companyId, session);
        var key = session.TrainingPayQueue[0];
        var text = key switch
        {
            "payerName" => "Who paid? Reply the *payer's name*.",
            "payerContact" => "Reply the payer's *mobile number* (the one the money came from).",
            "payerBank" => "Reply the *name of the payer's bank*.",
            "reference" => session.TrainingPayDraft?.Method == "BANK" ? "Reply the *bank reference* shown on the transfer." : "Reply the *transaction code / slip number*.",
            "cheque" => "Reply the *cheque number*.",
            "valueDate" => "Reply the *payment date* as yyyy-mm-dd, or *today*.",
            "destinationAccount" => "Which of OUR accounts received it? Reply the number:\n" + string.Join("\n", await BankLinesAsync(companyId)),
            _ => "Reply the value for " + key,
        };
        return prefix + text + "\n\n_Reply *back* to cancel._";
    }

    private async Task<List<string>> BankLinesAsync(int companyId)
    {
        var pdf = (await _mediator.Send(new Dog.Application.Features.BillingSetup.GetPdfInfoQuery(companyId)))?.Data;
        return pdf?.BankAccounts.Select((b, i) => $"{i + 1}. {b.Label}").ToList() ?? new();
    }

    private async Task<string> HandleTrainingPayFieldAsync(string input, int companyId, UserSession session)
    {
        var d = session.TrainingPayDraft;
        if (d == null || session.TrainingPayQueue.Count == 0) { ClearTrainingFlow(session); session.CurrentMenu = "main"; return GetMainMenu(); }
        var value = (input ?? "").Trim();
        if (value.Length == 0) return "❌ Please reply with a value.";
        var key = session.TrainingPayQueue[0];
        switch (key)
        {
            case "payerName": d.PayerName = value; break;
            case "payerContact": d.PayerContact = value; break;
            case "payerBank": d.PayerBank = value; break;
            case "reference": d.ReferenceNumber = value; break;
            case "cheque": d.ChequeNumber = value; break;
            case "valueDate":
                if (value.Equals("today", StringComparison.OrdinalIgnoreCase)) d.ValueDate = DateTime.Today;
                else if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt)) d.ValueDate = dt;
                else return "❌ Reply the date as *yyyy-mm-dd* (e.g. 2026-11-17) or *today*.";
                break;
            case "destinationAccount":
                if (!int.TryParse(value, out var n) || n < 1 || n > session.TrainingPayBankIds.Count) return "❌ Reply one of the account numbers in the list.";
                d.DestinationBankAccountId = session.TrainingPayBankIds[n - 1]; break;
        }
        session.TrainingPayQueue.RemoveAt(0);
        return await NextTrainingPayPromptAsync(companyId, session);
    }

    private async Task<string> FinishTrainingPaymentAsync(int companyId, UserSession session)
    {
        var dto = session.TrainingPayDraft!;
        var resp = await _mediator.Send(new RecordTrainingPaymentCommand(TrainingActorFor(session, companyId), dto));
        ClearTrainingFlow(session);

        if (resp?.Success != true || resp.Data == null)
            return $"❌ {resp?.Message ?? "Could not record the payment."}\n\n{StartTrainingMenu(session)}";

        var inv = resp.Data;
        var last = inv.Payments.LastOrDefault();
        var pending = last?.Status == "PENDING";
        return $"✅ *Payment recorded*{(last != null ? $" — receipt {last.PaymentNumber}" : "")}\n{inv.InvoiceNumber}: paid {inv.PaidAmount:0.00} of {inv.TotalAmount:0.00}\n" +
               (pending ? "⏳ It is *pending* until it is confirmed on the bank statement / cheque clears — then the balance updates." :
                inv.Status == "PAID" ? "🎉 Paid in full — the dog can now be allocated a handler and start training." : $"Balance still due: *{inv.BalanceAmount:0.00}*") +
               "\n\n" + StartTrainingMenu(session);
    }
}
