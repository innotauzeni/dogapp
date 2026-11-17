namespace Dog.Application.Features.Documents.DTOs
{
    public class DocumentAttachmentDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDocumentAttachmentDto
    {
        public int CompanyId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string? Notes { get; set; }
        public string? UploadedByUserId { get; set; }
    }
}
