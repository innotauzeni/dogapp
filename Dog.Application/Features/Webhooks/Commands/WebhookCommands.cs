using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Webhooks.DTOs;

namespace Dog.Application.Features.Webhooks.Commands
{
    public record CreateWebhookCommand(CreateWebhookDto Dto) : IRequest<ApiResponse<WebhookDto>>;
    public record UpdateWebhookCommand(UpdateWebhookDto Dto) : IRequest<ApiResponse<WebhookDto>>;
    public record DeleteWebhookCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record ToggleWebhookCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record DeliverWebhookCommand(WebhookDeliverDto Dto) : IRequest<ApiResponse<bool>>;
    public record RetryWebhookLogCommand(int LogId) : IRequest<ApiResponse<bool>>;
}
