using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.RoleAccess.DTOs;

namespace Dog.Application.Features.RoleAccess.Commands
{
    /// <summary>Replaces all menu assignments for a role (full sync)</summary>
    public record AssignMenusToRoleCommand(AssignMenuToRoleDto Dto) : IRequest<ApiResponse<bool>>;

    /// <summary>Replaces all permission assignments for a role (full sync)</summary>
    public record AssignPermissionsToRoleCommand(AssignPermissionsToRoleDto Dto) : IRequest<ApiResponse<bool>>;
}
