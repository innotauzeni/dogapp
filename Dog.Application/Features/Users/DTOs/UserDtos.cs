namespace Dog.Application.Features.Users.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Status { get; set; }
        public string? RoleName { get; set; }
        public int CompanyId { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        // Optional — falls back to the email when not provided, so existing callers
        // that don't send it keep working exactly as before. Login already accepts
        // either (see LoginHandler: FindByEmailAsync ?? FindByNameAsync).
        public string? Username { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class UpdateUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Status { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }
}
