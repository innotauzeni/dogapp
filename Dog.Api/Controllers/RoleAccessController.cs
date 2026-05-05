using MediatR;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Authorization;
using Dog.Application.Features.RoleAccess.Commands;
using Dog.Application.Features.RoleAccess.DTOs;
using Dog.Application.Features.RoleAccess.Queries;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleAccessController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RoleAccessController(IMediator mediator) => _mediator = mediator;

        [HttpGet("{roleId}/menus")]
        [HasPermission("roleaccess.view")]
        public async Task<IActionResult> GetRoleMenus(string roleId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetRoleMenusQuery(roleId), ct));

        [HttpGet("{roleId}/permissions")]
        [HasPermission("roleaccess.view")]
        public async Task<IActionResult> GetRolePermissions(string roleId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetRolePermissionsQuery(roleId), ct));

        [HttpPost("assign-menus")]
        [HasPermission("roleaccess.assignmenu")]
        public async Task<IActionResult> AssignMenus([FromBody] AssignMenuToRoleDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AssignMenusToRoleCommand(dto), ct));

        [HttpPost("assign-permissions")]
        [HasPermission("roleaccess.assignpermission")]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsToRoleDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AssignPermissionsToRoleCommand(dto), ct));
    }
}
