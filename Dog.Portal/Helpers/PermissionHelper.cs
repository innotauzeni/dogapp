using System.Text.Json;
using Dog.Portal.Models;

namespace Dog.Portal.Helpers
{
    /// <summary>
    /// Called from Razor views to check permissions stored in session.
    /// Usage in views:  @{ var P = PermHelper.For(Context); }
    ///                  @if (P.Can("users.create")) { ... }
    /// </summary>
    public class PermHelper
    {
        private readonly HashSet<string> _perms;

        private PermHelper(HashSet<string> perms) => _perms = perms;

        public static PermHelper For(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var json = context.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(json))
                return new PermHelper(new HashSet<string>());

            var session = JsonSerializer.Deserialize<UserSessionModel>(json);
            var perms = new HashSet<string>(
                session?.Permissions ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);

            return new PermHelper(perms);
        }

        /// <summary>Check if the user has a specific permission key.</summary>
        public bool Can(string permission) => _perms.Contains(permission);

        /// <summary>Check if the user has ANY of the given permissions.</summary>
        public bool CanAny(params string[] permissions) =>
            permissions.Any(p => _perms.Contains(p));
    }
}
