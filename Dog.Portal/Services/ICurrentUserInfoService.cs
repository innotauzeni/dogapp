namespace Dog.Portal.Services
{
    public interface ICurrentUserInfoService
    {
        Task<int> GetUserCompanyId();
        Task<bool> IsAuthenticated();
    }
}
