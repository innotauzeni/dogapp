using Dog.Application.Common;
using Dog.Application.Features.Notifications.DTOs;
using MediatR;

namespace Dog.Application.Features.Notifications.Queries
{
    public record GetNotificationLogsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<NotificationLogDto>>>;
}
