using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Users.DTOs;

namespace Dog.Application.Features.Users.Commands
{
    public record CreateUserCommand(CreateUserDto Dto) : IRequest<ApiResponse<UserDto>>;
    public record UpdateUserCommand(UpdateUserDto Dto) : IRequest<ApiResponse<UserDto>>;
    public record DeleteUserCommand(string Id) : IRequest<ApiResponse<bool>>;

    public record AssignUserCompanyCommand(string UserId, int CompanyId) : IRequest<ApiResponse<bool>>;
    public record ResetUserPasswordCommand(string UserId, string NewPassword) : IRequest<ApiResponse<bool>>;
}
