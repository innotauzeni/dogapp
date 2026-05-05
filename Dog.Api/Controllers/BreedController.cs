using Dog.Application.Authorization;
using Dog.Application.Features.Breeds.Commands;
using Dog.Application.Features.Breeds.DTOs;
using Dog.Application.Features.Breeds.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BreedController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BreedController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetBreedsByGroup")]
        [HasPermission("breed.view")]
        public async Task<IActionResult> GetBreedsByGroup(int BreedGroupId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetBreedByGroupQuery(BreedGroupId), ct));

        [HttpGet("GetPubicBreeds")]
        public async Task<IActionResult> GetPubicBreeds(int BreedGroupId, CancellationToken ct)
          => Ok(await _mediator.Send(new GetBreedByGroupQuery(BreedGroupId), ct));

        [HttpGet("GetById/{id:int}")]
        [HasPermission("breed.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
          => Ok(await _mediator.Send(new GetBreedByIdQuery(id), ct));

        [HttpPost("CreateBreed")]
        [HasPermission("breed.create")]
        public async Task<IActionResult> CreateBreed([FromBody] CreateBreedDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateBreedCommand(dto), ct));

        [HttpPut("breed/{id:int}")]
        [HasPermission("breed.edit")]
        public async Task<IActionResult> UpdateBreed(int id, [FromBody] CreateBreedDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateBreedCommand(id, dto), ct));
        }

        [HttpDelete("breed/{id:int}")]
        [HasPermission("breed.delete")]
        public async Task<IActionResult> DeleteBreed(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteBreedCommand(id), ct));
    }
}
