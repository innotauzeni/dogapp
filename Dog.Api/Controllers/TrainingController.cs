using Dog.Application.Authorization;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Documents.DTOs;
using Dog.Application.Features.DogTraining.Commands;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.DogTraining.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dog.Api.Controllers
{
    /// <summary>Dog Training module: health assessment → enrolment/invoice → handler → daily logbook →
    /// progress reports → final assessment → certificate. Permissions are the training.* set.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;
        private readonly IFileStorageService _storage;
        private readonly Dog.Api.Services.BillingDocuments _docs;
        public TrainingController(IMediator mediator, IUnitOfWork uow, IFileStorageService storage, Dog.Api.Services.BillingDocuments docs) { _mediator = mediator; _uow = uow; _storage = storage; _docs = docs; }

        // A user holding any of these manages the whole training operation; everyone else (handlers) only
        // ever sees dogs allocated to them, unless they hold training.manage.all.
        private static readonly string[] ManagerPermissions =
        {
            "training.manage.all", "training.handler.assign", "training.enrolment.create", "training.health.approve",
            "training.assessment.approve", "training.programme.manage"
        };

        private bool Has(string permission) => User.Claims.Any(c => c.Type == "permission" && c.Value == permission);
        private bool HasAny(params string[] permissions) => permissions.Any(Has);

        private static ApiResponse<bool> NoAccess() => ApiResponse<bool>.Fail("You do not have permission to do this.");

        /// <summary>The caller. A head-office user may act on one of their branches, never on an unrelated company.</summary>
        private async Task<TrainingActor> ActorAsync(int? requestedCompanyId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var name = User.FindFirstValue(ClaimTypes.Name) ?? "";
            int.TryParse(User.FindFirstValue("companyId"), out var own);

            var company = own;
            if (requestedCompanyId.HasValue && requestedCompanyId.Value > 0 && requestedCompanyId.Value != own)
            {
                var allowed = await CompanyScopeResolver.ResolveAsync(_uow, own, ct);
                if (allowed.Contains(requestedCompanyId.Value)) company = requestedCompanyId.Value;
            }
            return new TrainingActor(userId, name, company, ManagerPermissions.Any(Has), Has("training.payment.verify"));
        }

        // ── Training customers & dogs (separate from the company's own customers and dogs) ──
        [HttpGet("customers")]
        [Authorize]
        public async Task<IActionResult> Customers(int? companyId, string? search, CancellationToken ct)
        {
            if (!HasAny("training.customer.view", "training.enrolment.create")) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetTrainingCustomersQuery((await ActorAsync(companyId, ct)).CompanyId, search), ct));
        }

        [HttpPost("customers")]
        [HasPermission("training.customer.manage")]
        public async Task<IActionResult> SaveCustomer([FromBody] SaveTrainingCustomerDto dto, CancellationToken ct)
        {
            dto.CompanyId = (await ActorAsync(dto.CompanyId, ct)).CompanyId;
            return Ok(await _mediator.Send(new SaveTrainingCustomerCommand(dto), ct));
        }

        [HttpDelete("customers/{id:int}")]
        [HasPermission("training.customer.manage")]
        public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteTrainingCustomerCommand(id), ct));

        [HttpGet("dogs")]
        [Authorize]
        public async Task<IActionResult> Dogs(int? companyId, int? customerId, string? search, CancellationToken ct)
        {
            if (!HasAny("training.dog.view", "training.enrolment.create")) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetTrainingDogsQuery((await ActorAsync(companyId, ct)).CompanyId, customerId, search), ct));
        }

        [HttpPost("dogs")]
        [HasPermission("training.dog.manage")]
        public async Task<IActionResult> SaveDog([FromBody] SaveTrainingDogDto dto, CancellationToken ct)
        {
            dto.CompanyId = (await ActorAsync(dto.CompanyId, ct)).CompanyId;
            return Ok(await _mediator.Send(new SaveTrainingDogCommand(dto), ct));
        }

        [HttpDelete("dogs/{id:int}")]
        [HasPermission("training.dog.manage")]
        public async Task<IActionResult> DeleteDog(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteTrainingDogCommand(id), ct));

        // ── Training invoices & payments (separate from the company's Invoices module) ──
        [HttpGet("invoices")]
        [HasPermission("training.invoice.view")]
        public async Task<IActionResult> Invoices(int? companyId, string? status, string? search, CancellationToken ct)
            => Ok(await _mediator.Send(new GetTrainingInvoicesQuery(await ActorAsync(companyId, ct), status, search), ct));

        [HttpGet("invoices/{id:int}")]
        [HasPermission("training.invoice.view")]
        public async Task<IActionResult> Invoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetTrainingInvoiceByIdQuery(id), ct));

        [HttpPost("invoices/{id:int}/payments")]
        [HasPermission("training.payment.record")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordTrainingPaymentDto dto, CancellationToken ct)
        {
            dto.InvoiceId = id;
            var actor = await ActorAsync(null, ct);
            if (!actor.CanVerifyPayments) dto.VerifiedOnStatement = false;
            return Ok(await _mediator.Send(new RecordTrainingPaymentCommand(actor, dto), ct));
        }

        [HttpGet("payment-methods")]
        [Authorize]
        public async Task<IActionResult> PaymentMethods(CancellationToken ct)
            => Ok(await _mediator.Send(new Dog.Application.Features.Invoices.Billing.GetPaymentMethodsQuery(), ct));

        [HttpPost("payments/{paymentId:int}/confirm")]
        [HasPermission("training.payment.verify")]
        public async Task<IActionResult> ConfirmPayment(int paymentId, [FromBody] Dog.Application.Features.Invoices.Billing.ReviewPaymentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new ConfirmTrainingPaymentCommand(await ActorAsync(null, ct), paymentId, dto?.StatementReference), ct));

        [HttpPost("payments/{paymentId:int}/reject")]
        [HasPermission("training.payment.verify")]
        public async Task<IActionResult> RejectPayment(int paymentId, [FromBody] Dog.Application.Features.Invoices.Billing.ReviewPaymentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new RejectTrainingPaymentCommand(await ActorAsync(null, ct), paymentId, dto?.Reason), ct));

        [HttpGet("invoices/{id:int}/pdf")]
        [HasPermission("training.invoice.view")]
        public async Task<IActionResult> InvoicePdf(int id, CancellationToken ct)
        {
            var built = await _docs.TrainingInvoicePdfAsync(id, ct);
            if (built == null) return NotFound("Invoice not found.");
            return File(built.Value.Pdf, "application/pdf", $"Training_Invoice_{built.Value.Invoice.InvoiceNumber}.pdf");
        }

        [HttpPost("invoices/{id:int}/email")]
        [HasPermission("training.invoice.email")]
        public async Task<IActionResult> EmailInvoice(int id, [FromBody] Dog.Api.Services.EmailInvoiceRequest req, CancellationToken ct)
        {
            var (ok, msg) = await _docs.EmailTrainingInvoiceAsync(id, req ?? new Dog.Api.Services.EmailInvoiceRequest(), User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindFirstValue(ClaimTypes.Name), ct);
            return Ok(new { success = ok, message = msg });
        }

        [HttpPost("invoices/{id:int}/cancel")]
        [HasPermission("training.invoice.generate")]
        public async Task<IActionResult> CancelInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new CancelTrainingInvoiceCommand(id), ct));

        // ── Dashboard / reports ──────────────────────────────────────────────
        [HttpGet("dashboard")]
        [HasPermission("training.dashboard.view")]
        public async Task<IActionResult> Dashboard(int? companyId, string? handlerUserId, int? programmeId, DateTime? from, DateTime? to, CancellationToken ct)
            => Ok(await _mediator.Send(new GetTrainingDashboardQuery(await ActorAsync(companyId, ct), handlerUserId, programmeId, from, to), ct));

        [HttpGet("reports/summary")]
        [HasPermission("training.reports.view")]
        public async Task<IActionResult> ReportSummary(int? companyId, DateTime? from, DateTime? to, CancellationToken ct)
            => Ok(await _mediator.Send(new GetTrainingReportQuery(await ActorAsync(companyId, ct), from, to), ct));

        // ── Programmes ───────────────────────────────────────────────────────
        [HttpGet("programmes")]
        [HasPermission("training.programme.view")]
        public async Task<IActionResult> Programmes(int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetProgrammesQuery((await ActorAsync(companyId, ct)).CompanyId), ct));

        /// <summary>Active programmes for the enrolment form (name, duration and fee only).</summary>
        [HttpGet("programmes/active")]
        [Authorize]
        public async Task<IActionResult> ActiveProgrammes(int? companyId, CancellationToken ct)
        {
            if (!HasAny("training.enrolment.create", "training.enrolment.view", "training.programme.view")) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetProgrammesQuery((await ActorAsync(companyId, ct)).CompanyId, true), ct));
        }

        [HttpPost("programmes")]
        [HasPermission("training.programme.manage")]
        public async Task<IActionResult> SaveProgramme([FromBody] SaveProgrammeDto dto, CancellationToken ct)
        {
            var actor = await ActorAsync(dto.CompanyId, ct);
            dto.CompanyId = actor.CompanyId;
            return Ok(await _mediator.Send(new SaveProgrammeCommand(dto), ct));
        }

        [HttpDelete("programmes/{id:int}")]
        [HasPermission("training.programme.manage")]
        public async Task<IActionResult> DeleteProgramme(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteProgrammeCommand(id), ct));

        [HttpPost("programmes/seed-default")]
        [HasPermission("training.programme.manage")]
        public async Task<IActionResult> SeedDefaultProgramme(int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new SeedDefaultProgrammeCommand((await ActorAsync(companyId, ct)).CompanyId), ct));

        // ── Enrolments ───────────────────────────────────────────────────────
        [HttpGet("enrollments")]
        [HasPermission("training.enrolment.view")]
        public async Task<IActionResult> Enrollments(int? companyId, string? status, string? handlerUserId, int? programmeId,
            DateTime? from, DateTime? to, string? search, int? dogId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetEnrollmentsQuery(await ActorAsync(companyId, ct),
                new EnrollmentFilter(status, handlerUserId, programmeId, from, to, search, dogId)), ct));

        /// <summary>The handler's own dogs (in training / ready) — mobile home screen.</summary>
        [HttpGet("my-dogs")]
        [HasPermission("training.log.view")]
        public async Task<IActionResult> MyDogs(CancellationToken ct)
            => Ok(await _mediator.Send(new GetMyTrainingDogsQuery(await ActorAsync(null, ct)), ct));

        [HttpGet("enrollments/{id:int}")]
        [Authorize]
        public async Task<IActionResult> Enrollment(int id, CancellationToken ct)
        {
            if (!HasAny("training.enrolment.view", "training.log.view", "training.log.record", "training.progress.view", "training.progress.submit",
                    "training.health.view", "training.assessment.view", "training.handler.view")) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetEnrollmentByIdQuery(await ActorAsync(null, ct), id), ct));
        }

        [HttpPost("enrollments")]
        [HasPermission("training.enrolment.create")]
        public async Task<IActionResult> CreateEnrollment([FromBody] CreateEnrollmentDto dto, CancellationToken ct)
        {
            var actor = await ActorAsync(dto.CompanyId, ct);
            dto.CompanyId = actor.CompanyId;
            return Ok(await _mediator.Send(new CreateEnrollmentCommand(actor, dto), ct));
        }

        [HttpPost("enrollments/{id:int}/cancel")]
        [HasPermission("training.enrolment.create")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelRequest body, CancellationToken ct)
            => Ok(await _mediator.Send(new CancelEnrollmentCommand(id, body?.Reason), ct));

        [HttpPost("enrollments/{id:int}/invoice")]
        [HasPermission("training.invoice.generate")]
        public async Task<IActionResult> GenerateInvoice(int id, [FromBody] GenerateInvoiceDto dto, CancellationToken ct)
        {
            dto.EnrollmentId = id;
            return Ok(await _mediator.Send(new GenerateTrainingInvoiceCommand(await ActorAsync(null, ct), dto), ct));
        }

        [HttpPost("enrollments/{id:int}/assign-handler")]
        [HasPermission("training.handler.assign")]
        public async Task<IActionResult> AssignHandler(int id, [FromBody] AssignHandlerDto dto, CancellationToken ct)
        {
            dto.EnrollmentId = id;
            return Ok(await _mediator.Send(new AssignHandlerCommand(await ActorAsync(null, ct), dto), ct));
        }

        [HttpPost("enrollments/{id:int}/activate")]
        [HasPermission("training.handler.assign")]
        public async Task<IActionResult> Activate(int id, [FromBody] ActivateTrainingDto dto, CancellationToken ct)
        {
            dto.EnrollmentId = id;
            return Ok(await _mediator.Send(new ActivateTrainingCommand(await ActorAsync(null, ct), dto), ct));
        }

        [HttpPost("enrollments/{id:int}/extend")]
        [HasPermission("training.assessment.approve")]
        public async Task<IActionResult> Extend(int id, [FromBody] ExtendRequest body, CancellationToken ct)
            => Ok(await _mediator.Send(new ExtendTrainingCommand(await ActorAsync(null, ct), id, body?.Days ?? 0, body?.AdditionalActivities), ct));

        // ── Health assessments ───────────────────────────────────────────────
        [HttpGet("health-form-template")]
        [Authorize]
        public IActionResult HealthFormTemplate() => Ok(ApiResponse<object>.Ok(new
        {
            exam = new[] { "Eyes", "Ears", "Teeth", "Mucous membranes", "Heart", "Body score", "Lungs", "Musculoskeletal system", "Lymph nodes", "Skin", "Other", "General fitness", "Behaviour & temperament" },
            treatments = new[] { "External parasites", "Internal parasites", "Deworming", "Antibiotic injections", "Supplements", "Food type / brand", "Injured", "Mating", "Heat", "Feeding time", "Dipping", "Kennel clean" }
        }));

        [HttpGet("health-assessments")]
        [HasPermission("training.health.view")]
        public async Task<IActionResult> HealthAssessments(int? companyId, string? result, int? enrollmentId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetHealthAssessmentsQuery(await ActorAsync(companyId, ct), result, enrollmentId), ct));

        [HttpGet("health-assessments/{id:int}")]
        [HasPermission("training.health.view")]
        public async Task<IActionResult> HealthAssessment(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetHealthAssessmentByIdQuery(await ActorAsync(null, ct), id), ct));

        [HttpPost("health-assessments")]
        [HasPermission("training.health.create")]
        public async Task<IActionResult> SaveHealthAssessment([FromBody] SaveHealthAssessmentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new SaveHealthAssessmentCommand(await ActorAsync(null, ct), dto), ct));

        [HttpPost("health-assessments/{id:int}/decide")]
        [HasPermission("training.health.approve")]
        public async Task<IActionResult> DecideHealth(int id, [FromBody] DecideHealthDto dto, CancellationToken ct)
        {
            dto.AssessmentId = id;
            return Ok(await _mediator.Send(new DecideHealthAssessmentCommand(await ActorAsync(null, ct), dto), ct));
        }

        // ── Handlers ─────────────────────────────────────────────────────────
        [HttpGet("handlers")]
        [HasPermission("training.handler.view")]
        public async Task<IActionResult> Handlers(int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetHandlerCapacityQuery((await ActorAsync(companyId, ct)).CompanyId), ct));

        [HttpPost("handlers/profile")]
        [HasPermission("training.handler.assign")]
        public async Task<IActionResult> SaveHandlerProfile([FromBody] SaveHandlerProfileDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new SaveHandlerProfileCommand(dto), ct));

        // ── Weekly plans ─────────────────────────────────────────────────────
        [HttpGet("enrollments/{id:int}/plans")]
        [Authorize]
        public async Task<IActionResult> Plans(int id, CancellationToken ct)
        {
            if (!HasAny("training.enrolment.view", "training.log.view", "training.log.record", "training.progress.submit")) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetWeeklyPlansQuery(await ActorAsync(null, ct), id), ct));
        }

        [HttpPut("plans")]
        [HasPermission("training.programme.manage")]
        public async Task<IActionResult> SavePlan([FromBody] SaveWeeklyPlanDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new SaveWeeklyPlanCommand(dto), ct));

        // ── Daily logbook ────────────────────────────────────────────────────
        [HttpGet("logs")]
        [HasPermission("training.log.view")]
        public async Task<IActionResult> Logs(int? companyId, int? enrollmentId, DateTime? date, DateTime? from, DateTime? to, CancellationToken ct)
            => Ok(await _mediator.Send(new GetDailyLogsQuery(await ActorAsync(companyId, ct), enrollmentId, date, from, to), ct));

        [HttpGet("logs/day")]
        [Authorize]
        public async Task<IActionResult> LogForDay(int enrollmentId, DateTime date, CancellationToken ct)
        {
            if (!HasAny("training.log.view", "training.log.record")) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetDailyLogForDayQuery(await ActorAsync(null, ct), enrollmentId, date), ct));
        }

        [HttpPost("logs")]
        [HasPermission("training.log.record")]
        public async Task<IActionResult> SaveLog([FromBody] SaveDailyLogDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new SaveDailyLogCommand(await ActorAsync(null, ct), dto), ct));

        // ── Progress reports ─────────────────────────────────────────────────
        [HttpGet("progress-reports")]
        [HasPermission("training.progress.view")]
        public async Task<IActionResult> ProgressReports(int? companyId, string? status, int? enrollmentId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetProgressReportsQuery(await ActorAsync(companyId, ct), status, enrollmentId), ct));

        [HttpGet("progress-reports/{id:int}")]
        [Authorize]
        public async Task<IActionResult> ProgressReport(int id, CancellationToken ct)
        {
            if (!HasAny("training.progress.view", "training.progress.submit", "training.progress.review")) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetProgressReportByIdQuery(await ActorAsync(null, ct), id), ct));
        }

        [HttpGet("progress-reports/draft")]
        [HasPermission("training.progress.submit")]
        public async Task<IActionResult> ProgressReportDraft(int enrollmentId, int week, CancellationToken ct)
            => Ok(await _mediator.Send(new GetProgressReportDraftQuery(await ActorAsync(null, ct), enrollmentId, week), ct));

        [HttpPost("progress-reports")]
        [HasPermission("training.progress.submit")]
        public async Task<IActionResult> SaveProgressReport([FromBody] SaveProgressReportDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new SaveProgressReportCommand(await ActorAsync(null, ct), dto), ct));

        [HttpPost("progress-reports/{id:int}/review")]
        [HasPermission("training.progress.review")]
        public async Task<IActionResult> ReviewProgressReport(int id, [FromBody] ReviewReportDto dto, CancellationToken ct)
        {
            dto.ReportId = id;
            return Ok(await _mediator.Send(new ReviewProgressReportCommand(await ActorAsync(null, ct), dto), ct));
        }

        // ── Final assessments ────────────────────────────────────────────────
        [HttpGet("assessment-template")]
        [Authorize]
        public IActionResult AssessmentTemplate() => Ok(ApiResponse<string[]>.Ok(
            new[] { "Obedience", "Sit", "Stay", "Recall", "Heel", "Leash control", "Socialisation", "Behaviour", "Responsiveness", "Specialised skills" }));

        [HttpGet("final-assessments")]
        [HasPermission("training.assessment.view")]
        public async Task<IActionResult> FinalAssessments(int? companyId, string? result, int? enrollmentId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetFinalAssessmentsQuery(await ActorAsync(companyId, ct), result, enrollmentId), ct));

        [HttpGet("final-assessments/{id:int}")]
        [HasPermission("training.assessment.view")]
        public async Task<IActionResult> FinalAssessment(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetFinalAssessmentByIdQuery(await ActorAsync(null, ct), id), ct));

        [HttpPost("final-assessments")]
        [HasPermission("training.assessment.conduct")]
        public async Task<IActionResult> SaveFinalAssessment([FromBody] SaveFinalAssessmentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new SaveFinalAssessmentCommand(await ActorAsync(null, ct), dto), ct));

        [HttpPost("final-assessments/{id:int}/approve")]
        [HasPermission("training.assessment.approve")]
        public async Task<IActionResult> ApproveFinalAssessment(int id, [FromBody] ApproveAssessmentDto dto, CancellationToken ct)
        {
            dto.AssessmentId = id;
            return Ok(await _mediator.Send(new ApproveFinalAssessmentCommand(await ActorAsync(null, ct), dto), ct));
        }

        // ── Certificates ─────────────────────────────────────────────────────
        [HttpGet("certificates")]
        [HasPermission("training.certificate.view")]
        public async Task<IActionResult> Certificates(int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetTrainingCertificatesQuery(await ActorAsync(companyId, ct)), ct));

        [HttpGet("certificates/{id:int}")]
        [HasPermission("training.certificate.view")]
        public async Task<IActionResult> Certificate(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetTrainingCertificateByIdQuery(id), ct));

        [HttpPost("certificates/issue/{assessmentId:int}")]
        [HasPermission("training.certificate.issue")]
        public async Task<IActionResult> IssueCertificate(int assessmentId, CancellationToken ct)
            => Ok(await _mediator.Send(new IssueTrainingCertificateCommand(await ActorAsync(null, ct), assessmentId), ct));

        [HttpPost("certificates/{id:int}/revoke")]
        [HasPermission("training.certificate.issue")]
        public async Task<IActionResult> RevokeCertificate(int id, [FromBody] CancelRequest body, CancellationToken ct)
            => Ok(await _mediator.Send(new RevokeTrainingCertificateCommand(id, body?.Reason ?? ""), ct));

        /// <summary>Public: used by the QR code / verification page. No login.</summary>
        [HttpGet("verify/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string token, CancellationToken ct)
            => Ok(await _mediator.Send(new VerifyTrainingCertificateQuery(token), ct));

        // ── Attachments (photos, videos, vet documents) ──────────────────────
        // Permission by record type; the handler also checks the caller may open that dog.
        private bool CanViewAttachments(string type) => type switch
        {
            "TrainingHealth" => HasAny("training.health.view", "training.health.create"),
            "TrainingLog" => HasAny("training.log.view", "training.log.record"),
            "TrainingReport" => HasAny("training.progress.view", "training.progress.submit", "training.progress.review"),
            _ => false
        };

        private bool CanChangeAttachments(string type) => type switch
        {
            "TrainingHealth" => Has("training.health.create"),
            "TrainingLog" => Has("training.log.record"),
            "TrainingReport" => Has("training.progress.submit"),
            _ => false
        };

        [HttpGet("attachments")]
        [Authorize]
        public async Task<IActionResult> Attachments(string entityType, int entityId, CancellationToken ct)
        {
            if (!CanViewAttachments(entityType)) return Ok(NoAccess());
            return Ok(await _mediator.Send(new GetTrainingAttachmentsQuery(await ActorAsync(null, ct), entityType, entityId), ct));
        }

        [HttpPost("attachments")]
        [Authorize]
        public async Task<IActionResult> AddAttachment([FromBody] AttachmentRequest body, CancellationToken ct)
        {
            if (!CanChangeAttachments(body.EntityType)) return Ok(NoAccess());
            return Ok(await _mediator.Send(new AddTrainingAttachmentCommand(await ActorAsync(null, ct), body.EntityType, body.EntityId,
                body.FileName, body.FileUrl, body.ContentType, body.Notes), ct));
        }

        private static readonly Dictionary<string, long> UploadLimits = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = 12L << 20, [".jpeg"] = 12L << 20, [".png"] = 12L << 20, [".webp"] = 12L << 20, [".gif"] = 12L << 20,
            [".mp4"] = 80L << 20, [".mov"] = 80L << 20, [".webm"] = 80L << 20, [".m4v"] = 80L << 20, [".pdf"] = 12L << 20,
        };

        /// <summary>Mobile upload: stores the file and attaches it in one call (handlers don't need pet-photo permissions).</summary>
        [HttpPost("attachments/upload")]
        [Authorize]
        [RequestSizeLimit(90L << 20)]
        public async Task<IActionResult> UploadAttachment(IFormFile file, [FromForm] string entityType, [FromForm] int entityId, CancellationToken ct)
        {
            if (!CanChangeAttachments(entityType)) return Ok(NoAccess());
            if (!_storage.IsEnabled) return Ok(ApiResponse<bool>.Fail("File storage is not configured on the server."));
            if (file == null || file.Length == 0) return Ok(ApiResponse<bool>.Fail("No file uploaded."));
            var ext = Path.GetExtension(file.FileName);
            if (!UploadLimits.TryGetValue(ext ?? "", out var max)) return Ok(ApiResponse<bool>.Fail("Only photos, videos (MP4/MOV/WebM) and PDF files are allowed."));
            if (file.Length > max) return Ok(ApiResponse<bool>.Fail($"That file is too large (limit {max >> 20} MB)."));

            // Check the caller may open this record BEFORE storing anything.
            var probe = await _mediator.Send(new GetTrainingAttachmentsQuery(await ActorAsync(null, ct), entityType, entityId), ct);
            if (!probe.Success) return Ok(ApiResponse<bool>.Fail(probe.Message ?? "You do not have access to this record."));

            await using var stream = file.OpenReadStream();
            var url = await _storage.UploadAsync(stream, file.FileName, file.ContentType ?? "application/octet-stream", "training", ct);
            return Ok(await _mediator.Send(new AddTrainingAttachmentCommand(await ActorAsync(null, ct), entityType, entityId,
                file.FileName, url, file.ContentType, null), ct));
        }

        [HttpDelete("attachments/{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteAttachment(int id, string entityType, int entityId, CancellationToken ct)
        {
            if (!CanChangeAttachments(entityType)) return Ok(NoAccess());
            return Ok(await _mediator.Send(new DeleteTrainingAttachmentCommand(await ActorAsync(null, ct), entityType, entityId, id), ct));
        }

        public class CancelRequest { public string? Reason { get; set; } }
        public class ExtendRequest { public int Days { get; set; } public string? AdditionalActivities { get; set; } }
        public class AttachmentRequest
        {
            public string EntityType { get; set; } = string.Empty;
            public int EntityId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string FileUrl { get; set; } = string.Empty;
            public string? ContentType { get; set; }
            public string? Notes { get; set; }
        }
    }
}
