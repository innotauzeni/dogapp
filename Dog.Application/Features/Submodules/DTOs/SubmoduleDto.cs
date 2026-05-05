namespace Dog.Application.Features.Submodules.DTOs
{
    public class SubmoduleDetailDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int ModuleId { get; set; }
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public string? AccessName { get; set; }
        public int Order { get; set; }
    }

    public class CreateSubmoduleDto
    {
        public string Name { get; set; } = string.Empty;
        public int ModuleId { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string AccessName { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class UpdateSubmoduleDto : CreateSubmoduleDto
    {
        public int Id { get; set; }
    }
}
