using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>A training certification/qualification earned by a dog (e.g. obedience, protection, detection).</summary>
public class DogCertification : AuditableEntity
{
    public int DogId { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuingBody { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? TrainerName { get; set; }
    public string? Notes { get; set; }
    public string? CertificateFileUrl { get; set; }

    public virtual Dogtbl Dog { get; set; } = null!;
}
