using Dog.Application.Authorization;
using Dog.Application.Features.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        public NotificationController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("notification.view")]
        public async Task<IActionResult> GetByCompany(int companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetNotificationLogsByCompanyQuery(companyId), ct));
    }
}
