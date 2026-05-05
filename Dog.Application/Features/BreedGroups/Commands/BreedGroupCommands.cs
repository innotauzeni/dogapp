using Dog.Application.Common;
using Dog.Application.Features.BreedGroups.DTOs;
using MediatR;

namespace Dog.Application.Features.BreedGroups.Commands
{
    public record CreateBreedGroupCommand(CreateBreedGroupDto Dto) : IRequest<ApiResponse<BreedGroupDto>>;
    public record UpdateBreedGroupCommand(int Id, CreateBreedGroupDto Dto) : IRequest<ApiResponse<BreedGroupDto>>;
    public record DeleteBreedGroupCommand(int Id) : IRequest<ApiResponse<bool>>;
}
