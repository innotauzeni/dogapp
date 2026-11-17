using Dog.Application.Common;
using Dog.Application.Features.Animals.DTOs;
using MediatR;

namespace Dog.Application.Features.Animals.Commands
{
    public record CreateAnimalCommand(CreateAnimalDto Dto) : IRequest<ApiResponse<AnimalDto>>;
    public record UpdateAnimalCommand(UpdateAnimalDto Dto) : IRequest<ApiResponse<AnimalDto>>;
    public record DeleteAnimalCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record ToggleAnimalCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record AddVaccinationCommand(AddVaccinationDto Dto) : IRequest<ApiResponse<bool>>;
    public record AddMatingRecordCommand(AddMatingRecordDto Dto) : IRequest<ApiResponse<bool>>;
    public record AddLitterCommand(AddLitterDto Dto) : IRequest<ApiResponse<bool>>;
    public record AddPuppyCommand(AddPuppyDto Dto) : IRequest<ApiResponse<bool>>;

    /// <summary>Assign (or, with null CustomerId, unassign) a dog's owner.</summary>
    public record AssignAnimalCustomerCommand(int DogId, int? CustomerId) : IRequest<ApiResponse<bool>>;
    public record AssignDogCommand(AssignDogDto Dto) : IRequest<ApiResponse<bool>>;
    public record AddDogCareRecordCommand(AddDogCareRecordDto Dto) : IRequest<ApiResponse<bool>>;
    public record UpdateDogCareRecordCommand(UpdateDogCareRecordDto Dto) : IRequest<ApiResponse<bool>>;
}
