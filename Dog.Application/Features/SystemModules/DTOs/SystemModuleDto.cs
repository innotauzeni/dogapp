namespace Dog.Application.Features.SystemModules.DTOs
{
    public class SystemModuleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccessName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public List<SubmoduleDto> Children { get; set; } = new();
    }

    public class SubmoduleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public string? AccessName { get; set; }
        public int Order { get; set; }
    }

    public class CreateSystemModuleDto
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccessName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSystemModuleDto : CreateSystemModuleDto
    {
        public int Id { get; set; }
    }
}
