namespace Dog.Application.Features.Permissions.DTOs
{
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }

    public class CreatePermissionDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }

    public class UpdatePermissionDto : CreatePermissionDto
    {
        public int Id { get; set; }
    }
}
