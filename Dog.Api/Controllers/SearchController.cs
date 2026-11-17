using Dog.Application.Features.Search.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SearchController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] int companyId, [FromQuery] string query, CancellationToken ct)
            => Ok(await _mediator.Send(new GlobalSearchQuery(companyId, query), ct));
    }
}
