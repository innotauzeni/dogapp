namespace Dog.Application.Features.DogTraining.DTOs
{
    /// <summary>The caller, built by the API controller from the JWT. Handlers that are not managers only see
    /// the dogs allocated to them (unless <see cref="CanManageAll"/>).</summary>
    public record TrainingActor(string UserId, string Name, int CompanyId, bool CanManageAll, bool CanVerifyPayments = false);

    // ── Programmes ───────────────────────────────────────────────────────────
    public class ProgrammeWeekDto
    {
        public int WeekNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Focus { get; set; }
        public string? Targets { get; set; }
    }

    public class ProgrammeActivityDto
    {
        public int WeekNumber { get; set; }            // 0 = every week
        public string TimeOfDay { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Purpose { get; set; }
        public int SortOrder { get; set; }
    }

    public class ProgrammeDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationDays { get; set; }
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
        public int EnrolmentCount { get; set; }
        public List<ProgrammeWeekDto> Weeks { get; set; } = new();
        public List<ProgrammeActivityDto> Activities { get; set; } = new();
    }

    public class SaveProgrammeDto
    {
        public int? Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationDays { get; set; } = 21;
        public decimal Fee { get; set; }
        public bool IsActive { get; set; } = true;
        public List<ProgrammeWeekDto> Weeks { get; set; } = new();
        public List<ProgrammeActivityDto> Activities { get; set; } = new();
    }

    // ── Enrolments ───────────────────────────────────────────────────────────
    public class EnrollmentListDto
    {
        public int Id { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
        public int DogId { get; set; }
        public string DogName { get; set; } = string.Empty;
        public string? DogBreed { get; set; }
        public string? DogPhotoUrl { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int ProgrammeId { get; set; }
        public string ProgrammeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? HandlerUserId { get; set; }
        public string? HandlerName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public int DurationDays { get; set; }
        public int ExtensionDays { get; set; }
        /// <summary>0 when training has not started.</summary>
        public int CurrentWeek { get; set; }
        public int DayNumber { get; set; }
        public decimal Fee { get; set; }
        public decimal AdditionalCharges { get; set; }
        public decimal Total { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceStatus { get; set; }
        public decimal? InvoiceBalance { get; set; }
        public string? LatestHealthResult { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EnrollmentDetailDto : EnrollmentListDto
    {
        public string? Notes { get; set; }
        public string? AdditionalChargesNote { get; set; }
        public string? CancelReason { get; set; }
        public int CompanyId { get; set; }
        public List<HealthAssessmentDto> HealthAssessments { get; set; } = new();
        public List<HandlerAssignmentDto> Assignments { get; set; } = new();
        public List<WeeklyPlanDto> WeeklyPlans { get; set; } = new();
        public List<FinalAssessmentDto> FinalAssessments { get; set; } = new();
        public TrainingCertificateDto? Certificate { get; set; }
        public int DailyLogCount { get; set; }
        public int ReportCount { get; set; }
        public bool CanInvoice { get; set; }
        public bool CanAssignHandler { get; set; }
        public bool CanActivate { get; set; }
    }

    public class CreateEnrollmentDto
    {
        public int CompanyId { get; set; }
        public int DogId { get; set; }
        public int ProgrammeId { get; set; }
        public decimal AdditionalCharges { get; set; }
        public string? AdditionalChargesNote { get; set; }
        public string? Notes { get; set; }
    }

    public class GenerateInvoiceDto
    {
        public int EnrollmentId { get; set; }
        public int DueInDays { get; set; } = 7;
        public string? Notes { get; set; }
    }

    // ── Health assessment ────────────────────────────────────────────────────
    public class HealthItemDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = "Exam";     // Exam | Treatment
        public string Name { get; set; } = string.Empty;
        public string? Status { get; set; }                  // Normal | Abnormal
        public string? Notes { get; set; }
        public bool Administered { get; set; }
        public DateTime? TreatmentDate { get; set; }
        public string? Reason { get; set; }
        public bool BesNotified { get; set; }
        public int SortOrder { get; set; }
    }

    public class HealthAssessmentDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public string? EnrollmentNumber { get; set; }
        public int DogId { get; set; }
        public string DogName { get; set; } = string.Empty;
        public string? DogBreed { get; set; }
        public string? DogGender { get; set; }
        public DateTime? DogDateOfBirth { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public DateTime AssessmentDate { get; set; }
        public string? ExaminerUserId { get; set; }
        public string? ExaminerName { get; set; }
        public string? ForceNumber { get; set; }
        public string? ExaminerPhone { get; set; }
        public string? DogHandlerName { get; set; }
        public string? Species { get; set; }
        public decimal? WeightKg { get; set; }
        public string? PhysicalCondition { get; set; }
        public string? VaccinationStatus { get; set; }
        public string? DewormingStatus { get; set; }
        public string? MedicalConditions { get; set; }
        public DateTime? FleaTickTreatmentDate { get; set; }
        public bool CertGoodHealth { get; set; }
        public bool CertVaccinationsCurrent { get; set; }
        public bool CertNoParasites { get; set; }
        public bool CertNoBiteHistory { get; set; }
        public string Recommendation { get; set; } = "None";
        public string? Comments { get; set; }
        public string? InspectorSignatureName { get; set; }
        public DateTime? SignedOn { get; set; }
        public string Result { get; set; } = "Pending";
        public DateTime? ResultDate { get; set; }
        public List<HealthItemDto> Items { get; set; } = new();
        public int DocumentCount { get; set; }
    }

    public class SaveHealthAssessmentDto
    {
        public int? Id { get; set; }
        public int EnrollmentId { get; set; }
        public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
        public string? ExaminerName { get; set; }
        public string? ForceNumber { get; set; }
        public string? ExaminerPhone { get; set; }
        public string? DogHandlerName { get; set; }
        public string? Species { get; set; } = "Dog";
        public decimal? WeightKg { get; set; }
        public string? PhysicalCondition { get; set; }
        public string? VaccinationStatus { get; set; }
        public string? DewormingStatus { get; set; }
        public string? MedicalConditions { get; set; }
        public DateTime? FleaTickTreatmentDate { get; set; }
        public bool CertGoodHealth { get; set; }
        public bool CertVaccinationsCurrent { get; set; }
        public bool CertNoParasites { get; set; }
        public bool CertNoBiteHistory { get; set; }
        public string Recommendation { get; set; } = "None";
        public string? Comments { get; set; }
        public string? InspectorSignatureName { get; set; }
        public DateTime? SignedOn { get; set; }
        public List<HealthItemDto> Items { get; set; } = new();
    }

    public class DecideHealthDto
    {
        public int AssessmentId { get; set; }
        /// <summary>Passed | Failed | RequiresFurtherExamination</summary>
        public string Result { get; set; } = "Passed";
        public string? Comments { get; set; }
    }

    // ── Handlers ─────────────────────────────────────────────────────────────
    public class AssignedDogDto
    {
        public int EnrollmentId { get; set; }
        public string DogName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CurrentWeek { get; set; }
    }

    public class HandlerCapacityDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int MaxDogs { get; set; }
        public int CurrentDogs { get; set; }
        public string? Specialisation { get; set; }
        public bool IsAvailable { get; set; }
        public bool CanAccept { get; set; }
        public string? Notes { get; set; }
        public List<AssignedDogDto> Dogs { get; set; } = new();
    }

    public class SaveHandlerProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public int MaxDogs { get; set; } = 5;
        public string? Specialisation { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class AssignHandlerDto
    {
        public int EnrollmentId { get; set; }
        public string HandlerUserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class HandlerAssignmentDto
    {
        public int Id { get; set; }
        public string HandlerUserId { get; set; } = string.Empty;
        public string? HandlerName { get; set; }
        public DateTime AssignedOn { get; set; }
        public string? AssignedByName { get; set; }
        public string? Notes { get; set; }
        public DateTime? EndedOn { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class ActivateTrainingDto
    {
        public int EnrollmentId { get; set; }
        public DateTime? StartDate { get; set; }
    }

    // ── Weekly plans / daily log / progress reports ──────────────────────────
    public class ScheduleSlotDto
    {
        public string Time { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Purpose { get; set; }
    }

    public class WeeklyPlanDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int WeekNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Focus { get; set; }
        public string? Targets { get; set; }
        public List<ScheduleSlotDto> Schedule { get; set; } = new();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SaveWeeklyPlanDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Focus { get; set; }
        public string? Targets { get; set; }
        public List<ScheduleSlotDto> Schedule { get; set; } = new();
    }

    public class LogSlotDto
    {
        public string Time { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool Done { get; set; }
        /// <summary>0-10, as on the weekly plan sheets.</summary>
        public int? Rating { get; set; }
        public string? Notes { get; set; }
        /// <summary>The handler decided this activity was not done today (different from 'not logged yet').</summary>
        public bool Skipped { get; set; }
        /// <summary>When this activity was logged (the handler's own clock) - so each stage of the day can be logged as it happens.</summary>
        public DateTime? LoggedAt { get; set; }
    }

    public class DailyLogDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int DogId { get; set; }
        public string DogName { get; set; } = string.Empty;
        public string HandlerUserId { get; set; } = string.Empty;
        public string? HandlerName { get; set; }
        public DateTime TrainingDate { get; set; }
        public int WeekNumber { get; set; }
        public int DayNumber { get; set; }
        public string? ActivitiesPerformed { get; set; }
        public string? CommandsPracticed { get; set; }
        public int DurationMinutes { get; set; }
        public List<LogSlotDto> Slots { get; set; } = new();
        public decimal? AverageSlotRating { get; set; }
        public string Performance { get; set; } = "Good";
        public string? BehaviourObservations { get; set; }
        public string? Challenges { get; set; }
        public string? Improvements { get; set; }
        public string? Recommendations { get; set; }
        public string? NextObjectives { get; set; }
    }

    public class SaveDailyLogDto
    {
        public int EnrollmentId { get; set; }
        public DateTime TrainingDate { get; set; } = DateTime.Today;
        public string? ActivitiesPerformed { get; set; }
        public string? CommandsPracticed { get; set; }
        public int DurationMinutes { get; set; }
        public List<LogSlotDto> Slots { get; set; } = new();
        public string? Performance { get; set; }
        public string? BehaviourObservations { get; set; }
        public string? Challenges { get; set; }
        public string? Improvements { get; set; }
        public string? Recommendations { get; set; }
        public string? NextObjectives { get; set; }
    }

    public class ProgressReportDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public string? EnrollmentNumber { get; set; }
        public string DogName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int WeekNumber { get; set; }
        public string HandlerUserId { get; set; } = string.Empty;
        public string? HandlerName { get; set; }
        public string? Objectives { get; set; }
        public string? ActivitiesCompleted { get; set; }
        public string? ActivitiesOutstanding { get; set; }
        public string? PerformanceSummary { get; set; }
        public string? Challenges { get; set; }
        public string? Improvements { get; set; }
        public string? Recommendations { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime? SubmittedOn { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedOn { get; set; }
        public string? ReviewComments { get; set; }
        public decimal? AverageRating { get; set; }
        public int LogsInWeek { get; set; }
    }

    public class SaveProgressReportDto
    {
        public int EnrollmentId { get; set; }
        public int WeekNumber { get; set; }
        public string? Objectives { get; set; }
        public string? ActivitiesCompleted { get; set; }
        public string? ActivitiesOutstanding { get; set; }
        public string? PerformanceSummary { get; set; }
        public string? Challenges { get; set; }
        public string? Improvements { get; set; }
        public string? Recommendations { get; set; }
        /// <summary>True = submit for review; false = keep as draft.</summary>
        public bool Submit { get; set; }
    }

    public class ReviewReportDto
    {
        public int ReportId { get; set; }
        /// <summary>True = approve, false = return to the handler for changes.</summary>
        public bool Approve { get; set; }
        public string? Comments { get; set; }
    }

    // ── Final assessment / certificate ───────────────────────────────────────
    public class AssessmentAreaDto
    {
        public string Area { get; set; } = string.Empty;
        public int Score { get; set; }
        public string? Comments { get; set; }
    }

    public class FinalAssessmentDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public string? EnrollmentNumber { get; set; }
        public string DogName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime AssessmentDate { get; set; }
        public string? AssessorName { get; set; }
        public List<AssessmentAreaDto> Items { get; set; } = new();
        public decimal? OverallScore { get; set; }
        public string Result { get; set; } = "Passed";
        public string? Comments { get; set; }
        public int ExtensionDays { get; set; }
        public string? AdditionalActivities { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string? ApprovalComments { get; set; }
        public string? CertificateNumber { get; set; }
    }

    public class SaveFinalAssessmentDto
    {
        public int EnrollmentId { get; set; }
        public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
        public List<AssessmentAreaDto> Items { get; set; } = new();
        /// <summary>Passed | Failed | RequiresAdditionalTraining</summary>
        public string Result { get; set; } = "Passed";
        public string? Comments { get; set; }
        public int ExtensionDays { get; set; }
        public string? AdditionalActivities { get; set; }
    }

    public class ApproveAssessmentDto
    {
        public int AssessmentId { get; set; }
        public bool Approve { get; set; }
        public string? Comments { get; set; }
    }

    public class TrainingCertificateDto
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string VerificationToken { get; set; } = string.Empty;
        public DateTime IssuedOn { get; set; }
        public string DogName { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public string? OwnerName { get; set; }
        public string ProgrammeName { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string? HandlerName { get; set; }
        public decimal? OverallScore { get; set; }
        public bool IsRevoked { get; set; }
        public string? RevokedReason { get; set; }
        public int CompanyId { get; set; }
    }

    public class CertificateVerificationDto
    {
        public bool Valid { get; set; }
        public string Message { get; set; } = string.Empty;
        public TrainingCertificateDto? Certificate { get; set; }
        public string? CompanyName { get; set; }
    }

    // ── Dashboard / reports ──────────────────────────────────────────────────
    public class TrainingDashboardDto
    {
        public int AwaitingHealthAssessment { get; set; }
        public int HealthPassed { get; set; }
        public int AwaitingPayment { get; set; }
        public int AwaitingHandler { get; set; }
        public int ReadyToActivate { get; set; }
        public int InTraining { get; set; }
        public int Week1 { get; set; }
        public int Week2 { get; set; }
        public int Week3 { get; set; }
        public int AwaitingFinalAssessment { get; set; }
        public int AwaitingApproval { get; set; }
        public int RequiringAdditionalTraining { get; set; }
        public int Completed { get; set; }
        public int CertificatesIssued { get; set; }
        public int ReportsAwaitingReview { get; set; }
        public int HealthFailed { get; set; }
        public int Cancelled { get; set; }
        public List<EnrollmentListDto> Recent { get; set; } = new();
    }

    public class TrainingReportRowDto
    {
        public string Label { get; set; } = string.Empty;
        public int Enrolments { get; set; }
        public int Completed { get; set; }
        public int InTraining { get; set; }
        public decimal? AverageScore { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TrainingReportDto
    {
        public List<TrainingReportRowDto> ByProgramme { get; set; } = new();
        public List<TrainingReportRowDto> ByHandler { get; set; } = new();
        public List<TrainingReportRowDto> ByMonth { get; set; } = new();
        public int TotalEnrolments { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalFailedAssessments { get; set; }
        public decimal PassRatePercent { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // ── Training customers, dogs, invoices & payments (separate from the company's own dogs / invoices) ──
    public class TrainingCustomerDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? IdNumber { get; set; }
        public string? Notes { get; set; }
        public int DogCount { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class SaveTrainingCustomerDto
    {
        public int? Id { get; set; }
        public int CompanyId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? IdNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class TrainingDogDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public string? Color { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? MicrochipNumber { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Notes { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class SaveTrainingDogDto
    {
        public int? Id { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public string? Color { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? MicrochipNumber { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Notes { get; set; }
    }

    public class TrainingInvoiceItemDto
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class TrainingPaymentDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string? ReceivedByName { get; set; }
        public DateTime? ValueDate { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public string Status { get; set; } = "CONFIRMED";
        public string? MethodLabel { get; set; }
        public string? PayerName { get; set; }
        public string? PayerContact { get; set; }
        public string? PayerBank { get; set; }
        public string? PayerAccount { get; set; }
        public int? DestinationBankAccountId { get; set; }
        public string? DestinationAccountLabel { get; set; }
        public string? ChequeNumber { get; set; }
        public string? ProofUrl { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? StatementReference { get; set; }
        public string? RejectReason { get; set; }
    }

    public class TrainingInvoiceDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? EnrollmentId { get; set; }
        public string? EnrollmentNumber { get; set; }
        public string? DogName { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public List<TrainingInvoiceItemDto> Items { get; set; } = new();
        public List<TrainingPaymentDto> Payments { get; set; } = new();
        public decimal PendingAmount { get; set; }
        public DateTime? EmailedAt { get; set; }
        public string? EmailedTo { get; set; }
    }

    /// <summary>Same fields and rules as company-invoice payments (method-specific required details, traceable reference).</summary>
    public class RecordTrainingPaymentDto : Dog.Application.Common.Payments.PaymentInput
    {
        public int InvoiceId { get; set; }
    }
}
