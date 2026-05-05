namespace Dog.Application.Features.Animals.DTOs;

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
    // Deployment status — populated by GetAnimalsByCompanyHandler
    public bool IsDeployed { get; set; }
    public string? DeployedLocation { get; set; }
    public string? DeployedTo { get; set; }
}

public class CreateAnimalDto
{
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
    public bool? IsActive { get; set; }
}


public class UpdateAnimalDto : CreateAnimalDto
{
    public int Id { get; set; }
    public bool? IsActive { get; set; }
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

public class PuppyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
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

public class AddVaccinationDto
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

public class AddMatingRecordDto
{
    public int SireId { get; set; }
    public int CompanyId { get; set; }
    public int DamId { get; set; }
    public DateTime MatingDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class AddLitterDto
{
    public int MatingRecordId { get; set; }
    public int CompanyId { get; set; }
    public DateTime BirthDate { get; set; }
    public int TotalBorn { get; set; }
    public int SurvivedCount { get; set; }
    public int DiedCount { get; set; }
    public string? Notes { get; set; }
}

public class AddPuppyDto
{
    public int LitterId { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string Color { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
}

// Report DTOs
public class AnimalItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? OwnerName { get; set; }
    public bool IsActive { get; set; }
}

public class AnimalPageDto
{
    public List<AnimalItemDto> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class VaccinatedDogDto
{
    public string DogName { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public DateTime LastVaccinationDate { get; set; }
    public string VaccineType { get; set; } = string.Empty;
    public DateTime NextDueDate { get; set; }
    public string VaccinationStatus { get; set; } = string.Empty;
}

public class LitterReportDto
{
    public int Id { get; set; }
    public string SireName { get; set; } = string.Empty;
    public string DamName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int TotalPuppies { get; set; }
    public int AlivePuppies { get; set; }
    public int DeceasedPuppies { get; set; }
}

public class MatingRecordReportDto
{
    public int Id { get; set; }
    public string SireName { get; set; } = string.Empty;
    public string DamName { get; set; } = string.Empty;
    public DateTime MatingDate { get; set; }
    public string? Location { get; set; }
    public DateTime ExpectedDueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}