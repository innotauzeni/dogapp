using MediatR;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Authorization;
using Dog.Application.Features.Permissions.Commands;
using Dog.Application.Features.Permissions.DTOs;
using Dog.Application.Features.Permissions.Queries;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PermissionsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("permissions.view")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllPermissionsQuery(), ct));

        [HttpGet("by-module/{module}")]
        [HasPermission("permissions.view")]
        public async Task<IActionResult> GetByModule(string module, CancellationToken ct)
            => Ok(await _mediator.Send(new GetPermissionsByModuleQuery(module), ct));

        [HttpGet("{id:int}")]
        [HasPermission("permissions.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetPermissionByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("permissions.create")]
        public async Task<IActionResult> Create([FromBody] CreatePermissionDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreatePermissionCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("permissions.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePermissionDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdatePermissionCommand(dto), ct));
        }

        [HttpDelete("{id:int}")]
        [HasPermission("permissions.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeletePermissionCommand(id), ct));
    }
}
