using Dog.Core.Entities;

namespace Dog.Application.Services
{
    public partial interface IErrorLoggingService
    {
        public interface IJwtService
        {
            string GenerateAccessToken(ApplicationUser user, IList<string> roles, IList<string> permissions);
            string GenerateRefreshToken();
            string? GetUserIdFromToken(string token);
        }
    }
}
