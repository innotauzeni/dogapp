using Dog.Application.Common;
using Dog.Application.Features.Animals.DTOs;
using MediatR;

namespace Dog.Application.Features.Animals.Queries
{
    public record GetAnimalsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<AnimalListDto>>>;
    public record GetDogProfileQuery(int DogId) : IRequest<ApiResponse<DogProfileDto>>;
    public record GetAssignedDogsQuery(string HandlerUserId) : IRequest<ApiResponse<List<AnimalListDto>>>;
    public record GetHandlerActivitiesQuery(string HandlerUserId) : IRequest<ApiResponse<List<HandlerActivityDto>>>;
    public record GetVaccineDueQuery(int CompanyId, int Days) : IRequest<ApiResponse<List<VaccineDueDto>>>;
    public record GetPuppyVaccinationsQuery(int PuppyId) : IRequest<ApiResponse<List<VaccinationRecordDto>>>;
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

    // CompanyId is optional: the mobile app scopes these to a company (expanded to
    // its branches for a head-office user); the web portal's system-wide reports
    // call these with no CompanyId to see every company.
    public record GetVaccinatedDogsCountQuery(int? CompanyId) : IRequest<ApiResponse<object>>;
    public record GetVaccinatedDogsQuery(int? CompanyId) : IRequest<ApiResponse<List<VaccinatedDogDto>>>;
    public record GetLittersCountQuery(int? CompanyId) : IRequest<ApiResponse<object>>;
    public record GetLittersForReportQuery : IRequest<ApiResponse<List<LitterReportDto>>>;
    public record GetMatingRecordsCountQuery(int? CompanyId) : IRequest<ApiResponse<object>>;
    public record GetMatingRecordsForReportQuery : IRequest<ApiResponse<List<MatingRecordReportDto>>>;
    public record GetPuppiesForReportQuery : IRequest<ApiResponse<List<PuppyReportDto>>>;
}
