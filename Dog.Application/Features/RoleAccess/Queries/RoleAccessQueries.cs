using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.RoleAccess.DTOs;

namespace Dog.Application.Features.RoleAccess.Queries
{
    public record GetRoleMenusQuery(string RoleId) : IRequest<ApiResponse<RoleMenusDto>>;
    public record GetRolePermissionsQuery(string RoleId) : IRequest<ApiResponse<RolePermissionsDto>>;
}
