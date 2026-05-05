using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.SystemModules.DTOs;

namespace Dog.Application.Features.SystemModules.Commands
{
    public record CreateSystemModuleCommand(CreateSystemModuleDto Dto) : IRequest<ApiResponse<SystemModuleDto>>;

    public record UpdateSystemModuleCommand(UpdateSystemModuleDto Dto) : IRequest<ApiResponse<SystemModuleDto>>;

    public record DeleteSystemModuleCommand(int Id) : IRequest<ApiResponse<bool>>;
}
