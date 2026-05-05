using MediatR;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Features.Submodules.Commands;
using Dog.Application.Features.Submodules.DTOs;
using Dog.Application.Features.Submodules.Queries;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmodulesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SubmodulesController(IMediator mediator) => _mediator = mediator;

        [HttpGet("by-module/{moduleId:int}")]
        public async Task<IActionResult> GetByModule(int moduleId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetSubmodulesByModuleQuery(moduleId), ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetSubmoduleByIdQuery(id), ct));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubmoduleDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateSubmoduleCommand(dto), ct));

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSubmoduleDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateSubmoduleCommand(dto), ct));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteSubmoduleCommand(id), ct));
    }
}
