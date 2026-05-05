using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Submodules.DTOs;

namespace Dog.Application.Features.Submodules.Commands
{
    public record CreateSubmoduleCommand(CreateSubmoduleDto Dto) : IRequest<ApiResponse<SubmoduleDetailDto>>;
    public record UpdateSubmoduleCommand(UpdateSubmoduleDto Dto) : IRequest<ApiResponse<SubmoduleDetailDto>>;
    public record DeleteSubmoduleCommand(int Id) : IRequest<ApiResponse<bool>>;
}
