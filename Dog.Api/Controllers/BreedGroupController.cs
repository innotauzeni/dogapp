using Dog.Application.Authorization;
using Dog.Application.Features.BreedGroups.Commands;
using Dog.Application.Features.BreedGroups.DTOs;
using Dog.Application.Features.BreedGroups.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BreedGroupController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BreedGroupController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetBreedGroups")]
        [HasPermission("breedgroup.view")]
        public async Task<IActionResult> GetBreedGroups(int CompanyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetBreedGroupByCompanyQuery(CompanyId), ct));

        [HttpGet("GetPubicBreedGroups")]
        public async Task<IActionResult> GetPubicBreedGroups(int CompanyId, CancellationToken ct)
          => Ok(await _mediator.Send(new GetBreedGroupByCompanyQuery(CompanyId), ct));

        [HttpGet("GetBreedGroupById/{id:int}")]
        [HasPermission("breedgroup.view")]
        public async Task<IActionResult> GetBreedGroupById(int id, CancellationToken ct)
          => Ok(await _mediator.Send(new GetBreedGroupByIdQuery(id), ct));

        [HttpPost("CreateBreedGroup")]
        [HasPermission("breedgroup.create")]
        public async Task<IActionResult> CreateBreedGroup([FromBody] CreateBreedGroupDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateBreedGroupCommand(dto), ct));

        [HttpPut("breedgroup/{id:int}")]
        [HasPermission("breedgroup.edit")]
        public async Task<IActionResult> UpdateBreedGroup(int id, [FromBody] CreateBreedGroupDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateBreedGroupCommand(id, dto), ct));
        }

        [HttpDelete("breedgroup/{id:int}")]
        [HasPermission("breedgroup.delete")]
        public async Task<IActionResult> DeleteBreedGroup(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteBreedGroupCommand(id), ct));
    }
}
