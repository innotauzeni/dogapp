using System.ComponentModel.DataAnnotations;

namespace Dog.Portal.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email, username, or phone number is required")]
        [Display(Name = "Username")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public LoginResponseData? Data { get; set; }
    }

    public class LoginResponseData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = new();
        public List<MenuItemModel> Menus { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public List<BranchModel> Branches { get; set; } = new();
        public bool IsHeadOffice { get; set; }
    }

    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class UserSessionModel
    {
        public string Id { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public List<MenuItemModel> Menus { get; set; } = new();
        public bool IsHeadOffice { get; set; }
        public bool IsDogHandler => Role == "Dog Handler";
        public List<BranchModel> Branches { get; set; } = new();
        public int? CurrentBranchId { get; set; }
    }

    public class BranchModel
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyCode { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
    }

    public class MenuItemModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? AccessName { get; set; }
        public int SortOrder { get; set; }
        public List<SubMenuItemModel> Children { get; set; } = new();
    }

    public class SubMenuItemModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public string? AccessName { get; set; }
        public int Order { get; set; }
    }

    public class SwitchBranchRequest
    {
        public int BranchId { get; set; }
    }
}
