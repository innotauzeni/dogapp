using System.Text.Json;
using Dog.Portal.Models;

namespace Dog.Portal.Services
{
    public class CurrentUserInfoService : ICurrentUserInfoService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserInfoService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> GetUserCompanyId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("HttpContext is not available");
            }

            var userSessionJson = httpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                throw new UnauthorizedAccessException("User session not found");
            }

            var userSession = JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (userSession?.CompanyId == null)
            {
                throw new InvalidOperationException("Company ID not found in user session");
            }

            return Convert.ToInt32(userSession.CompanyId);
        }

        public Task<bool> IsAuthenticated()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.FromResult(false);
            }

            var userSessionJson = httpContext.Session.GetString("UserSession");
            return Task.FromResult(!string.IsNullOrEmpty(userSessionJson));
        }
    }
}
