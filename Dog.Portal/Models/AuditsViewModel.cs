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
        /// <summary>The record after the operation (for a delete: the record that was deleted).</summary>
        public JsonElement? Changes { get; set; }
        /// <summary>For an update: the record as it was before the change.</summary>
        public JsonElement? Previous { get; set; }
        public AuditActorDto? Actor { get; set; }

        /// <summary>The log stores INSERT/UPDATE/DELETE; the UI shows Create/Update/Delete.</summary>
        public string ActionKind => (ActionType ?? "").ToLowerInvariant() switch
        {
            "insert" or "create" => "create",
            "update" => "update",
            "delete" => "delete",
            _ => "other"
        };

        public string ActionLabel => ActionKind switch
        {
            "create" => "Create",
            "update" => "Update",
            "delete" => "Delete",
            _ => ActionType ?? ""
        };

        // Helper: returns the Changes as a JSON string for use in data attributes
        public string ChangesJson => Raw(Changes);
        public string PreviousJson => Raw(Previous);
        public string ActorJson => Actor == null ? "null" : JsonSerializer.Serialize(Actor);

        private static string Raw(JsonElement? e) =>
            e.HasValue && e.Value.ValueKind == JsonValueKind.Object ? e.Value.GetRawText() : "null";

        // Bookkeeping columns that change on every save and aren't a real edit by the user.
        private static readonly HashSet<string> NoiseFields = new(StringComparer.OrdinalIgnoreCase)
            { "UpdatedAt", "UpdatedBy", "DeletedAt", "DeletedBy" };

        /// <summary>Number of fields shown on the row: fields that really changed for an update, all fields otherwise.</summary>
        public int ChangesCount
        {
            get
            {
                if (!Changes.HasValue || Changes.Value.ValueKind != JsonValueKind.Object) return 0;
                if (ActionKind == "update" && Previous.HasValue && Previous.Value.ValueKind == JsonValueKind.Object)
                {
                    var prev = Previous.Value;
                    var n = 0;
                    foreach (var p in Changes.Value.EnumerateObject())
                    {
                        if (NoiseFields.Contains(p.Name)) continue;
                        if (!prev.TryGetProperty(p.Name, out var old) || old.GetRawText() != p.Value.GetRawText()) n++;
                    }
                    return n;
                }
                return Changes.Value.EnumerateObject().Count();
            }
        }
    }

    public class AuditActorDto
    {
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? CompanyName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
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
