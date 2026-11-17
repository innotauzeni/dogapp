using Dog.Application.Contracts;
using Dog.Application.Services;
using Dog.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dog.Infrastructure.Services
{

    public class ErrorLoggingService : IErrorLoggingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ErrorLoggingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ErrorLoggingService(IUnitOfWork unitOfWork, ILogger<ErrorLoggingService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogErrorAsync(Exception exception, string? additionalInfo = null)
        {
            _logger.LogError(exception, additionalInfo);

            var ctx = _httpContextAccessor.HttpContext;
            var log = new SystemErrorLog
            {
                Message = $"{exception.Message}{(additionalInfo != null ? $" | {additionalInfo}" : "")}",
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                ExceptionType = exception.GetType().FullName,
                Severity = "Error",
                Source = "System",
                MachineName = Environment.MachineName,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Path = ctx?.Request?.Path,
                Method = ctx?.Request?.Method,
                IpAddress = ctx?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = ctx?.Request?.Headers["User-Agent"],
                QueryString = ctx?.Request?.QueryString.ToString(),
            };

            await FileLogWriter.AppendAsync(
                $"ERROR | {log.ExceptionType} | {log.Message}{Environment.NewLine}" +
                $"Path: {log.Method} {log.Path}{log.QueryString}{Environment.NewLine}" +
                $"Inner: {log.InnerException}{Environment.NewLine}" +
                $"StackTrace:{Environment.NewLine}{log.StackTrace}");

            await PersistLogAsync(log);
        }

        public async Task LogErrorAsync(string message, string severity = "Error", string source = "System")
        {
            _logger.LogError("{Message} | Severity: {Severity} | Source: {Source}", message, severity, source);

            var log = new SystemErrorLog
            {
                Message = message,
                Severity = severity,
                Source = source,
                MachineName = Environment.MachineName,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            };

            await FileLogWriter.AppendAsync($"{severity.ToUpperInvariant()} | {source} | {message}");

            await PersistLogAsync(log);
        }

        public async Task LogWarningAsync(string message, string source = "System")
        {
            _logger.LogWarning("{Message} | Source: {Source}", message, source);

            var log = new SystemErrorLog
            {
                Message = message,
                Severity = "Warning",
                Source = source,
                MachineName = Environment.MachineName,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            };

            await FileLogWriter.AppendAsync($"WARNING | {source} | {message}");

            await PersistLogAsync(log);
        }

        public async Task LogInformationAsync(string message, string source = "System")
        {
            _logger.LogInformation("{Message} | Source: {Source}", message, source);

            var log = new SystemErrorLog
            {
                Message = message,
                Severity = "Information",
                Source = source,
                MachineName = Environment.MachineName,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            };

            await PersistLogAsync(log);
        }

        public async Task<IEnumerable<SystemErrorLog>> GetRecentErrorsAsync(int count = 50)
        {
            return await Task.FromResult(
                _unitOfWork.Repository<SystemErrorLog>()
                    .TableNoTracking
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(count)
                    .ToList()
            );
        }

        public async Task<IEnumerable<SystemErrorLog>> GetErrorsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await Task.FromResult(
                _unitOfWork.Repository<SystemErrorLog>()
                    .TableNoTracking
                    .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToList()
            );
        }

        public async Task MarkErrorAsResolvedAsync(Guid errorId, string resolution, Guid? resolvedBy = null)
        {
            var repo = _unitOfWork.Repository<SystemErrorLog>();
            var log = repo.TableNoTracking.FirstOrDefault(e => e.Uuid == errorId.ToString());
            if (log == null) return;

            log.IsResolved = true;
            log.Resolution = resolution;
            log.ResolvedAt = DateTime.UtcNow;
            log.ResolvedBy = resolvedBy;

            await repo.UpdateAsync(log);
        }

        private async Task PersistLogAsync(SystemErrorLog log)
        {
            try
            {
                await _unitOfWork.Repository<SystemErrorLog>().AddAsync(log);
            }
            catch (Exception ex)
            {
                // Fallback to logger only — never let logging crash the app
                _logger.LogCritical(ex, "Failed to persist error log to database");
            }
        }
    }




}
