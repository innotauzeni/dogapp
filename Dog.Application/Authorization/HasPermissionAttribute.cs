using Microsoft.AspNetCore.Authorization;

namespace Dog.Application.Authorization
{
    /// <summary>
    /// Decorate controllers/actions with [HasPermission("systemmodules.create")]
    /// The permission name must match the Permission.Name stored in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "Permission:";

        public HasPermissionAttribute(string permission)
            : base($"{PolicyPrefix}{permission}")
        {
            Permission = permission;
        }

        public string Permission { get; }
    }


}
