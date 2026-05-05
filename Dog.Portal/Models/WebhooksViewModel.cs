using Dog.Application.Features.Webhooks.DTOs;

namespace Dog.Portal.Models
{
    public class WebhooksListViewModel
    {
        public List<WebhookDto> WebhookList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class WebhookLogsListViewModel
    {
        public List<WebhookLogDto> LogList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
        public int? WebhookId { get; set; }
    }

    // ── DTOs for webhook logs page response ───────────────────────────────────
    public class WebhookLogPageDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<WebhookLogDto> Data { get; set; } = new();
    }

    // ── AJAX request models ───────────────────────────────────
    public class CreateWebhookRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Secret { get; set; }
        public List<string> Events { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public int RetryCount { get; set; } = 3;
        public int CompanyId { get; set; }
    }

    public class UpdateWebhookRequest : CreateWebhookRequest
    {
        public int Id { get; set; }
    }
}