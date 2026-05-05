namespace Dog.Portal.Models
{
    // ── Reports ──────────────────────────────────────────────

    public class ReportsViewModel
    {
        public int TotalDogs { get; set; }
        public int VaccinatedDogs { get; set; }
        public int TotalLitters { get; set; }
        public int TotalMatingRecords { get; set; }
    }

    // Report DTOs for export functionality
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
}