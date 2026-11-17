namespace Dog.Application.Features.DogCertifications.DTOs
{
    public class DogCertificationDto
    {
        public int Id { get; set; }
        public int DogId { get; set; }
        public string? DogName { get; set; }
        public string CertificationName { get; set; } = string.Empty;
        public string? IssuingBody { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? TrainerName { get; set; }
        public string? Notes { get; set; }
        public string? CertificateFileUrl { get; set; }
    }

    public class CreateDogCertificationDto
    {
        public int DogId { get; set; }
        public int CompanyId { get; set; }
        public string CertificationName { get; set; } = string.Empty;
        public string? IssuingBody { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public string? TrainerName { get; set; }
        public string? Notes { get; set; }
        public string? CertificateFileUrl { get; set; }
    }

    public class UpdateDogCertificationDto : CreateDogCertificationDto
    {
        public int Id { get; set; }
    }
}
