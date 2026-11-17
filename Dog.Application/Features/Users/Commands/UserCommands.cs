using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Users.DTOs;

namespace Dog.Application.Features.Users.Commands
{
    // CallerIsSystemOwner is set by the controller from the caller's own role claim
    // (never trust a client-supplied value for this) — Portal and mobile already hide
    // SystemOwner/Admin from a non-SystemOwner's role picker, but that's UI-only; this
    // is the actual enforcement so the same escalation can't happen via a raw API call.
    public record CreateUserCommand(CreateUserDto Dto, bool CallerIsSystemOwner = false) : IRequest<ApiResponse<UserDto>>;
    public record UpdateUserCommand(UpdateUserDto Dto, bool CallerIsSystemOwner = false) : IRequest<ApiResponse<UserDto>>;
    public record DeleteUserCommand(string Id) : IRequest<ApiResponse<bool>>;

    public record AssignUserCompanyCommand(string UserId, int CompanyId) : IRequest<ApiResponse<bool>>;
    public record ResetUserPasswordCommand(string UserId, string NewPassword) : IRequest<ApiResponse<bool>>;
}
