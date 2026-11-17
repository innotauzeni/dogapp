using Dog.Api.Services;
using Dog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dog.Api.BackgroundJobs
{
    /// <summary>
    /// Polls for newly-logged Critical-severity errors and sends a single WhatsApp
    /// alert to the configured admin phone number (appsettings "SystemAlerts:AdminPhone")
    /// so a critical failure doesn't sit silently on the System Logs page until someone
    /// happens to check it. Each log is only ever alerted once (IsAlerted flag).
    /// </summary>
    public class CriticalErrorAlertService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<CriticalErrorAlertService> _logger;

        public CriticalErrorAlertService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<CriticalErrorAlertService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await SendPendingAlertsAsync(stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "Critical error alert run failed."); }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task SendPendingAlertsAsync(CancellationToken ct)
        {
            var adminPhone = _config["SystemAlerts:AdminPhone"];
            if (string.IsNullOrWhiteSpace(adminPhone)) return; // not configured — nothing to do

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var bot = scope.ServiceProvider.GetRequiredService<IWhatsAppBotService>();

            var pending = await db.ka_sytemerrorlogs
                .IgnoreQueryFilters()
                .Where(e => e.Severity == "Critical" && !e.IsAlerted)
                .OrderBy(e => e.CreatedAt)
                .Take(20)
                .ToListAsync(ct);

            if (pending.Count == 0) return;

            foreach (var log in pending)
            {
                var message = $"🚨 *CRITICAL ERROR*\n\n{log.Message}\n\nSource: {log.Source}\nPath: {log.Path ?? "—"}\nTime: {log.CreatedAt:yyyy-MM-dd HH:mm} UTC";
                try
                {
                    await bot.SendMessageAsync(adminPhone, message);
                    log.IsAlerted = true;
                    log.AlertedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send critical error alert for log {LogId}.", log.Id);
                }
            }

            db.ka_sytemerrorlogs.UpdateRange(pending);
            await db.SaveChangesAsync(ct);
        }
    }
}
