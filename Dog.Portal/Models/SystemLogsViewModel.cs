namespace Dog.Portal.Models
{
    public class SystemLogsListViewModel
    {
        public List<ErrorLogItemDto> ErrorLogList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalDeleted { get; set; }
        public bool ShowDeleted { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    // ── DTOs matching API responses ───────────────────────────────────
    public class ErrorLogItemDto
    {
        public int Id { get; set; }
        public string? Uuid { get; set; }
        public string? Message { get; set; }
        public string? Severity { get; set; }
        public string? Source { get; set; }
        public string? ExceptionType { get; set; }
        public string? Path { get; set; }
        public string? Method { get; set; }
        public string? IpAddress { get; set; }
        public string? MachineName { get; set; }
        public string? Environment { get; set; }
        public bool IsResolved { get; set; }
        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StackTrace { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class ErrorLogPageDto
    {
        public int Total { get; set; }
        public int TotalDeleted { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ErrorLogItemDto> Data { get; set; } = new();
    }

    // ── AJAX request models ───────────────────────────────────
    public class ResolveErrorRequest
    {
        public string Resolution { get; set; } = string.Empty;
    }
}