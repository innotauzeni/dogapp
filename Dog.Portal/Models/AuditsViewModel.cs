using System.Text.Json;

namespace Dog.Portal.Models
{
    public class AuditsListViewModel
    {
        public List<AuditItemDto> AuditList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    // ── DTOs matching API responses ───────────────────────────────────
    public class AuditItemDto
    {
        public long Id { get; set; }
        public string? EntityName { get; set; }
        public string? ActionType { get; set; }
        public string? Username { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? EntityId { get; set; }
        // Use JsonElement so System.Text.Json preserves the raw JSON structure exactly
        public JsonElement? Changes { get; set; }

        // Helper: returns the Changes as a JSON string for use in data attributes
        public string ChangesJson =>
            Changes.HasValue && Changes.Value.ValueKind == JsonValueKind.Object
                ? Changes.Value.GetRawText()
                : "null";

        // Helper: count of top-level keys in Changes
        public int ChangesCount =>
            Changes.HasValue && Changes.Value.ValueKind == JsonValueKind.Object
                ? Changes.Value.EnumerateObject().Count()
                : 0;
    }

    public class AuditPageDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<AuditItemDto> Data { get; set; } = new();
        public List<string> Entities { get; set; } = new();
    }
}