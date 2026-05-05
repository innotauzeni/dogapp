using Dog.Application.Common;
using Dog.Application.Features.BreedGroups.DTOs;
using MediatR;

namespace Dog.Application.Features.BreedGroups.Queries
{
    public record GetBreedGroupByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<BreedGroupDto>>>;
    public record GetBreedGroupByIdQuery(int Id) : IRequest<ApiResponse<BreedGroupDto>>;
    public record GetAllBreedGroupsQuery() : IRequest<ApiResponse<List<BreedGroupDto>>>;
}
