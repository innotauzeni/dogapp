namespace Dog.Application.Features.Roles.DTOs
{
    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Level { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Level { get; set; }
    }

    public class UpdateRoleDto : CreateRoleDto
    {
        public string Id { get; set; } = string.Empty;
    }
}
