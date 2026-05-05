using MediatR;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Authorization;
using Dog.Application.Features.SystemModules.Commands;
using Dog.Application.Features.SystemModules.DTOs;
using Dog.Application.Features.SystemModules.Queries;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemModulesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SystemModulesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("systemmodules.view")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllSystemModulesQuery(), ct));

        [HttpGet("{id:int}")]
        [HasPermission("systemmodules.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetSystemModuleByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("systemmodules.create")]
        public async Task<IActionResult> Create([FromBody] CreateSystemModuleDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateSystemModuleCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("systemmodules.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSystemModuleDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateSystemModuleCommand(dto), ct));
        }

        [HttpDelete("{id:int}")]
        [HasPermission("systemmodules.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteSystemModuleCommand(id), ct));
    }
}
