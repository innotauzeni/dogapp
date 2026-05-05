using Dog.Core.Entities;

namespace Dog.Application.Services
{
    public partial interface IErrorLoggingService
    {
        Task LogErrorAsync(Exception exception, string? additionalInfo = null);
        Task LogErrorAsync(string message, string severity = "Error", string source = "System");
        Task LogWarningAsync(string message, string source = "System");
        Task LogInformationAsync(string message, string source = "System");
        Task<IEnumerable<SystemErrorLog>> GetRecentErrorsAsync(int count = 50);
        Task<IEnumerable<SystemErrorLog>> GetErrorsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task MarkErrorAsResolvedAsync(Guid errorId, string resolution, Guid? resolvedBy = null);
    }
}
