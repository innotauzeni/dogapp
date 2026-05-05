using MediatR;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Authorization;
using Dog.Application.Features.Webhooks.Commands;
using Dog.Application.Features.Webhooks.DTOs;
using Dog.Application.Features.Webhooks.Queries;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly IMediator _mediator;
        public WebhooksController(IMediator mediator) => _mediator = mediator;

        [HttpGet("company/{companyId:int}")]
        [HasPermission("webhooks.view")]
        public async Task<IActionResult> GetByCompany(int companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetWebhooksByCompanyQuery(companyId), ct));

        [HttpGet("{id:int}")]
        [HasPermission("webhooks.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetWebhookByIdQuery(id), ct));

        [HttpGet("logs")]
        [HasPermission("webhooks.view")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int? webhookId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
            => Ok(await _mediator.Send(new GetWebhookLogsQuery(webhookId, page, pageSize), ct));

        [HttpPost]
        [HasPermission("webhooks.create")]
        public async Task<IActionResult> Create([FromBody] CreateWebhookDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateWebhookCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("webhooks.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWebhookDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateWebhookCommand(dto), ct));
        }

        [HttpDelete("{id:int}")]
        [HasPermission("webhooks.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteWebhookCommand(id), ct));

        [HttpPost("{id:int}/toggle")]
        [HasPermission("webhooks.manage")]
        public async Task<IActionResult> Toggle(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleWebhookCommand(id), ct));

        [HttpPost("deliver")]
        [HasPermission("webhooks.manage")]
        public async Task<IActionResult> Deliver([FromBody] WebhookDeliverDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new DeliverWebhookCommand(dto), ct));

        [HttpPost("logs/{logId:int}/retry")]
        [HasPermission("webhooks.manage")]
        public async Task<IActionResult> Retry(int logId, CancellationToken ct)
            => Ok(await _mediator.Send(new RetryWebhookLogCommand(logId), ct));

        /// <summary>
        /// Returns all available webhook event types.
        /// </summary>
        [HttpGet("events")]
        [HasPermission("webhooks.view")]
        public IActionResult GetEvents()
            => Ok(new { events = WebhookEvents.All });
    }
}
