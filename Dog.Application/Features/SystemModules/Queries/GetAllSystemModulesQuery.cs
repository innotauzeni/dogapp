using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.SystemModules.DTOs;

namespace Dog.Application.Features.SystemModules.Queries
{
    public record GetAllSystemModulesQuery : IRequest<ApiResponse<List<SystemModuleDto>>>;

    public record GetSystemModuleByIdQuery(int Id) : IRequest<ApiResponse<SystemModuleDto>>;
}
