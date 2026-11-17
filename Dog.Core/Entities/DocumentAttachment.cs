using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>A file (photo, contract, insurance doc...) attached to some other record, referenced loosely
/// by <see cref="EntityType"/>/<see cref="EntityId"/> (e.g. "Dog"/12) rather than a typed foreign key, so
/// one attachments table can serve every entity in the system without a relation per owner type.</summary>
public class DocumentAttachment : AuditableEntity
{
    public string EntityType { get; set; } = string.Empty; // "Dog", "Customer", "Accident"
    public int EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? Notes { get; set; }
    public string? UploadedByUserId { get; set; }
}
