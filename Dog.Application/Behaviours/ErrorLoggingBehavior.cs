using Dog.Application.Services;
using MediatR;

namespace Dog.Application.Behaviours
{
    public class ErrorLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
       where TRequest : IRequest<TResponse>
    {
        private readonly IErrorLoggingService _errorLoggingService;

        public ErrorLoggingBehavior(IErrorLoggingService errorLoggingService)
        {
            _errorLoggingService = errorLoggingService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                await _errorLoggingService.LogErrorAsync(ex, $"MediatR pipeline error for {typeof(TRequest).Name}");
                throw;
            }
        }
    }

}
