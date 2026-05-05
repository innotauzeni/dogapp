using Dog.Application.Common;
using Dog.Application.Features.Animals.DTOs;
using MediatR;

namespace Dog.Application.Features.Animals.Queries
{
    public record GetAnimalsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<AnimalListDto>>>;
    public record GetDogProfileQuery(int DogId) : IRequest<ApiResponse<DogProfileDto>>;
    public record GetMatingRecordsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<MatingRecordDto>>>;
    public record GetLittersByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<LitterDto>>>;

    // Reports queries
    public record GetAnimalsQuery : IRequest<ApiResponse<AnimalPageDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Gender { get; set; }
    }

    public record GetVaccinatedDogsCountQuery : IRequest<ApiResponse<object>>;
    public record GetVaccinatedDogsQuery : IRequest<ApiResponse<List<VaccinatedDogDto>>>;
    public record GetLittersCountQuery : IRequest<ApiResponse<object>>;
    public record GetLittersForReportQuery : IRequest<ApiResponse<List<LitterReportDto>>>;
    public record GetMatingRecordsCountQuery : IRequest<ApiResponse<object>>;
    public record GetMatingRecordsForReportQuery : IRequest<ApiResponse<List<MatingRecordReportDto>>>;
}
