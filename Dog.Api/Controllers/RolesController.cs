using Dog.Application.Authorization;
using Dog.Application.Features.Roles.Commands;
using Dog.Application.Features.Roles.DTOs;
using Dog.Application.Features.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public RolesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("roles.view")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllRolesQuery(), ct));

        [HttpGet("{id}")]
        [HasPermission("roles.view")]
        public async Task<IActionResult> GetById(string id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetRoleByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("roles.create")]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateRoleCommand(dto), ct));

        [HttpPut("{id}")]
        [HasPermission("roles.edit")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRoleDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateRoleCommand(dto), ct));
        }

        [HttpDelete("{id}")]
        [HasPermission("roles.delete")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteRoleCommand(id), ct));


        [HttpGet("GetRoles")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoles(CancellationToken ct)
           => Ok(await _mediator.Send(new GetAllRolesQuery(), ct));
    }
}
