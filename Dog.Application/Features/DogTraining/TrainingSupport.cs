using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dog.Application.Features.DogTraining
{
    /// <summary>Shared rules and lookups for the Dog Training module.</summary>
    internal static class TrainingSupport
    {
        public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // ── Form templates (from the BES "Health Check Form – Dog" and the final-assessment brief) ──
        public static readonly string[] ExamItems =
        {
            "Eyes", "Ears", "Teeth", "Mucous membranes", "Heart", "Body score", "Lungs",
            "Musculoskeletal system", "Lymph nodes", "Skin", "Other", "General fitness", "Behaviour & temperament"
        };

        public static readonly string[] TreatmentItems =
        {
            "External parasites", "Internal parasites", "Deworming", "Antibiotic injections", "Supplements",
            "Food type / brand", "Injured", "Mating", "Heat", "Feeding time", "Dipping", "Kennel clean"
        };

        public static readonly string[] AssessmentAreas =
        {
            "Obedience", "Sit", "Stay", "Recall", "Heel", "Leash control", "Socialisation", "Behaviour", "Responsiveness", "Specialised skills"
        };

        public static readonly string[] PerformanceLevels = { "Excellent", "Very Good", "Good", "Needs Improvement", "Poor" };

        // ── Small helpers ──
        public static string FullName(ApplicationUser? u)
        {
            if (u == null) return string.Empty;
            var n = $"{u.Name} {u.Surname}".Trim();
            return string.IsNullOrWhiteSpace(n) ? (u.UserName ?? u.Email ?? "") : n;
        }

        public static string CustomerName(DogTrainingCustomer? c) => c?.FullName ?? string.Empty;

        public static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        /// <summary>Suggests the day's overall label from the average slot rating (0-10).</summary>
        public static string PerformanceFromAverage(decimal avg) => avg switch
        {
            >= 9m => "Excellent",
            >= 7m => "Very Good",
            >= 5m => "Good",
            >= 3m => "Needs Improvement",
            _ => "Poor"
        };

        public static decimal? Average(IEnumerable<int?> ratings)
        {
            var list = ratings.Where(r => r.HasValue).Select(r => (decimal)r!.Value).ToList();
            return list.Count == 0 ? null : Math.Round(list.Average(), 1);
        }

        public static int TotalDays(DogTrainingEnrollment e) => e.DurationDays + e.ExtensionDays;
        public static int TotalWeeks(DogTrainingEnrollment e) => Math.Max(1, (int)Math.Ceiling(TotalDays(e) / 7.0));

        /// <summary>Week (1-based) and day (1-based) of training on <paramref name="on"/>; (0,0) if not started.</summary>
        public static (int Week, int Day) WeekAndDay(DogTrainingEnrollment e, DateTime on)
        {
            if (e.StartDate == null || e.Status is TrainingStatus.Completed or TrainingStatus.Cancelled) return (0, 0);
            var day = (on.Date - e.StartDate.Value.Date).Days + 1;
            if (day < 1) return (0, 0);
            var week = Math.Min((int)Math.Ceiling(day / 7.0), TotalWeeks(e));
            return (week, day);
        }

        public static bool CanAccess(TrainingActor actor, DogTrainingEnrollment e)
            => actor.CanManageAll || (e.HandlerUserId != null && e.HandlerUserId == actor.UserId);

        public static IQueryable<DogTrainingEnrollment> ScopeToActor(IQueryable<DogTrainingEnrollment> q, TrainingActor actor)
            => actor.CanManageAll ? q : q.Where(e => e.HandlerUserId == actor.UserId);

        public static Task<List<int>> CompanyScope(IUnitOfWork uow, int companyId, CancellationToken ct)
            => CompanyScopeResolver.ResolveAsync(uow, companyId, ct);

        public static async Task<string> NextNumberAsync(IUnitOfWork uow, string prefix, bool certificate, CancellationToken ct)
        {
            // Counted including soft-deleted rows so a number is never reused.
            var year = DateTime.UtcNow.Year;
            var count = certificate
                ? await uow.Repository<DogTrainingCertificate>().Table.AsTracking().IgnoreQueryFilters().CountAsync(ct)
                : await uow.Repository<DogTrainingEnrollment>().Table.AsTracking().IgnoreQueryFilters().CountAsync(ct);
            return certificate ? $"{prefix}-{year}-{count + 1:D6}" : $"{prefix}-{year}-{count + 1:D4}";
        }

        public static async Task<string> NextInvoiceNumberAsync(IUnitOfWork uow, CancellationToken ct)
        {
            var n = await uow.Repository<DogTrainingInvoice>().Table.AsTracking().IgnoreQueryFilters().CountAsync(ct);
            return $"TIV-{DateTime.UtcNow.Year}-{n + 1:D4}";
        }

        public static async Task<string> NextPaymentNumberAsync(IUnitOfWork uow, CancellationToken ct)
        {
            var n = await uow.Repository<DogTrainingPayment>().Table.AsTracking().IgnoreQueryFilters().CountAsync(ct);
            return $"TPY-{DateTime.UtcNow.Year}-{n + 1:D4}";
        }

        // ── Payment gate ─────────────────────────────────────────────────────
        /// <summary>
        /// Reads the existing Invoice for enrolments that are AwaitingPayment and moves them on when the invoice is
        /// fully PAID (→ AwaitingHandler, or Ready if a handler is already allocated). A cancelled invoice sends the
        /// enrolment back to HealthPassed so a new one can be raised. The Invoices module itself is never changed.
        /// </summary>
        public static async Task SyncPaymentAsync(IUnitOfWork uow, int? enrollmentId, List<int>? companyIds, CancellationToken ct, int? invoiceId = null)
        {
            var q = uow.Repository<DogTrainingEnrollment>().Table.AsTracking()
                .Where(e => e.Status == TrainingStatus.AwaitingPayment && e.InvoiceId != null);
            if (enrollmentId.HasValue) q = q.Where(e => e.Id == enrollmentId.Value);
            if (invoiceId.HasValue) q = q.Where(e => e.InvoiceId == invoiceId.Value);
            if (companyIds != null) q = q.Where(e => companyIds.Contains(e.CompanyId));

            var pending = await q.ToListAsync(ct);
            if (pending.Count == 0) return;

            var invoiceIds = pending.Select(e => e.InvoiceId!.Value).Distinct().ToList();
            var invoices = await uow.Repository<DogTrainingInvoice>().TableNoTracking
                .Where(i => invoiceIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Status, i.BalanceAmount })
                .ToDictionaryAsync(i => i.Id, ct);

            foreach (var e in pending)
            {
                if (!invoices.TryGetValue(e.InvoiceId!.Value, out var inv)) continue;
                if (inv.Status == "PAID" && inv.BalanceAmount <= 0)
                {
                    e.Status = e.HandlerUserId != null ? TrainingStatus.Ready : TrainingStatus.AwaitingHandler;
                    await uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);
                }
                else if (inv.Status == "CANCELLED")
                {
                    e.Status = TrainingStatus.HealthPassed;
                    e.InvoiceId = null;
                    await uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);
                }
            }
        }

        /// <summary>Training that has reached its planned end date becomes "awaiting final assessment".</summary>
        public static async Task SyncProgressAsync(IUnitOfWork uow, List<int>? companyIds, CancellationToken ct)
        {
            var today = DateTime.Today;
            var q = uow.Repository<DogTrainingEnrollment>().Table.AsTracking()
                .Where(e => e.Status == TrainingStatus.InTraining && e.PlannedEndDate != null && e.PlannedEndDate <= today);
            if (companyIds != null) q = q.Where(e => companyIds.Contains(e.CompanyId));
            foreach (var e in await q.ToListAsync(ct))
            {
                e.Status = TrainingStatus.AwaitingFinalAssessment;
                await uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);
            }
        }

        public static async Task<bool> IsInvoicePaidAsync(IUnitOfWork uow, int? invoiceId, CancellationToken ct)
        {
            if (invoiceId == null) return false;
            var inv = await uow.Repository<DogTrainingInvoice>().TableNoTracking
                .Where(i => i.Id == invoiceId.Value).Select(i => new { i.Status, i.BalanceAmount }).FirstOrDefaultAsync(ct);
            return inv != null && inv.Status == "PAID" && inv.BalanceAmount <= 0;
        }

        // ── DTO building ─────────────────────────────────────────────────────
        public static async Task<List<EnrollmentListDto>> BuildListAsync(IUnitOfWork uow, List<DogTrainingEnrollment> list, CancellationToken ct)
        {
            if (list.Count == 0) return new();

            var dogIds = list.Select(e => e.DogId).Distinct().ToList();
            var customerIds = list.Where(e => e.CustomerId != null).Select(e => e.CustomerId!.Value).Distinct().ToList();
            var programmeIds = list.Select(e => e.ProgrammeId).Distinct().ToList();
            var invoiceIds = list.Where(e => e.InvoiceId != null).Select(e => e.InvoiceId!.Value).Distinct().ToList();
            var handlerIds = list.Where(e => e.HandlerUserId != null).Select(e => e.HandlerUserId!).Distinct().ToList();
            var enrollmentIds = list.Select(e => e.Id).ToList();

            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name, d.Breed, d.PhotoUrl }).ToDictionaryAsync(d => d.Id, ct);
            var customers = customerIds.Count == 0 ? new Dictionary<int, string>() :
                (await uow.Repository<DogTrainingCustomer>().TableNoTracking.Where(c => customerIds.Contains(c.Id)).ToListAsync(ct))
                    .ToDictionary(c => c.Id, c => CustomerName(c));
            var programmes = await uow.Repository<DogTrainingProgramme>().TableNoTracking.Where(p => programmeIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, ct);
            var invoices = invoiceIds.Count == 0 ? new Dictionary<int, (string Number, string Status, decimal Balance)>() :
                (await uow.Repository<DogTrainingInvoice>().TableNoTracking.Where(i => invoiceIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.InvoiceNumber, i.Status, i.BalanceAmount }).ToListAsync(ct))
                    .ToDictionary(i => i.Id, i => (Number: i.InvoiceNumber, Status: i.Status, Balance: i.BalanceAmount));
            var handlers = handlerIds.Count == 0 ? new Dictionary<string, string>() :
                (await uow.Repository<ApplicationUser>().TableNoTracking.Where(u => handlerIds.Contains(u.Id)).ToListAsync(ct))
                    .ToDictionary(u => u.Id, u => FullName(u));
            var health = (await uow.Repository<DogHealthAssessment>().TableNoTracking
                    .Where(a => enrollmentIds.Contains(a.EnrollmentId))
                    .Select(a => new { a.EnrollmentId, a.Result, a.AssessmentDate, a.Id }).ToListAsync(ct))
                .GroupBy(a => a.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AssessmentDate).ThenByDescending(a => a.Id).First().Result);

            return list.Select(e =>
            {
                var (week, day) = WeekAndDay(e, DateTime.Today);
                dogs.TryGetValue(e.DogId, out var dog);
                invoices.TryGetValue(e.InvoiceId ?? 0, out var inv);
                return new EnrollmentListDto
                {
                    Id = e.Id,
                    EnrollmentNumber = e.EnrollmentNumber,
                    DogId = e.DogId,
                    DogName = dog?.Name ?? "",
                    DogBreed = dog?.Breed,
                    DogPhotoUrl = dog?.PhotoUrl,
                    CustomerId = e.CustomerId,
                    CustomerName = e.CustomerId != null && customers.TryGetValue(e.CustomerId.Value, out var cn) ? cn : null,
                    ProgrammeId = e.ProgrammeId,
                    ProgrammeName = programmes.TryGetValue(e.ProgrammeId, out var p) ? p.Name : "",
                    Status = e.Status,
                    HandlerUserId = e.HandlerUserId,
                    HandlerName = e.HandlerUserId != null && handlers.TryGetValue(e.HandlerUserId, out var hn) ? hn : null,
                    StartDate = e.StartDate,
                    PlannedEndDate = e.PlannedEndDate,
                    DurationDays = e.DurationDays,
                    ExtensionDays = e.ExtensionDays,
                    CurrentWeek = week,
                    DayNumber = day,
                    Fee = e.Fee,
                    AdditionalCharges = e.AdditionalCharges,
                    Total = e.Fee + e.AdditionalCharges,
                    InvoiceId = e.InvoiceId,
                    InvoiceNumber = e.InvoiceId != null && invoices.ContainsKey(e.InvoiceId.Value) ? inv.Number : null,
                    InvoiceStatus = e.InvoiceId != null && invoices.ContainsKey(e.InvoiceId.Value) ? inv.Status : null,
                    InvoiceBalance = e.InvoiceId != null && invoices.ContainsKey(e.InvoiceId.Value) ? inv.Balance : null,
                    LatestHealthResult = health.TryGetValue(e.Id, out var hr) ? hr : null,
                    CreatedAt = e.CreatedAt,
                };
            }).ToList();
        }

        public static List<ScheduleSlotDto> ParseSchedule(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<ScheduleSlotDto>>(json, Json) ?? new(); }
            catch { return new(); }
        }

        public static List<T> ParseList<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<T>>(json, Json) ?? new(); }
            catch { return new(); }
        }

        public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, Json);

        public static WeeklyPlanDto MapPlan(DogTrainingWeeklyPlan p) => new()
        {
            Id = p.Id, EnrollmentId = p.EnrollmentId, WeekNumber = p.WeekNumber, Title = p.Title, Focus = p.Focus,
            Targets = p.Targets, Schedule = ParseSchedule(p.ScheduleJson), StartDate = p.StartDate, EndDate = p.EndDate
        };
    }

    /// <summary>The default programme built from the three weekly plan sheets (WEEK 1/2/3 PLAN.docx).</summary>
    internal static class DefaultProgramme
    {
        public static SaveProgrammeDto Create(int companyId)
        {
            var dto = new SaveProgrammeDto
            {
                CompanyId = companyId,
                Name = "21-Day Dog Training",
                Description = "Foundation, intermediate and real-world proofing over three weeks, finishing with an owner handoff session.",
                DurationDays = 21,
                Fee = 0m,
                IsActive = true,
            };

            dto.Weeks.Add(new ProgrammeWeekDto
            {
                WeekNumber = 1, Title = "Foundation & Engagement", Focus = "Low Distraction",
                Targets = string.Join("\n", new[]
                {
                    "Marker & Lure Mechanics: Load a clicker or verbal marker (\"Yes\") and teach the dog to follow food lures willingly.",
                    "Name Recognition & Eye Contact: Reward instant check-ins when the dog's name is called.",
                    "Basic Positions: Introduce Sit, Down, and Place (going to a cot/mat) inside a quiet training room.",
                    "Leash Pressure: Teach the dog to yield to gentle leash tension rather than pulling against it.",
                })
            });
            dto.Weeks.Add(new ProgrammeWeekDto
            {
                WeekNumber = 2, Title = "Duration, Distance & Impulse Control", Focus = "Moderate Distraction",
                Targets = string.Join("\n", new[]
                {
                    "Stay & Release Cues: Build duration up to 2–3 minutes in Sit, Down, and Place before giving a clear release word (\"Break\" or \"Free\").",
                    "Impulse Control: Introduce Leave It (ignoring dropped food) and Wait at crate doors, front doors, and food bowls.",
                    "Loose-Leash Heel: Transition from indoor leash pressure games to structured neighborhood walks with turns and pace changes.",
                    "Long-Line Recall: Practice enthusiastic Come commands on a 15-to-30-foot lead in a fenced yard or quiet field.",
                })
            });
            dto.Weeks.Add(new ProgrammeWeekDto
            {
                WeekNumber = 3, Title = "Real-World Proofing & Owner Handoff", Focus = "High Distraction",
                Targets = string.Join("\n", new[]
                {
                    "Public Proofing: Practice all commands around dogs, joggers, traffic, and unfamiliar spaces while maintaining engagement.",
                    "Distraction Challenges: Test Place and Stay while toys are bounced or people walk past.",
                    "Fading Lures: Transition from continuous food rewards to variable reinforcement (praise, toys, intermittent treats).",
                    "Owner Transfer Session: Film a summary video of the dog's cues and conduct a 60-to-90-minute handoff lesson to teach the owner timing, leash handling, and house rules.",
                })
            });

            // The same daily schedule applies to every week (WeekNumber 0).
            var slots = new (string Time, string Title, string Purpose)[]
            {
                ("07:00", "Morning Walk", "Structured 15-minute walk focusing on loose-leash manners and calm threshold exits."),
                ("07:30", "Training Session 1", "Use morning kibble for marker charging, luring, and learning new commands."),
                ("08:00", "Crate Rest", "Mandatory decompression so the dog processes the morning session without overstimulation."),
                ("10:30", "Training Session 3 (Dinner & Recall)", "Use meal to practice \"Come\" on a long line, \"Leave it,\" and polite door manners."),
                ("17:00", "Training Session 2", "Focus on impulse control, \"Place\" command, and extending \"Sit\" or \"Down\" stays."),
            };
            for (var i = 0; i < slots.Length; i++)
                dto.Activities.Add(new ProgrammeActivityDto { WeekNumber = 0, TimeOfDay = slots[i].Time, Title = slots[i].Title, Purpose = slots[i].Purpose, SortOrder = i + 1 });

            return dto;
        }
    }
}
