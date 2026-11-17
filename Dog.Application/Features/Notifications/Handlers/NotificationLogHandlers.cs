using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Notifications.DTOs;
using Dog.Application.Features.Notifications.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Notifications.Handlers
{
    public class GetNotificationLogsByCompanyHandler : IRequestHandler<GetNotificationLogsByCompanyQuery, ApiResponse<List<NotificationLogDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetNotificationLogsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<NotificationLogDto>>> Handle(GetNotificationLogsByCompanyQuery request, CancellationToken ct)
        {
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);
            var list = await _uow.Repository<NotificationLog>()
                .TableNoTracking
                .Where(n => companyIds.Contains(n.CompanyId))
                .OrderByDescending(n => n.SentAt)
                .Take(200)
                .ToListAsync(ct);

            return ApiResponse<List<NotificationLogDto>>.Ok(list.Select(n => new NotificationLogDto
            {
                Id = n.Id,
                NotificationType = n.NotificationType,
                RecipientPhone = n.RecipientPhone,
                Message = n.Message,
                SentAt = n.SentAt,
                Success = n.Success,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
            }).ToList());
        }
    }
}
