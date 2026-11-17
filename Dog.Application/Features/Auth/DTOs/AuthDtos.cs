namespace Dog.Application.Features.Auth.DTOs
{
    public class LoginRequestDto
    {
        /// <summary>Email, username, or phone number</summary>
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = null!;
        public List<MenuNodeDto> Menus { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public List<BranchDto> Branches { get; set; } = new();
        public bool IsHeadOffice { get; set; }
        /// The signed-in user's company brand color (hex, e.g. "#1D4ED8"), used to theme the mobile app's welcome screen.
        public string? BrandColor { get; set; }
    }

    public class BranchDto
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyCode { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class MenuNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? AccessName { get; set; }
        public int SortOrder { get; set; }
        public List<SubMenuNodeDto> Children { get; set; } = new();
    }

    public class SubMenuNodeDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public string? AccessName { get; set; }
        public int Order { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
