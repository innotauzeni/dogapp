namespace Dog.Application.Features.Accidents.DTOs;

public class AccidentDto
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public string? DogName { get; set; }
    public DateTime AccidentDate { get; set; }
    public int AccidentType { get; set; } // 1=BitingIncident, 2=SicknessRecord, 3=PropertyDamage
    public string AccidentTypeLabel { get; set; } = string.Empty;
    public int Severity { get; set; } // 1=Low, 2=Medium, 3=High, 4=Critical
    public string SeverityLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Resolution { get; set; }
    public bool IsResolved { get; set; }
    public int CompanyId { get; set; }

    // For biting incidents
    public string? PersonName { get; set; }
    public string? PersonPhoneNumber { get; set; }
    public string? PersonEmail { get; set; }
    public string? MedicalTreatmentProvided { get; set; }
    public bool HospitalVisitRequired { get; set; }

    // For sickness records
    public string? Symptoms { get; set; }
    public string? Treatment { get; set; }
    public string? VeterinarianName { get; set; }
    public DateTime? RecoveryDate { get; set; }

    // For property damage
    public decimal? EstimatedDamageCost { get; set; }
    public string? DamageDescription { get; set; }
    public bool InsuranceClaimFiled { get; set; }
    public string? InsuranceClaimNumber { get; set; }
}

public class CreateAccidentDto
{
    public int DogId { get; set; }
    public int CompanyId { get; set; }
    public DateTime AccidentDate { get; set; }
    public int AccidentType { get; set; }
    public int Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Resolution { get; set; }
    public bool IsResolved { get; set; }

    // For biting incidents
    public string? PersonName { get; set; }
    public string? PersonPhoneNumber { get; set; }
    public string? PersonEmail { get; set; }
    public string? MedicalTreatmentProvided { get; set; }
    public bool HospitalVisitRequired { get; set; }

    // For sickness records
    public string? Symptoms { get; set; }
    public string? Treatment { get; set; }
    public string? VeterinarianName { get; set; }
    public DateTime? RecoveryDate { get; set; }

    // For property damage
    public decimal? EstimatedDamageCost { get; set; }
    public string? DamageDescription { get; set; }
    public bool InsuranceClaimFiled { get; set; }
    public string? InsuranceClaimNumber { get; set; }
}

public class UpdateAccidentDto : CreateAccidentDto
{
    public int Id { get; set; }
}

public class AccidentListDto
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public string? DogName { get; set; }
    public DateTime AccidentDate { get; set; }
    public string AccidentTypeLabel { get; set; } = string.Empty;
    public string SeverityLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsResolved { get; set; }
}
