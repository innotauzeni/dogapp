namespace Dog.Application.Features.Breeds.DTOs
{
    public class BreedDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CompanyId { get; set; }
        public int BreedGroupId { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBreedDto
    {
        public int BreedGroupId { get; set; }
        public string? Name { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateBreedDto : CreateBreedDto
    {
        public int Id { get; set; }
    }
}
