using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Roles.DTOs;

namespace Dog.Application.Features.Roles.Queries
{
    public record GetAllRolesQuery : IRequest<ApiResponse<List<RoleDto>>>;
    public record GetRoleByIdQuery(string Id) : IRequest<ApiResponse<RoleDto>>;
}
