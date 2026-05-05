using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Permissions.DTOs;

namespace Dog.Application.Features.Permissions.Queries
{
    public record GetAllPermissionsQuery : IRequest<ApiResponse<List<PermissionDto>>>;
    public record GetPermissionsByModuleQuery(string Module) : IRequest<ApiResponse<List<PermissionDto>>>;
    public record GetPermissionByIdQuery(int Id) : IRequest<ApiResponse<PermissionDto>>;
}
