namespace Dog.Portal.Models
{
    // ── Dogs ──────────────────────────────────────────────

    public class AnimalDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDeath { get; set; }
        public string? Gender { get; set; }
        public string? MicrochipNumber { get; set; }
        public string? PhotoUrl { get; set; }
        public int? SireId { get; set; }
        public int? DamId { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; }
    }
    public class AnimalListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDeath { get; set; }
        public string? Gender { get; set; }
        public string? MicrochipNumber { get; set; }
        public string? PhotoUrl { get; set; }
        public int? SireId { get; set; }
        public string? SireName { get; set; }
        public int? DamId { get; set; }
        public string? DamName { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool IsActive { get; set; }
    }


    public class CreateAnimalRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDeath { get; set; }
        public string? Gender { get; set; }
        public string? MicrochipNumber { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoBase64 { get; set; }  // base64 data URI from browser
        public int? SireId { get; set; }
        public int? DamId { get; set; }
        public int CompanyId { get; set; }
    }

    public class UpdateAnimalRequest : CreateAnimalRequest
    {
        public int Id { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? DateOfDeath { get; set; }
    }
    public class DogsListViewModel
    {
        public List<AnimalListDto> Doglist { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class PuppyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDeath { get; set; }
    }

    public class LitterDto
    {
        public int Id { get; set; }
        public DateTime BirthDate { get; set; }
        public int TotalBorn { get; set; }
        public int SurvivedCount { get; set; }
        public int DiedCount { get; set; }
        public string? Notes { get; set; }
        public string? SireName { get; set; }
        public string? DamName { get; set; }
        public List<PuppyDto> Puppies { get; set; } = new();
    }

    public class MatingRecordDto
    {
        public int Id { get; set; }
        public int SireId { get; set; }
        public string? SireName { get; set; }
        public int DamId { get; set; }
        public string? DamName { get; set; }
        public DateTime MatingDate { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public List<LitterDto> Litters { get; set; } = new();
    }

    public class VaccinationRecordDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? VaccType { get; set; }
        public string? BatchNumber { get; set; }
        public string? VetclinicName { get; set; }
        public DateTime VaccinationDate { get; set; }
        public DateTime? NextVaccinationDate { get; set; }
        public string? Notes { get; set; }
    }

    public class DogProfileDto
    {
        public AnimalListDto Dog { get; set; } = null!;
        public List<MatingRecordDto> MatingRecords { get; set; } = new();
        public List<VaccinationRecordDto> Vaccinations { get; set; } = new();
    }

    public class AddVaccinationRequest
    {
        public int DogId { get; set; }
        public int CompanyId { get; set; }
        public string? Name { get; set; }
        public string? VaccType { get; set; }
        public string? BatchNumber { get; set; }
        public string? VetclinicName { get; set; }
        public DateTime VaccinationDate { get; set; }
        public DateTime? NextVaccinationDate { get; set; }
        public string? Notes { get; set; }
    }

    public class AddMatingRecordRequest
    {
        public int SireId { get; set; }
        public int DamId { get; set; }
        public DateTime MatingDate { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }

        public int CompanyId { get; set; }
    }

    public class AddLitterRequest
    {
        public int MatingRecordId { get; set; }
        public DateTime BirthDate { get; set; }
        public int TotalBorn { get; set; }
        public int SurvivedCount { get; set; }
        public int DiedCount { get; set; }
        public string? Notes { get; set; }

        public int CompanyId { get; set; }
    }

    public class AddPuppyRequest
    {
        public int LitterId { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string Color { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDeath { get; set; }
    }
}
