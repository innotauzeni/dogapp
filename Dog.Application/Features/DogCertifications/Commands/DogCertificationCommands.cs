using Dog.Application.Common;
using Dog.Application.Features.DogCertifications.DTOs;
using MediatR;

namespace Dog.Application.Features.DogCertifications.Commands
{
    public record CreateDogCertificationCommand(CreateDogCertificationDto Dto) : IRequest<ApiResponse<DogCertificationDto>>;
    public record UpdateDogCertificationCommand(int Id, UpdateDogCertificationDto Dto) : IRequest<ApiResponse<DogCertificationDto>>;
    public record DeleteDogCertificationCommand(int Id) : IRequest<ApiResponse<bool>>;
}
