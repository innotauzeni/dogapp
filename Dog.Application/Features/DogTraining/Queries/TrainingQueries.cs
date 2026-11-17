using Dog.Application.Common;
using Dog.Application.Features.DogTraining.DTOs;
using MediatR;

namespace Dog.Application.Features.DogTraining.Queries
{
    public record GetTrainingCustomersQuery(int CompanyId, string? Search) : IRequest<ApiResponse<List<TrainingCustomerDto>>>;
    public record GetTrainingDogsQuery(int CompanyId, int? CustomerId, string? Search) : IRequest<ApiResponse<List<TrainingDogDto>>>;
    public record GetTrainingInvoicesQuery(TrainingActor Actor, string? Status, string? Search) : IRequest<ApiResponse<List<TrainingInvoiceDto>>>;
    public record GetTrainingInvoiceByIdQuery(int Id) : IRequest<ApiResponse<TrainingInvoiceDto>>;

    public record GetProgrammesQuery(int CompanyId, bool ActiveOnly = false) : IRequest<ApiResponse<List<ProgrammeDto>>>;

    /// <summary>Filters shared by the list screens. Status may be a comma-separated list.</summary>
    public record EnrollmentFilter(string? Status = null, string? HandlerUserId = null, int? ProgrammeId = null,
        DateTime? From = null, DateTime? To = null, string? Search = null, int? DogId = null);

    public record GetEnrollmentsQuery(TrainingActor Actor, EnrollmentFilter Filter) : IRequest<ApiResponse<List<EnrollmentListDto>>>;
    public record GetEnrollmentByIdQuery(TrainingActor Actor, int Id) : IRequest<ApiResponse<EnrollmentDetailDto>>;

    public record GetHealthAssessmentsQuery(TrainingActor Actor, string? Result, int? EnrollmentId) : IRequest<ApiResponse<List<HealthAssessmentDto>>>;
    public record GetHealthAssessmentByIdQuery(TrainingActor Actor, int Id) : IRequest<ApiResponse<HealthAssessmentDto>>;

    public record GetHandlerCapacityQuery(int CompanyId) : IRequest<ApiResponse<List<HandlerCapacityDto>>>;

    public record GetWeeklyPlansQuery(TrainingActor Actor, int EnrollmentId) : IRequest<ApiResponse<List<WeeklyPlanDto>>>;
    public record GetDailyLogsQuery(TrainingActor Actor, int? EnrollmentId, DateTime? Date, DateTime? From, DateTime? To) : IRequest<ApiResponse<List<DailyLogDto>>>;
    public record GetDailyLogForDayQuery(TrainingActor Actor, int EnrollmentId, DateTime Date) : IRequest<ApiResponse<DailyLogDto>>;

    /// <summary>The handler's own dogs currently in training, with today's schedule — the mobile home screen.</summary>
    public record GetMyTrainingDogsQuery(TrainingActor Actor) : IRequest<ApiResponse<List<EnrollmentListDto>>>;

    public record GetProgressReportsQuery(TrainingActor Actor, string? Status, int? EnrollmentId) : IRequest<ApiResponse<List<ProgressReportDto>>>;
    public record GetProgressReportByIdQuery(TrainingActor Actor, int Id) : IRequest<ApiResponse<ProgressReportDto>>;
    /// <summary>Pre-filled draft for a week (objectives from the plan, activities from the daily logs).</summary>
    public record GetProgressReportDraftQuery(TrainingActor Actor, int EnrollmentId, int WeekNumber) : IRequest<ApiResponse<ProgressReportDto>>;

    public record GetFinalAssessmentsQuery(TrainingActor Actor, string? Result, int? EnrollmentId) : IRequest<ApiResponse<List<FinalAssessmentDto>>>;
    public record GetFinalAssessmentByIdQuery(TrainingActor Actor, int Id) : IRequest<ApiResponse<FinalAssessmentDto>>;

    public record GetTrainingCertificatesQuery(TrainingActor Actor) : IRequest<ApiResponse<List<TrainingCertificateDto>>>;
    public record GetTrainingCertificateByIdQuery(int Id) : IRequest<ApiResponse<TrainingCertificateDto>>;
    /// <summary>Public lookup used by the verification page / QR code. No authentication.</summary>
    public record VerifyTrainingCertificateQuery(string Token) : IRequest<ApiResponse<CertificateVerificationDto>>;

    public record GetTrainingDashboardQuery(TrainingActor Actor, string? HandlerUserId, int? ProgrammeId, DateTime? From, DateTime? To)
        : IRequest<ApiResponse<TrainingDashboardDto>>;
    public record GetTrainingReportQuery(TrainingActor Actor, DateTime? From, DateTime? To) : IRequest<ApiResponse<TrainingReportDto>>;
}

namespace Dog.Application.Features.DogTraining.Queries
{
    public record GetTrainingAttachmentsQuery(TrainingActor Actor, string EntityType, int EntityId)
        : IRequest<ApiResponse<List<Dog.Application.Features.Documents.DTOs.DocumentAttachmentDto>>>;
}
