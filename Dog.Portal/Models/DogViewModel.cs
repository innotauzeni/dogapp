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
        public int? CustomerId { get; set; }
        public string? CompanyName { get; set; }
        public bool IsActive { get; set; }
        // Deployment status — populated by GetAnimalsByCompanyHandler
        public bool IsDeployed { get; set; }
        public string? DeployedLocation { get; set; }
        public string? DeployedTo { get; set; }
        // Branch assignment — dog assigned here but owned by head-office
        public bool IsAssignedToBranch { get; set; }
        public string? AssignedHandlerName { get; set; }
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
        public int? CustomerId { get; set; }
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
        public List<VaccinationRecordDto> Vaccinations { get; set; } = new();
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
        public int? SireId { get; set; }
        public string? SireName { get; set; }
        public int? DamId { get; set; }
        public string? DamName { get; set; }
        public DateTime MatingDate { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public List<LitterDto> Litters { get; set; } = new();
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

    public class GalleryImageDto
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DogProfileDto
    {
        public AnimalListDto Dog { get; set; } = null!;
        public List<MatingRecordDto> MatingRecords { get; set; } = new();
        public List<VaccinationRecordDto> Vaccinations { get; set; } = new();
        public DogAssignmentDto? CurrentAssignment { get; set; }
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

    public class DogCareRecordDto
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; }
        public string? Details { get; set; }
        public string? QuantityOrOutcome { get; set; }
        public bool RequiresAttention { get; set; }
        public string? RecordedByName { get; set; }
    }

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

    public class HandlerActivityViewModel
    {
        public List<HandlerActivityDto> Activities { get; set; } = new();
        public int TotalCount => Activities.Count;
        public int AttentionCount => Activities.Count(a => a.RequiresAttention);
        public int DogCount => Activities.Select(a => a.DogId).Distinct().Count();
    }

    public class UpdateCareRecordRequest
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; }
        public string? Details { get; set; }
        public string? QuantityOrOutcome { get; set; }
        public bool RequiresAttention { get; set; }
    }

    public class AssignDogRequest
    {
        public int DogId { get; set; }
        public string? HandlerUserId { get; set; }
        public int? BranchId { get; set; }
        public string? Notes { get; set; }
    }

    public class AddDogCareRecordRequest
    {
        public int DogId { get; set; }
        public int CompanyId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; }
        public string? Details { get; set; }
        public string? QuantityOrOutcome { get; set; }
        public bool RequiresAttention { get; set; }
        // extended fields
        public string? VaccineName { get; set; }
        public string? VaccineBatch { get; set; }
        public DateTime? VaccineNextDue { get; set; }
        public string? VaccineNotes { get; set; }

        public string? InjurySeverity { get; set; }
        public string? InjuryDetails { get; set; }
        public bool? InjuryTreated { get; set; }

        public string? BiteSeverity { get; set; }
        public string? BiteDetails { get; set; }
        public string? BiteReportedTo { get; set; }

        public int? MatingPartnerId { get; set; }
        public string? MatingPartnerName { get; set; }
        public string? MatingDuration { get; set; }
    }

    public class DogBranchOption
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
    }

    public class AddVaccinationRequest
    {
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

    public class AddMatingRecordRequest
    {
        public int? SireId { get; set; }
        public int? DamId { get; set; }
        // Free-text names for an unregistered partner.
        public string? SireName { get; set; }
        public string? DamName { get; set; }
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
