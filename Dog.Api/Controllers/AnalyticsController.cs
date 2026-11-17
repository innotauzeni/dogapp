using Dog.Application.Features.Analytics.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AnalyticsController(IMediator mediator) => _mediator = mediator;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int companyId, [FromQuery] int months = 6, CancellationToken ct = default)
            => Ok(await _mediator.Send(new GetAnalyticsSummaryQuery(companyId, months), ct));
    }
}
