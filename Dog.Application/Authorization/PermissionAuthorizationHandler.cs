using Microsoft.AspNetCore.Authorization;

namespace Dog.Application.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Works for both JWT (API) and Cookie (Portal) — both embed "permission" claims
            var hasClaim = context.User.Claims
                .Any(c => c.Type == "permission" && c.Value == requirement.Permission);

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

}
