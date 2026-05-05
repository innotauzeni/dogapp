using Dog.Application.Common;
using Dog.Application.Features.Breeds.DTOs;
using MediatR;

namespace Dog.Application.Features.Breeds.Queries
{
    public record GetBreedByGroupQuery(int BreedGroupId) : IRequest<ApiResponse<List<BreedDto>>>;
    public record GetBreedByIdQuery(int Id) : IRequest<ApiResponse<BreedDto>>;
    public record GetAllBreedsQuery() : IRequest<ApiResponse<List<BreedDto>>>;
}
