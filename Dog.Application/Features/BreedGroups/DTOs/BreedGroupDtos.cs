namespace Dog.Application.Features.BreedGroups.DTOs
{
    public class BreedGroupDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBreedGroupDto
    {
        public string? Name { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateBreedGroupDto : CreateBreedGroupDto
    {
        public int Id { get; set; }
    }
}
