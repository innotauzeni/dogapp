using Dog.Portal.Models;

namespace Dog.Portal.Services
{
    public interface ICurrentUserInfoService
    {
        /// <summary>
        /// Returns the user's own CompanyId (their home company/branch) — never changes.
        /// </summary>
        Task<int> GetUserCompanyId();

        /// <summary>
        /// Returns the ACTIVE company context:
        /// - Head-office users who have switched to a branch → returns CurrentBranchId
        /// - Head-office users with no branch selected   → returns their own CompanyId
        /// - Branch users                                → returns their own CompanyId
        /// Use this everywhere you need to scope data (dogs, customers, deployments…)
        /// </summary>
        Task<int> GetActiveCompanyId();

        /// <summary>True if the current user belongs to the head-office company.</summary>
        Task<bool> IsHeadOffice();

        Task<bool> IsAuthenticated();

        /// <summary>Returns the full session model or null if not authenticated.</summary>
        UserSessionModel? GetSession();
    }
}
