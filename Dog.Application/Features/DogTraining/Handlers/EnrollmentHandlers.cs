using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.DogTraining.Commands;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.DogTraining.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.DogTraining.Handlers
{
    internal static class EnrollmentDetailBuilder
    {
        public static async Task<EnrollmentDetailDto?> BuildAsync(IUnitOfWork uow, int id, CancellationToken ct)
        {
            var e = await uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e == null) return null;

            var list = (await TrainingSupport.BuildListAsync(uow, new List<DogTrainingEnrollment> { e }, ct)).First();
            var detail = new EnrollmentDetailDto
            {
                Id = list.Id, EnrollmentNumber = list.EnrollmentNumber, DogId = list.DogId, DogName = list.DogName, DogBreed = list.DogBreed,
                DogPhotoUrl = list.DogPhotoUrl, CustomerId = list.CustomerId, CustomerName = list.CustomerName, ProgrammeId = list.ProgrammeId,
                ProgrammeName = list.ProgrammeName, Status = list.Status, HandlerUserId = list.HandlerUserId, HandlerName = list.HandlerName,
                StartDate = list.StartDate, PlannedEndDate = list.PlannedEndDate, DurationDays = list.DurationDays, ExtensionDays = list.ExtensionDays,
                CurrentWeek = list.CurrentWeek, DayNumber = list.DayNumber, Fee = list.Fee, AdditionalCharges = list.AdditionalCharges, Total = list.Total,
                InvoiceId = list.InvoiceId, InvoiceNumber = list.InvoiceNumber, InvoiceStatus = list.InvoiceStatus, InvoiceBalance = list.InvoiceBalance,
                LatestHealthResult = list.LatestHealthResult, CreatedAt = list.CreatedAt,
                Notes = e.Notes, AdditionalChargesNote = e.AdditionalChargesNote, CancelReason = e.CancelReason, CompanyId = e.CompanyId,
            };

            detail.HealthAssessments = await HealthMapper.LoadForEnrollmentAsync(uow, id, ct);
            detail.FinalAssessments = await FinalAssessmentMapper.LoadForEnrollmentAsync(uow, id, ct);
            detail.Certificate = await CertificateMapper.LoadByEnrollmentAsync(uow, id, ct);

            var assignments = await uow.Repository<DogHandlerAssignment>().TableNoTracking
                .Where(a => a.EnrollmentId == id).OrderByDescending(a => a.AssignedOn).ToListAsync(ct);
            var userIds = assignments.Select(a => a.HandlerUserId).Concat(assignments.Where(a => a.AssignedByUserId != null).Select(a => a.AssignedByUserId!)).Distinct().ToList();
            var users = userIds.Count == 0 ? new Dictionary<string, string>() :
                (await uow.Repository<ApplicationUser>().TableNoTracking.Where(u => userIds.Contains(u.Id)).ToListAsync(ct))
                    .ToDictionary(u => u.Id, u => TrainingSupport.FullName(u));
            detail.Assignments = assignments.Select(a => new HandlerAssignmentDto
            {
                Id = a.Id, HandlerUserId = a.HandlerUserId,
                HandlerName = users.TryGetValue(a.HandlerUserId, out var hn) ? hn : null,
                AssignedOn = a.AssignedOn,
                AssignedByName = a.AssignedByUserId != null && users.TryGetValue(a.AssignedByUserId, out var an) ? an : null,
                Notes = a.Notes, EndedOn = a.EndedOn, IsCurrent = a.IsCurrent
            }).ToList();

            detail.WeeklyPlans = (await uow.Repository<DogTrainingWeeklyPlan>().TableNoTracking.Where(p => p.EnrollmentId == id)
                .OrderBy(p => p.WeekNumber).ToListAsync(ct)).Select(TrainingSupport.MapPlan).ToList();
            detail.DailyLogCount = await uow.Repository<DogTrainingDailyLog>().TableNoTracking.CountAsync(l => l.EnrollmentId == id, ct);
            detail.ReportCount = await uow.Repository<DogTrainingProgressReport>().TableNoTracking.CountAsync(r => r.EnrollmentId == id, ct);

            detail.CanInvoice = e.Status == TrainingStatus.HealthPassed && e.InvoiceId == null;
            detail.CanAssignHandler = e.Status is TrainingStatus.AwaitingHandler or TrainingStatus.Ready or TrainingStatus.InTraining;
            detail.CanActivate = e.Status == TrainingStatus.Ready;
            return detail;
        }
    }

    public class GetEnrollmentsHandler : IRequestHandler<GetEnrollmentsQuery, ApiResponse<List<EnrollmentListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetEnrollmentsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<EnrollmentListDto>>> Handle(GetEnrollmentsQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            await TrainingSupport.SyncPaymentAsync(_uow, null, companyIds, ct);
            await TrainingSupport.SyncProgressAsync(_uow, companyIds, ct);

            var f = request.Filter;
            var q = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);

            if (!string.IsNullOrWhiteSpace(f.Status))
            {
                var statuses = f.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                q = q.Where(e => statuses.Contains(e.Status));
            }
            if (!string.IsNullOrWhiteSpace(f.HandlerUserId)) q = q.Where(e => e.HandlerUserId == f.HandlerUserId);
            if (f.ProgrammeId.HasValue) q = q.Where(e => e.ProgrammeId == f.ProgrammeId.Value);
            if (f.DogId.HasValue) q = q.Where(e => e.DogId == f.DogId.Value);
            if (f.From.HasValue) q = q.Where(e => e.CreatedAt >= f.From.Value.Date);
            if (f.To.HasValue) { var to = f.To.Value.Date.AddDays(1); q = q.Where(e => e.CreatedAt < to); }
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                var dogs = _uow.Repository<DogTrainingDog>().TableNoTracking;
                var customers = _uow.Repository<DogTrainingCustomer>().TableNoTracking;
                q = q.Where(e => e.EnrollmentNumber.Contains(s)
                    || dogs.Any(d => d.Id == e.DogId && d.Name.Contains(s))
                    || customers.Any(c => c.Id == e.CustomerId && c.FullName.Contains(s)));
            }

            var entities = await q.OrderByDescending(e => e.CreatedAt).Take(1000).ToListAsync(ct);
            return ApiResponse<List<EnrollmentListDto>>.Ok(await TrainingSupport.BuildListAsync(_uow, entities, ct));
        }
    }

    public class GetEnrollmentByIdHandler : IRequestHandler<GetEnrollmentByIdQuery, ApiResponse<EnrollmentDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetEnrollmentByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<EnrollmentDetailDto>> Handle(GetEnrollmentByIdQuery request, CancellationToken ct)
        {
            await TrainingSupport.SyncPaymentAsync(_uow, request.Id, null, ct);
            await TrainingSupport.SyncProgressAsync(_uow, null, ct);
            var detail = await EnrollmentDetailBuilder.BuildAsync(_uow, request.Id, ct);
            if (detail == null) return ApiResponse<EnrollmentDetailDto>.Fail("Enrolment not found.");
            if (!request.Actor.CanManageAll && detail.HandlerUserId != request.Actor.UserId)
                return ApiResponse<EnrollmentDetailDto>.Fail("You can only open dogs that are allocated to you.");
            return ApiResponse<EnrollmentDetailDto>.Ok(detail);
        }
    }

    public class CreateEnrollmentHandler : IRequestHandler<CreateEnrollmentCommand, ApiResponse<EnrollmentDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateEnrollmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<EnrollmentDetailDto>> Handle(CreateEnrollmentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);

            var dog = await _uow.Repository<DogTrainingDog>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == d.DogId && companyIds.Contains(x.CompanyId), ct);
            if (dog == null) return ApiResponse<EnrollmentDetailDto>.Fail("Choose a dog from the training dogs list (register it first if it is new).");

            var programme = await _uow.Repository<DogTrainingProgramme>().TableNoTracking
                .FirstOrDefaultAsync(p => p.Id == d.ProgrammeId && companyIds.Contains(p.CompanyId), ct);
            if (programme == null) return ApiResponse<EnrollmentDetailDto>.Fail("Training programme not found.");
            if (!programme.IsActive) return ApiResponse<EnrollmentDetailDto>.Fail("This programme is not active.");
            if (d.AdditionalCharges < 0) return ApiResponse<EnrollmentDetailDto>.Fail("Additional charges cannot be negative.");

            var open = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking
                .AnyAsync(e => e.DogId == dog.Id && e.Status != TrainingStatus.Completed && e.Status != TrainingStatus.Cancelled, ct);
            if (open) return ApiResponse<EnrollmentDetailDto>.Fail("This dog already has an enrolment in progress.");

            var entity = new DogTrainingEnrollment
            {
                CompanyId = dog.CompanyId,
                EnrollmentNumber = await TrainingSupport.NextNumberAsync(_uow, "TRN", false, ct),
                DogId = dog.Id,
                CustomerId = dog.CustomerId,
                ProgrammeId = programme.Id,
                DurationDays = programme.DurationDays,
                Fee = programme.Fee,
                AdditionalCharges = d.AdditionalCharges,
                AdditionalChargesNote = TrainingSupport.Trim(d.AdditionalChargesNote),
                Notes = TrainingSupport.Trim(d.Notes),
                Status = TrainingStatus.AwaitingHealthAssessment,
            };
            await _uow.Repository<DogTrainingEnrollment>().AddAsync(entity, ct);
            return ApiResponse<EnrollmentDetailDto>.Ok((await EnrollmentDetailBuilder.BuildAsync(_uow, entity.Id, ct))!, "Enrolment created. The dog now needs a health assessment.");
        }
    }

    public class CancelEnrollmentHandler : IRequestHandler<CancelEnrollmentCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public CancelEnrollmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(CancelEnrollmentCommand request, CancellationToken ct)
        {
            var e = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.EnrollmentId, ct);
            if (e == null) return ApiResponse<bool>.Fail("Enrolment not found.");
            if (e.Status == TrainingStatus.Completed) return ApiResponse<bool>.Fail("A completed enrolment cannot be cancelled.");
            if (e.Status == TrainingStatus.Cancelled) return ApiResponse<bool>.Fail("Already cancelled.");
            e.Status = TrainingStatus.Cancelled;
            e.CancelReason = TrainingSupport.Trim(request.Reason);
            e.ActualEndDate = DateTime.UtcNow;
            await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);
            return ApiResponse<bool>.Ok(true, "Enrolment cancelled. Any invoice raised for it is left untouched — cancel it from Invoices if needed.");
        }
    }

    /// <summary>Creates the training invoice in the training module's own invoice table (separate from the company's Invoices).</summary>
    public class GenerateTrainingInvoiceHandler : IRequestHandler<GenerateTrainingInvoiceCommand, ApiResponse<EnrollmentDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        public GenerateTrainingInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<EnrollmentDetailDto>> Handle(GenerateTrainingInvoiceCommand request, CancellationToken ct)
        {
            var e = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.Dto.EnrollmentId, ct);
            if (e == null) return ApiResponse<EnrollmentDetailDto>.Fail("Enrolment not found.");
            if (e.InvoiceId != null) return ApiResponse<EnrollmentDetailDto>.Fail("An invoice has already been raised for this enrolment.");
            if (e.Status != TrainingStatus.HealthPassed)
                return ApiResponse<EnrollmentDetailDto>.Fail("An invoice can only be raised after the dog has passed its health assessment.");
            if (e.Fee + e.AdditionalCharges <= 0)
                return ApiResponse<EnrollmentDetailDto>.Fail("The training fee is zero. Set the fee on the programme (or add charges) before invoicing.");

            var dog = await _uow.Repository<DogTrainingDog>().TableNoTracking.FirstOrDefaultAsync(d => d.Id == e.DogId, ct);
            var customer = e.CustomerId == null ? null : await _uow.Repository<DogTrainingCustomer>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == e.CustomerId, ct);
            var programme = await _uow.Repository<DogTrainingProgramme>().TableNoTracking.FirstOrDefaultAsync(p => p.Id == e.ProgrammeId, ct);
            if (customer == null) return ApiResponse<EnrollmentDetailDto>.Fail("The dog's customer could not be found.");

            var items = new List<DogTrainingInvoiceItem>
            {
                new() { CompanyId = e.CompanyId, Description = $"Dog training – {programme?.Name} ({e.DurationDays} days) – {dog?.Name}", Quantity = 1, UnitPrice = e.Fee, Amount = e.Fee }
            };
            if (e.AdditionalCharges > 0)
                items.Add(new() { CompanyId = e.CompanyId, Description = string.IsNullOrWhiteSpace(e.AdditionalChargesNote) ? "Additional training charges" : e.AdditionalChargesNote!, Quantity = 1, UnitPrice = e.AdditionalCharges, Amount = e.AdditionalCharges });
            var total = items.Sum(i => i.Amount);

            var invoice = new DogTrainingInvoice
            {
                CompanyId = e.CompanyId,
                InvoiceNumber = await TrainingSupport.NextInvoiceNumberAsync(_uow, ct),
                CustomerId = customer.Id, CustomerName = customer.FullName, CustomerEmail = customer.Email, CustomerPhone = customer.Phone,
                InvoiceDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(Math.Max(0, request.Dto.DueInDays)),
                TotalAmount = total, PaidAmount = 0, BalanceAmount = total, Status = "SENT",
                Notes = TrainingSupport.Trim(request.Dto.Notes) ?? $"Training enrolment {e.EnrollmentNumber} · Dog: {dog?.Name} · Programme: {programme?.Name} · Duration: {e.DurationDays} days",
                Terms = "Training starts only after full payment has been received.", CreatedByName = request.Actor.Name,
                Items = items,
            };
            await _uow.Repository<DogTrainingInvoice>().AddAsync(invoice, ct);

            e.InvoiceId = invoice.Id;
            e.Status = TrainingStatus.AwaitingPayment;
            await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);

            return ApiResponse<EnrollmentDetailDto>.Ok((await EnrollmentDetailBuilder.BuildAsync(_uow, e.Id, ct))!,
                $"Invoice {invoice.InvoiceNumber} raised. Training can start once it is fully paid.");
        }
    }

    public class AssignHandlerHandler : IRequestHandler<AssignHandlerCommand, ApiResponse<EnrollmentDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        public AssignHandlerHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<EnrollmentDetailDto>> Handle(AssignHandlerCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            await TrainingSupport.SyncPaymentAsync(_uow, d.EnrollmentId, null, ct);

            var e = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.EnrollmentId, ct);
            if (e == null) return ApiResponse<EnrollmentDetailDto>.Fail("Enrolment not found.");
            if (e.Status is not (TrainingStatus.AwaitingHandler or TrainingStatus.Ready or TrainingStatus.InTraining))
                return ApiResponse<EnrollmentDetailDto>.Fail("A handler can only be allocated after payment has been confirmed.");

            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var handler = await _uow.Repository<ApplicationUser>().TableNoTracking.FirstOrDefaultAsync(u => u.Id == d.HandlerUserId, ct);
            if (handler == null || !companyIds.Contains(handler.CompanyId))
                return ApiResponse<EnrollmentDetailDto>.Fail("Handler not found.");

            if (e.HandlerUserId != d.HandlerUserId)
            {
                var profile = await _uow.Repository<DogTrainingHandlerProfile>().TableNoTracking.FirstOrDefaultAsync(p => p.UserId == d.HandlerUserId, ct);
                var max = profile?.MaxDogs ?? 5;
                if (profile != null && !profile.IsAvailable)
                    return ApiResponse<EnrollmentDetailDto>.Fail($"{TrainingSupport.FullName(handler)} is marked as unavailable.");

                var load = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.CountAsync(x =>
                    x.HandlerUserId == d.HandlerUserId && x.Id != e.Id &&
                    (x.Status == TrainingStatus.Ready || x.Status == TrainingStatus.InTraining ||
                     x.Status == TrainingStatus.AwaitingFinalAssessment || x.Status == TrainingStatus.AwaitingApproval), ct);
                if (load >= max)
                    return ApiResponse<EnrollmentDetailDto>.Fail($"{TrainingSupport.FullName(handler)} is at full capacity ({load}/{max} dogs).");
            }

            // End the previous allocation, record the new one.
            var current = await _uow.Repository<DogHandlerAssignment>().Table.AsTracking().Where(a => a.EnrollmentId == e.Id && a.IsCurrent).ToListAsync(ct);
            foreach (var a in current)
            {
                a.IsCurrent = false;
                a.EndedOn = DateTime.UtcNow;
                await _uow.Repository<DogHandlerAssignment>().UpdateAsync(a, ct);
            }

            await _uow.Repository<DogHandlerAssignment>().AddAsync(new DogHandlerAssignment
            {
                CompanyId = e.CompanyId,
                EnrollmentId = e.Id,
                HandlerUserId = d.HandlerUserId,
                AssignedOn = DateTime.UtcNow,
                AssignedByUserId = request.Actor.UserId,
                Notes = TrainingSupport.Trim(d.Notes),
                IsCurrent = true,
            }, ct);

            e.HandlerUserId = d.HandlerUserId;
            if (e.Status == TrainingStatus.AwaitingHandler) e.Status = TrainingStatus.Ready;
            await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);

            return ApiResponse<EnrollmentDetailDto>.Ok((await EnrollmentDetailBuilder.BuildAsync(_uow, e.Id, ct))!,
                $"Allocated to {TrainingSupport.FullName(handler)}.");
        }
    }

    public class ActivateTrainingHandler : IRequestHandler<ActivateTrainingCommand, ApiResponse<EnrollmentDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        public ActivateTrainingHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<EnrollmentDetailDto>> Handle(ActivateTrainingCommand request, CancellationToken ct)
        {
            var id = request.Dto.EnrollmentId;
            await TrainingSupport.SyncPaymentAsync(_uow, id, null, ct);

            var e = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e == null) return ApiResponse<EnrollmentDetailDto>.Fail("Enrolment not found.");
            if (e.Status == TrainingStatus.InTraining) return ApiResponse<EnrollmentDetailDto>.Fail("Training is already active.");

            // Hard gates — payment first, then handler.
            if (!await TrainingSupport.IsInvoicePaidAsync(_uow, e.InvoiceId, ct))
                return ApiResponse<EnrollmentDetailDto>.Fail("Training cannot start until the invoice has been paid in full.");
            if (string.IsNullOrEmpty(e.HandlerUserId))
                return ApiResponse<EnrollmentDetailDto>.Fail("Allocate a handler before activating training.");
            if (e.Status != TrainingStatus.Ready)
                return ApiResponse<EnrollmentDetailDto>.Fail("This enrolment is not ready to start.");

            var start = (request.Dto.StartDate ?? DateTime.Today).Date;
            e.StartDate = start;
            e.PlannedEndDate = start.AddDays(TrainingSupport.TotalDays(e) - 1);
            e.ActivatedByUserId = request.Actor.UserId;
            e.ActivatedOn = DateTime.UtcNow;
            e.Status = TrainingStatus.InTraining;
            await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);

            await CreatePlansAsync(e, ct);
            return ApiResponse<EnrollmentDetailDto>.Ok((await EnrollmentDetailBuilder.BuildAsync(_uow, e.Id, ct))!,
                $"Training started. It runs for {e.DurationDays} days, to {e.PlannedEndDate:dd MMM yyyy}.");
        }

        /// <summary>This dog's own copy of the programme: one weekly plan per week, with targets and daily schedule.</summary>
        private async Task CreatePlansAsync(DogTrainingEnrollment e, CancellationToken ct)
        {
            var existing = await _uow.Repository<DogTrainingWeeklyPlan>().TableNoTracking.AnyAsync(p => p.EnrollmentId == e.Id, ct);
            if (existing) return;

            var weeks = await _uow.Repository<DogTrainingProgrammeWeek>().TableNoTracking.Where(w => w.ProgrammeId == e.ProgrammeId).ToListAsync(ct);
            var acts = await _uow.Repository<DogTrainingProgrammeActivity>().TableNoTracking.Where(a => a.ProgrammeId == e.ProgrammeId).ToListAsync(ct);
            var totalWeeks = TrainingSupport.TotalWeeks(e);
            var plans = new List<DogTrainingWeeklyPlan>();

            for (var w = 1; w <= totalWeeks; w++)
            {
                var cfg = weeks.FirstOrDefault(x => x.WeekNumber == w);
                var slots = acts.Where(a => a.WeekNumber == 0 || a.WeekNumber == w)
                    .OrderBy(a => a.SortOrder).ThenBy(a => a.TimeOfDay)
                    .Select(a => new ScheduleSlotDto { Time = a.TimeOfDay, Title = a.Title, Purpose = a.Purpose }).ToList();

                var start = e.StartDate!.Value.AddDays((w - 1) * 7);
                var end = start.AddDays(6);
                if (e.PlannedEndDate.HasValue && end > e.PlannedEndDate.Value) end = e.PlannedEndDate.Value;

                plans.Add(new DogTrainingWeeklyPlan
                {
                    CompanyId = e.CompanyId, EnrollmentId = e.Id, WeekNumber = w,
                    Title = cfg?.Title ?? $"Week {w}", Focus = cfg?.Focus, Targets = cfg?.Targets,
                    ScheduleJson = TrainingSupport.ToJson(slots), StartDate = start, EndDate = end,
                });
            }
            await _uow.Repository<DogTrainingWeeklyPlan>().AddRangeAsync(plans, ct);
        }
    }
}
