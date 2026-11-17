using Dog.Application.Common;
using Dog.Application.Features.DogTraining.DTOs;
using MediatR;

namespace Dog.Application.Features.DogTraining.Commands
{
    // Programmes
    public record SaveProgrammeCommand(SaveProgrammeDto Dto) : IRequest<ApiResponse<ProgrammeDto>>;
    public record DeleteProgrammeCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record SeedDefaultProgrammeCommand(int CompanyId) : IRequest<ApiResponse<ProgrammeDto>>;

    // Training customers / dogs / invoices / payments
    public record SaveTrainingCustomerCommand(SaveTrainingCustomerDto Dto) : IRequest<ApiResponse<TrainingCustomerDto>>;
    public record DeleteTrainingCustomerCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record SaveTrainingDogCommand(SaveTrainingDogDto Dto) : IRequest<ApiResponse<TrainingDogDto>>;
    public record DeleteTrainingDogCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record RecordTrainingPaymentCommand(TrainingActor Actor, RecordTrainingPaymentDto Dto) : IRequest<ApiResponse<TrainingInvoiceDto>>;
    public record ConfirmTrainingPaymentCommand(TrainingActor Actor, int PaymentId, string? StatementReference) : IRequest<ApiResponse<TrainingInvoiceDto>>;
    public record RejectTrainingPaymentCommand(TrainingActor Actor, int PaymentId, string? Reason) : IRequest<ApiResponse<TrainingInvoiceDto>>;
    public record CancelTrainingInvoiceCommand(int InvoiceId) : IRequest<ApiResponse<TrainingInvoiceDto>>;

    // Enrolments
    public record CreateEnrollmentCommand(TrainingActor Actor, CreateEnrollmentDto Dto) : IRequest<ApiResponse<EnrollmentDetailDto>>;
    public record CancelEnrollmentCommand(int EnrollmentId, string? Reason) : IRequest<ApiResponse<bool>>;
    public record GenerateTrainingInvoiceCommand(TrainingActor Actor, GenerateInvoiceDto Dto) : IRequest<ApiResponse<EnrollmentDetailDto>>;
    public record ActivateTrainingCommand(TrainingActor Actor, ActivateTrainingDto Dto) : IRequest<ApiResponse<EnrollmentDetailDto>>;

    // Health assessment
    public record SaveHealthAssessmentCommand(TrainingActor Actor, SaveHealthAssessmentDto Dto) : IRequest<ApiResponse<HealthAssessmentDto>>;
    public record DecideHealthAssessmentCommand(TrainingActor Actor, DecideHealthDto Dto) : IRequest<ApiResponse<HealthAssessmentDto>>;

    // Handlers
    public record SaveHandlerProfileCommand(SaveHandlerProfileDto Dto) : IRequest<ApiResponse<HandlerCapacityDto>>;
    public record AssignHandlerCommand(TrainingActor Actor, AssignHandlerDto Dto) : IRequest<ApiResponse<EnrollmentDetailDto>>;

    // Plans / logbook / reports
    public record SaveWeeklyPlanCommand(SaveWeeklyPlanDto Dto) : IRequest<ApiResponse<WeeklyPlanDto>>;
    public record SaveDailyLogCommand(TrainingActor Actor, SaveDailyLogDto Dto) : IRequest<ApiResponse<DailyLogDto>>;
    public record SaveProgressReportCommand(TrainingActor Actor, SaveProgressReportDto Dto) : IRequest<ApiResponse<ProgressReportDto>>;
    public record ReviewProgressReportCommand(TrainingActor Actor, ReviewReportDto Dto) : IRequest<ApiResponse<ProgressReportDto>>;

    // Final assessment / certificate
    public record SaveFinalAssessmentCommand(TrainingActor Actor, SaveFinalAssessmentDto Dto) : IRequest<ApiResponse<FinalAssessmentDto>>;
    /// <summary>Training Manager extends training after a failed / "requires additional training" assessment.</summary>
    public record ExtendTrainingCommand(TrainingActor Actor, int EnrollmentId, int ExtensionDays, string? AdditionalActivities) : IRequest<ApiResponse<EnrollmentDetailDto>>;
    public record ApproveFinalAssessmentCommand(TrainingActor Actor, ApproveAssessmentDto Dto) : IRequest<ApiResponse<FinalAssessmentDto>>;
    public record IssueTrainingCertificateCommand(TrainingActor Actor, int AssessmentId) : IRequest<ApiResponse<TrainingCertificateDto>>;
    public record RevokeTrainingCertificateCommand(int CertificateId, string Reason) : IRequest<ApiResponse<bool>>;
}

namespace Dog.Application.Features.DogTraining.Commands
{
    /// <summary>Attach a photo / video / document to a health assessment, daily log or progress report.
    /// Stored as a DocumentAttachment (EntityType TrainingHealth / TrainingLog / TrainingReport).</summary>
    public record AddTrainingAttachmentCommand(TrainingActor Actor, string EntityType, int EntityId, string FileName, string FileUrl, string? ContentType, string? Notes)
        : IRequest<ApiResponse<bool>>;
    public record DeleteTrainingAttachmentCommand(TrainingActor Actor, string EntityType, int EntityId, int AttachmentId) : IRequest<ApiResponse<bool>>;
}
