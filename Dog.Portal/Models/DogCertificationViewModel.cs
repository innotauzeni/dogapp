namespace Dog.Portal.Models
{
    public class DogCertificationRequest
    {
        public int Id { get; set; }
        public int DogId { get; set; }
        public string CertificationName { get; set; } = string.Empty;
        public string? IssuingBody { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? TrainerName { get; set; }
        public string? Notes { get; set; }
    }
}
