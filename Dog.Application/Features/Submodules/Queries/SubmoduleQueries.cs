using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Submodules.DTOs;

namespace Dog.Application.Features.Submodules.Queries
{
    public record GetSubmodulesByModuleQuery(int ModuleId) : IRequest<ApiResponse<List<SubmoduleDetailDto>>>;
    public record GetSubmoduleByIdQuery(int Id) : IRequest<ApiResponse<SubmoduleDetailDto>>;
}
