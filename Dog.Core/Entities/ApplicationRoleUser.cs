using Microsoft.AspNetCore.Identity;

namespace Dog.Core.Entities
{
    // Navigation properties are intentionally omitted here.
    // IdentityUserRole<string> already owns UserId + RoleId FKs.
    // Adding nav props here causes EF to create shadow FK columns (RoleId1, UserId1).
    // Access User/Role via UserManager / RoleManager instead.
    public class ApplicationRoleUser : IdentityUserRole<string>
    {
    }
}
