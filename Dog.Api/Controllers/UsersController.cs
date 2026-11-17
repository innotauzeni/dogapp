using MediatR;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Authorization;
using Dog.Application.Features.Users.Commands;
using Dog.Application.Features.Users.DTOs;
using Dog.Application.Features.Users.Queries;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("users.view")]
        public async Task<IActionResult> GetAll([FromQuery] int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllUsersQuery(companyId), ct));

        [HttpGet("{id}")]
        [HasPermission("users.view")]
        public async Task<IActionResult> GetById(string id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetUserByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("users.create")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateUserCommand(dto, User.IsInRole("SystemOwner")), ct));

        [HttpPut("{id}")]
        [HasPermission("users.edit")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateUserCommand(dto, User.IsInRole("SystemOwner")), ct));
        }

        [HttpDelete("{id}")]
        [HasPermission("users.delete")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteUserCommand(id), ct));

        [HttpPost("{id}/reset-password")]
        [HasPermission("users.edit")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] string newPassword, CancellationToken ct)
            => Ok(await _mediator.Send(new ResetUserPasswordCommand(id, newPassword), ct));

        [HttpPut("{id}/assign-company/{companyId:int}")]
        [HasPermission("users.edit")]
        public async Task<IActionResult> AssignCompany(string id, int companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new AssignUserCompanyCommand(id, companyId), ct));
    }
}
