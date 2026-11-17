using Dog.Application.Features.Deployments.DTOs;

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
    public int? CustomerId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeceased => DateOfDeath.HasValue;
    public List<DogCareRecordDto> CareRecords { get; set; } = new();
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
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public bool IsActive { get; set; }
    // Deployment status — populated by GetAnimalsByCompanyHandler
    public bool IsDeployed { get; set; }
    public string? DeployedLocation { get; set; }
    public string? DeployedTo { get; set; }
    // Branch assignment — true when this dog is assigned to the viewed branch (not owned by it)
    public bool IsAssignedToBranch { get; set; }
    public string? AssignedHandlerName { get; set; }
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
    public int? CustomerId { get; set; }
    public bool? IsActive { get; set; }
}


public class UpdateAnimalDto : CreateAnimalDto
{
    public int Id { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignCustomerDto
{
    public int DogId { get; set; }
    /// <summary>Customer to own the dog; null unassigns the current owner.</summary>
    public int? CustomerId { get; set; }
}

public class MatingRecordDto
{
    public int Id { get; set; }
    public int? SireId { get; set; }
    public string? SireName { get; set; }
    public int? DamId { get; set; }
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
    public List<VaccinationRecordDto> Vaccinations { get; set; } = new();
}

public class VaccinationRecordDto
{
    public int Id { get; set; }
    public int? DogId { get; set; }
    public int? PuppyId { get; set; }
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
    public DogAssignmentDto? CurrentAssignment { get; set; }
    public DeploymentDto? CurrentDeployment { get; set; }
    public List<DogCareRecordDto> CareRecords { get; set; } = new();
}

public class DogAssignmentDto
{
    public int Id { get; set; }
    public string? HandlerUserId { get; set; }
    public string? HandlerName { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime AssignedOn { get; set; }
    public string? Notes { get; set; }
}

public class AssignDogDto
{
    public int DogId { get; set; }
    public string? HandlerUserId { get; set; }
    public int? BranchId { get; set; }
    public string? Notes { get; set; }
}

public class DogCareRecordDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public string? Details { get; set; }
    public string? QuantityOrOutcome { get; set; }
    public string? Location { get; set; }
    public bool RequiresAttention { get; set; }
    public string? RecordedByName { get; set; }

    // Vaccination
    public string? VaccineName { get; set; }
    public string? VaccineBatch { get; set; }
    public DateTime? VaccineNextDue { get; set; }
    public string? VaccineNotes { get; set; }

    // Mating
    public string? MatingPartnerName { get; set; }
    public string? MatingDuration { get; set; }

    // Injury
    public string? InjurySeverity { get; set; }
    public string? InjuryDetails { get; set; }
    public bool InjuryTreated { get; set; }

    // Bite
    public string? BiteSeverity { get; set; }
    public string? BiteDetails { get; set; }
    public string? BiteReportedTo { get; set; }

    // Deworming
    public string? DewormingProduct { get; set; }
    public DateTime? DewormingNextDue { get; set; }
}

/// <summary>A dog whose next vaccination is due soon or overdue — used for alerts.</summary>
public class VaccineDueDto
{
    public int DogId { get; set; }
    public string DogName { get; set; } = string.Empty;
    public string? DogPhotoUrl { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime NextDue { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
}

/// <summary>A single care activity a handler recorded, enriched with dog info for the activity feed.</summary>
public class HandlerActivityDto
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public string DogName { get; set; } = string.Empty;
    public string? DogPhotoUrl { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public string? Details { get; set; }
    public string? QuantityOrOutcome { get; set; }
    public bool RequiresAttention { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanEdit { get; set; }
}

/// <summary>Edit an existing care record. Ownership is enforced by RecordedByUserId.</summary>
public class UpdateDogCareRecordDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public string? Details { get; set; }
    public string? QuantityOrOutcome { get; set; }
    public string? Location { get; set; }
    public bool RequiresAttention { get; set; }
    public string? RecordedByUserId { get; set; }
}

public class AddDogCareRecordDto
{
    public int DogId { get; set; }
    public int CompanyId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public string? Details { get; set; }
    public string? QuantityOrOutcome { get; set; }
    public string? Location { get; set; } // Kennel/location where event occurred (for feeding, etc)
    public bool RequiresAttention { get; set; }
    public string? RecordedByUserId { get; set; }

    // Optional extended fields for specific event types
    // Vaccination
    public string? VaccineName { get; set; }
    public string? VaccineBatch { get; set; }
    public DateTime? VaccineNextDue { get; set; }
    public string? VaccineNotes { get; set; }

    // Injury
    public string? InjurySeverity { get; set; }
    public string? InjuryDetails { get; set; }
    public bool? InjuryTreated { get; set; }

    // Bite
    public string? BiteSeverity { get; set; }
    public string? BiteDetails { get; set; }
    public string? BiteReportedTo { get; set; }

    // Mating
    public int? MatingPartnerId { get; set; }
    public string? MatingPartnerName { get; set; }
    public string? MatingDuration { get; set; }

    // Deworming
    public string? DewormingProduct { get; set; }
    public DateTime? DewormingNextDue { get; set; }
}

public class AddVaccinationDto
{
    // Provide DogId for a registered dog, OR PuppyId for a puppy tracked under a litter.
    public int? DogId { get; set; }
    public int? PuppyId { get; set; }
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
    public int? SireId { get; set; }
    public int CompanyId { get; set; }
    public int? DamId { get; set; }
    // Free-text names for a partner that is not registered in the system.
    public string? SireName { get; set; }
    public string? DamName { get; set; }
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
    public int DogId { get; set; }
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

public class PuppyReportDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public bool IsAlive => DateOfDeath == null;
    public int LitterId { get; set; }
    public string SireName { get; set; } = string.Empty;
    public string DamName { get; set; } = string.Empty;
}
