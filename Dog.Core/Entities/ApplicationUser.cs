using Microsoft.AspNetCore.Identity;

namespace Dog.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }

        public string? Surname { get; set; }
        public string? Address { get; set; }

        public string? RawPassword { get; set; }

        public string? Gender { get; set; }
        public string? RoleName { get; set; }
        public string? AdminRole { get; set; }
        public string? Role { get; set; }
        public DateTime? LastLoginDate { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Status { get; set; } = "ACTIVE";
        public int CompanyId { get; set; }

    }

}
