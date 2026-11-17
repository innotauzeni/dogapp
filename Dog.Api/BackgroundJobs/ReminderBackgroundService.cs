using Dog.Api.Services;
using Dog.Core.Entities;
using Dog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dog.Api.BackgroundJobs
{
    /// <summary>
    /// Once a day, checks every company for vaccines due soon, customer contracts nearing
    /// expiry, and overdue invoices, and sends a single WhatsApp digest to that company's
    /// registered phone number — so nobody has to remember to go check the lists. Every
    /// item sent is logged in ka_notificationlogs so the same reminder isn't repeated once
    /// it's already gone out.
    /// </summary>
    public class ReminderBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private const int VaccineDueWithinDays = 7;
        private const int ContractExpiryWithinDays = 30;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderBackgroundService> _logger;

        public ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await SendDueRemindersAsync(stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "Reminder run failed."); }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task SendDueRemindersAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var bot = scope.ServiceProvider.GetRequiredService<IWhatsAppBotService>();

            var today = DateTime.UtcNow.Date;
            var vaccineCutoff = today.AddDays(VaccineDueWithinDays);
            var contractCutoff = today.AddDays(ContractExpiryWithinDays);

            var companies = await db.ka_companies
                .Where(c => c.IsActive && c.PhoneNumber != null && c.PhoneNumber != "")
                .ToListAsync(ct);

            foreach (var company in companies)
            {
                try { await SendCompanyDigestAsync(db, bot, company, today, vaccineCutoff, contractCutoff, ct); }
                catch (Exception ex) { _logger.LogError(ex, "Reminder digest failed for company {CompanyId}.", company.Id); }
            }
        }

        private async Task SendCompanyDigestAsync(ApplicationDbContext db, IWhatsAppBotService bot, Company company,
            DateTime today, DateTime vaccineCutoff, DateTime contractCutoff, CancellationToken ct)
        {
            // Don't repeat a reminder for the same item — once it's logged, it's done,
            // regardless of how many times this job has run since.
            var alreadySent = (await db.ka_notificationlogs
                    .Where(n => n.CompanyId == company.Id)
                    .Select(n => new { n.NotificationType, n.RelatedEntityId })
                    .ToListAsync(ct))
                .ToHashSet();
            bool WasSent(string type, int? id) => alreadySent.Contains(new { NotificationType = type, RelatedEntityId = id });

            var lines = new List<string>();
            var logsToAdd = new List<NotificationLog>();

            var dueVaccines = await db.ka_vaccinationrecords
                .Include(v => v.Dog)
                .Where(v => v.DogId != null && v.NextVaccinationDate != null && v.NextVaccinationDate <= vaccineCutoff
                    && v.Dog != null && v.Dog.CompanyId == company.Id)
                .ToListAsync(ct);
            foreach (var v in dueVaccines)
            {
                if (WasSent("VaccineDue", v.Id)) continue;
                var overdue = v.NextVaccinationDate!.Value.Date < today;
                lines.Add($"💉 {v.Dog?.Name ?? "Dog"} — {v.Name ?? "vaccination"} {(overdue ? "OVERDUE since" : "due")} {v.NextVaccinationDate:yyyy-MM-dd}");
                logsToAdd.Add(NewLog(company.Id, company.PhoneNumber, "VaccineDue", "VaccinationRecord", v.Id));
            }

            var expiringContracts = await db.ka_customercontracts
                .Include(c => c.Customer)
                .Where(c => c.CompanyId == company.Id && c.ExpiryDate != null && c.ExpiryDate <= contractCutoff)
                .ToListAsync(ct);
            foreach (var c in expiringContracts)
            {
                if (WasSent("ContractRenewal", c.Id)) continue;
                var name = c.Customer?.FullName ?? $"{c.Customer?.FirstName} {c.Customer?.LastName}".Trim();
                lines.Add($"📄 Contract with {name} expires {c.ExpiryDate:yyyy-MM-dd}");
                logsToAdd.Add(NewLog(company.Id, company.PhoneNumber, "ContractRenewal", "CustomerContract", c.Id));
            }

            var overdueInvoices = await db.ka_invoices
                .Where(i => i.CompanyId == company.Id && i.DueDate < today
                    && i.Status != "PAID" && i.Status != "CANCELLED" && i.BalanceAmount > 0)
                .ToListAsync(ct);
            foreach (var inv in overdueInvoices)
            {
                if (WasSent("InvoiceOverdue", inv.Id)) continue;
                lines.Add($"🧾 Invoice {inv.InvoiceNumber} for {inv.CustomerName ?? "customer"} overdue — balance {inv.BalanceAmount:0.00}");
                logsToAdd.Add(NewLog(company.Id, company.PhoneNumber, "InvoiceOverdue", "Invoice", inv.Id));
            }

            if (logsToAdd.Count == 0) return;

            var message = $"🔔 *Daily Reminders — {company.CompanyName}*\n\n" + string.Join("\n", lines);
            var success = true;
            try { await bot.SendMessageAsync(company.PhoneNumber!, message); }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError(ex, "Failed to send reminder digest to company {CompanyId}.", company.Id);
            }

            foreach (var log in logsToAdd)
            {
                log.Success = success;
                log.Message = message;
                db.ka_notificationlogs.Add(log);
            }
            await db.SaveChangesAsync(ct);
        }

        private static NotificationLog NewLog(int companyId, string? phone, string type, string relatedType, int relatedId) => new()
        {
            CompanyId = companyId,
            NotificationType = type,
            RecipientPhone = phone,
            RelatedEntityType = relatedType,
            RelatedEntityId = relatedId,
            SentAt = DateTime.UtcNow,
            Message = string.Empty,
        };
    }
}
