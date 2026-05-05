using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Permissions.DTOs;

namespace Dog.Application.Features.Permissions.Commands
{
    public record CreatePermissionCommand(CreatePermissionDto Dto) : IRequest<ApiResponse<PermissionDto>>;
    public record UpdatePermissionCommand(UpdatePermissionDto Dto) : IRequest<ApiResponse<PermissionDto>>;
    public record DeletePermissionCommand(int Id) : IRequest<ApiResponse<bool>>;
}
