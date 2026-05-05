using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Roles.DTOs;

namespace Dog.Application.Features.Roles.Commands
{
    public record CreateRoleCommand(CreateRoleDto Dto) : IRequest<ApiResponse<RoleDto>>;
    public record UpdateRoleCommand(UpdateRoleDto Dto) : IRequest<ApiResponse<RoleDto>>;
    public record DeleteRoleCommand(string Id) : IRequest<ApiResponse<bool>>;
}
