using Dog.Application.Common;
using Dog.Application.Features.DogCertifications.DTOs;
using MediatR;

namespace Dog.Application.Features.DogCertifications.Queries
{
    public record GetCertificationsByDogQuery(int DogId) : IRequest<ApiResponse<List<DogCertificationDto>>>;
    public record GetDogCertificationByIdQuery(int Id) : IRequest<ApiResponse<DogCertificationDto>>;
}
