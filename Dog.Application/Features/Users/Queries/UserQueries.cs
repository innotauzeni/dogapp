using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Users.DTOs;

namespace Dog.Application.Features.Users.Queries
{
    public record GetAllUsersQuery(int? CompanyId = null) : IRequest<ApiResponse<List<UserDto>>>;
    public record GetUserByIdQuery(string Id) : IRequest<ApiResponse<UserDto>>;
}
