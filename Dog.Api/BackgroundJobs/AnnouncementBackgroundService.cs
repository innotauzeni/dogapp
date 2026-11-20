using Dog.Api.Services;
using Dog.Core.Entities;
using Dog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dog.Api.BackgroundJobs
{
    /// <summary>
    /// Sends each active announcement to every staff user on WhatsApp, once per person per day, for the number of days the
    /// announcement runs (default 4), inside its daily sending window (default 09:00-18:00 local). Every message is
    /// recorded in ka_appannouncementdeliveries, so a restart or a second run never sends a duplicate.
    /// Note: WhatsApp only delivers free text to people who messaged the business in the last 24 hours; everyone else needs an
    /// approved message template (the bot's send routine already falls back to the template configured on the WhatsApp account).
    /// </summary>
    public class AnnouncementBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(20);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnnouncementBackgroundService> _logger;

        public AnnouncementBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AnnouncementBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); } catch (TaskCanceledException) { return; }
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await RunOnceAsync(stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "Announcement run failed."); }
                try { await Task.Delay(Interval, stoppingToken); } catch (TaskCanceledException) { break; }
            }
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var bot = scope.ServiceProvider.GetRequiredService<IWhatsAppBotService>();
            var clock = scope.ServiceProvider.GetRequiredService<IAppClock>();

            var now = clock.Now; var today = now.Date;
            var due = (await db.ka_appannouncements.Where(a => a.IsActive).ToListAsync(ct))
                .Where(a => today >= a.StartDate.Date && today < a.StartDate.Date.AddDays(Math.Max(1, a.WhatsAppDays)) && now.Hour >= a.SendFromHour && now.Hour < a.SendToHour)
                .ToList();
            if (due.Count == 0) return;

            // Staff with a phone number (customer-portal accounts are not sent app-download notices).
            var customerRoleId = await db.Roles.Where(r => r.Name == "Customer").Select(r => r.Id).FirstOrDefaultAsync(ct);
            var excluded = customerRoleId == null ? new List<string>()
                : await db.Set<ApplicationRoleUser>().Where(ur => ur.RoleId == customerRoleId).Select(ur => ur.UserId).ToListAsync(ct);
            var users = await db.Users.Where(u => u.PhoneNumber != null && u.PhoneNumber != "" && (u.Status == null || u.Status == "ACTIVE") && !excluded.Contains(u.Id))
                .Select(u => new { u.Id, u.PhoneNumber, u.CompanyId }).ToListAsync(ct);
            var recipients = users.GroupBy(u => Digits(u.PhoneNumber!)).Where(g => g.Key.Length >= 9).Select(g => g.First()).ToList();

            foreach (var a in due)
            {
                var sentToday = (await db.ka_appannouncementdeliveries.Where(d => d.AnnouncementId == a.Id && d.SentOn == today).Select(d => d.Phone).ToListAsync(ct)).ToHashSet();
                var dayNo = (today - a.StartDate.Date).Days + 1;
                var text = a.Message + (dayNo > 1 ? $"\n\n_Reminder {dayNo} of {a.WhatsAppDays}_" : "");
                var sent = 0;
                foreach (var u in recipients)
                {
                    if (ct.IsCancellationRequested) return;
                    var phone = Digits(u.PhoneNumber!);
                    if (sentToday.Contains(phone)) continue;
                    string? error = null;
                    try { await bot.SendMessageAsync(phone, text); }
                    catch (Exception ex) { error = ex.Message.Length > 300 ? ex.Message[..300] : ex.Message; }
                    db.ka_appannouncementdeliveries.Add(new AppAnnouncementDelivery
                    {
                        CompanyId = a.CompanyId, AnnouncementId = a.Id, Phone = phone, UserId = u.Id, SentOn = today, Success = error == null, Error = error
                    });
                    await db.SaveChangesAsync(ct);
                    sent++;
                    await Task.Delay(1200, ct); // stay well inside WhatsApp's rate limits
                }
                if (sent > 0) _logger.LogInformation("Announcement '{Key}' day {Day}: sent to {Count} people.", a.Key, dayNo, sent);
            }
        }

        private static string Digits(string phone) => new string(phone.Where(char.IsDigit).ToArray());
    }
}
