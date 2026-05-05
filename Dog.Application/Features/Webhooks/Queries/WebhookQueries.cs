using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Webhooks.DTOs;

namespace Dog.Application.Features.Webhooks.Queries
{
    public record GetWebhooksByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<WebhookDto>>>;
    public record GetWebhookByIdQuery(int Id) : IRequest<ApiResponse<WebhookDto>>;
    public record GetWebhookLogsQuery(int? WebhookId, int Page = 1, int PageSize = 50) : IRequest<ApiResponse<WebhookLogPageDto>>;
}

namespace Dog.Application.Features.Webhooks.DTOs
{
    public class WebhookLogPageDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<WebhookLogDto> Data { get; set; } = new();
    }
}
