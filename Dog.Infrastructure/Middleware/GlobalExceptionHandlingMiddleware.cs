using Dog.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dog.Infrastructure.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IErrorLoggingService errorLoggingService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An unhandled exception occurred while processing the request");

                // Log the error using our error logging service
                await errorLoggingService.LogErrorAsync(exception, "Unhandled exception caught by global middleware");

                await HandleExceptionAsync(context, exception);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case ArgumentNullException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid request: Missing required parameter";
                    response.Details = exception.Message;
                    break;

                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid request: Invalid parameter value";
                    response.Details = exception.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Access denied";
                    response.Details = "You don't have permission to access this resource";
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    response.Details = exception.Message;
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid operation";
                    response.Details = exception.Message;
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request timeout";
                    response.Details = "The request took too long to process";
                    break;

                case NotImplementedException:
                    response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    response.Message = "Feature not implemented";
                    response.Details = "This feature is not yet implemented";
                    break;

                // Almost always a foreign-key constraint violation (e.g. deleting a user/company
                // that other records still reference) — the previous generic 500 gave the client
                // no way to tell "you don't have permission" apart from "this is still in use".
                case DbUpdateException dbEx:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Message = "This action could not be completed because related records still reference it.";
                    response.Details = (dbEx.InnerException?.Message ?? dbEx.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An internal server error occurred";
                    response.Details = "Please try again later or contact support if the problem persists";
                    break;
            }

            context.Response.StatusCode = response.StatusCode;

            // Add error ID for tracking
            response.ErrorId = Guid.NewGuid().ToString();
            response.Timestamp = DateTime.UtcNow;

            // In development, include more details
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                response.DeveloperMessage = exception.Message;
                response.StackTrace = exception.StackTrace;
            }

            // Every other endpoint in this API returns the ApiResponse<T> envelope
            // ({ success, message, data, errors }, camelCase) via System.Text.Json's default
            // formatter. This handler used Newtonsoft with default PascalCase casing and no
            // "success" field at all, so on any unhandled exception (e.g. a DB foreign-key
            // violation from deleting a user/company still referenced elsewhere), both the
            // mobile app and Portal failed to recognize the response as an error body and fell
            // back to a generic, unhelpful "Request failed (500)" — making real failures look
            // like the action silently did nothing. Matching the envelope shape/casing here
            // lets both clients surface the actual reason.
            var jsonResponse = JsonConvert.SerializeObject(response, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            });
            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string ErrorId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? DeveloperMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}
