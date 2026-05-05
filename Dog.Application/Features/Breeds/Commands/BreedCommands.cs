using Dog.Application.Common;
using Dog.Application.Features.Breeds.DTOs;
using MediatR;

namespace Dog.Application.Features.Breeds.Commands
{
    public record CreateBreedCommand(CreateBreedDto Dto) : IRequest<ApiResponse<BreedDto>>;
    public record UpdateBreedCommand(int Id, CreateBreedDto Dto) : IRequest<ApiResponse<BreedDto>>;
    public record DeleteBreedCommand(int Id) : IRequest<ApiResponse<bool>>;
}
