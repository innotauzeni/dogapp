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

        // ── Internal helpers ─────────────────────────────────────────────────────

        private HttpContext? Ctx => _httpContextAccessor.HttpContext;

        public UserSessionModel? GetSession()
        {
            var json = Ctx?.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<UserSessionModel>(json);
        }

        // ── Interface implementation ─────────────────────────────────────────────

        /// <summary>Always returns the user's own home company/branch ID.</summary>
        public Task<int> GetUserCompanyId()
        {
            var session = GetSession()
                ?? throw new UnauthorizedAccessException("User session not found.");
            return Task.FromResult(Convert.ToInt32(session.CompanyId));
        }

        /// <summary>
        /// Returns the ACTIVE context company:
        ///   - Head-office user who switched to a branch → CurrentBranchId
        ///   - Head-office user with no branch selected  → own CompanyId (head-office)
        ///   - Branch user (always)                      → own CompanyId
        /// All data fetches (dogs, customers, dashboard…) should use this.
        /// </summary>
        public Task<int> GetActiveCompanyId()
        {
            var session = GetSession()
                ?? throw new UnauthorizedAccessException("User session not found.");

            // Head-office user with a branch selected → use that branch as the active scope
            if (session.IsHeadOffice && session.CurrentBranchId.HasValue && session.CurrentBranchId.Value > 0)
                return Task.FromResult(session.CurrentBranchId.Value);

            return Task.FromResult(Convert.ToInt32(session.CompanyId));
        }

        /// <summary>True when the logged-in user belongs to the head-office company.</summary>
        public Task<bool> IsHeadOffice()
        {
            var session = GetSession();
            return Task.FromResult(session?.IsHeadOffice ?? false);
        }

        public Task<bool> IsAuthenticated()
        {
            var json = Ctx?.Session.GetString("UserSession");
            return Task.FromResult(!string.IsNullOrEmpty(json));
        }
    }
}
