using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Auth.DTOs;

namespace Dog.Application.Features.Auth.Commands
{
    public record LoginCommand(LoginRequestDto Dto) : IRequest<ApiResponse<LoginResponseDto>>;
    public record RefreshTokenCommand(RefreshTokenRequestDto Dto) : IRequest<ApiResponse<LoginResponseDto>>;
}
