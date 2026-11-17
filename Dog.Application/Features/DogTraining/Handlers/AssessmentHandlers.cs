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
    internal static class FinalAssessmentMapper
    {
        public static async Task<List<FinalAssessmentDto>> MapAsync(IUnitOfWork uow, List<DogTrainingFinalAssessment> list, CancellationToken ct)
        {
            if (list.Count == 0) return new();
            var enrIds = list.Select(a => a.EnrollmentId).Distinct().ToList();
            var ids = list.Select(a => a.Id).ToList();
            var enrs = await uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => enrIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
            var dogIds = enrs.Values.Select(e => e.DogId).Distinct().ToList();
            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);
            var custIds = enrs.Values.Where(e => e.CustomerId != null).Select(e => e.CustomerId!.Value).Distinct().ToList();
            var customers = custIds.Count == 0 ? new Dictionary<int, string>() :
                (await uow.Repository<DogTrainingCustomer>().TableNoTracking.Where(c => custIds.Contains(c.Id)).ToListAsync(ct)).ToDictionary(c => c.Id, c => TrainingSupport.CustomerName(c));
            var userIds = list.Where(a => a.ApprovedByUserId != null).Select(a => a.ApprovedByUserId!).Distinct().ToList();
            var users = userIds.Count == 0 ? new Dictionary<string, string>() :
                (await uow.Repository<ApplicationUser>().TableNoTracking.Where(u => userIds.Contains(u.Id)).ToListAsync(ct)).ToDictionary(u => u.Id, u => TrainingSupport.FullName(u));
            var certs = await uow.Repository<DogTrainingCertificate>().TableNoTracking
                .Where(c => ids.Contains(c.FinalAssessmentId) && !c.IsRevoked).ToDictionaryAsync(c => c.FinalAssessmentId, c => c.CertificateNumber, ct);

            return list.Select(a =>
            {
                enrs.TryGetValue(a.EnrollmentId, out var e);
                return new FinalAssessmentDto
                {
                    Id = a.Id, EnrollmentId = a.EnrollmentId, EnrollmentNumber = e?.EnrollmentNumber,
                    DogName = e != null && dogs.TryGetValue(e.DogId, out var dn) ? dn : "",
                    CustomerName = e?.CustomerId != null && customers.TryGetValue(e.CustomerId.Value, out var cn) ? cn : null,
                    AttemptNumber = a.AttemptNumber, AssessmentDate = a.AssessmentDate, AssessorName = a.AssessorName,
                    Items = TrainingSupport.ParseList<AssessmentAreaDto>(a.ItemsJson), OverallScore = a.OverallScore, Result = a.Result,
                    Comments = a.Comments, ExtensionDays = a.ExtensionDays, AdditionalActivities = a.AdditionalActivities,
                    ApprovalStatus = a.ApprovalStatus,
                    ApprovedByName = a.ApprovedByUserId != null && users.TryGetValue(a.ApprovedByUserId, out var an) ? an : null,
                    ApprovedOn = a.ApprovedOn, ApprovalComments = a.ApprovalComments,
                    CertificateNumber = certs.TryGetValue(a.Id, out var cert) ? cert : null,
                };
            }).ToList();
        }

        public static async Task<List<FinalAssessmentDto>> LoadForEnrollmentAsync(IUnitOfWork uow, int enrollmentId, CancellationToken ct)
        {
            var list = await uow.Repository<DogTrainingFinalAssessment>().TableNoTracking.Where(a => a.EnrollmentId == enrollmentId)
                .OrderByDescending(a => a.AttemptNumber).ToListAsync(ct);
            return await MapAsync(uow, list, ct);
        }
    }

    internal static class CertificateMapper
    {
        public static async Task<List<TrainingCertificateDto>> MapAsync(IUnitOfWork uow, List<DogTrainingCertificate> list, CancellationToken ct)
        {
            if (list.Count == 0) return new();
            var enrIds = list.Select(c => c.EnrollmentId).Distinct().ToList();
            var assessIds = list.Select(c => c.FinalAssessmentId).Distinct().ToList();
            var enrs = await uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => enrIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
            var dogIds = enrs.Values.Select(e => e.DogId).Distinct().ToList();
            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, ct);
            var custIds = enrs.Values.Where(e => e.CustomerId != null).Select(e => e.CustomerId!.Value).Distinct().ToList();
            var customers = custIds.Count == 0 ? new Dictionary<int, string>() :
                (await uow.Repository<DogTrainingCustomer>().TableNoTracking.Where(c => custIds.Contains(c.Id)).ToListAsync(ct)).ToDictionary(c => c.Id, c => TrainingSupport.CustomerName(c));
            var progIds = enrs.Values.Select(e => e.ProgrammeId).Distinct().ToList();
            var progs = await uow.Repository<DogTrainingProgramme>().TableNoTracking.Where(p => progIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
            var handlerIds = enrs.Values.Where(e => e.HandlerUserId != null).Select(e => e.HandlerUserId!).Distinct().ToList();
            var handlers = handlerIds.Count == 0 ? new Dictionary<string, string>() :
                (await uow.Repository<ApplicationUser>().TableNoTracking.Where(u => handlerIds.Contains(u.Id)).ToListAsync(ct)).ToDictionary(u => u.Id, u => TrainingSupport.FullName(u));
            var assessments = await uow.Repository<DogTrainingFinalAssessment>().TableNoTracking.Where(a => assessIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

            return list.Select(c =>
            {
                enrs.TryGetValue(c.EnrollmentId, out var e);
                assessments.TryGetValue(c.FinalAssessmentId, out var fa);
                DogTrainingDog? dog = e != null && dogs.TryGetValue(e.DogId, out var dg) ? dg : null;
                return new TrainingCertificateDto
                {
                    Id = c.Id, EnrollmentId = c.EnrollmentId, CertificateNumber = c.CertificateNumber, VerificationToken = c.VerificationToken,
                    IssuedOn = c.IssuedOn, DogName = dog?.Name ?? "", Breed = dog?.Breed,
                    OwnerName = e?.CustomerId != null && customers.TryGetValue(e.CustomerId.Value, out var cn) ? cn : null,
                    ProgrammeName = e != null && progs.TryGetValue(e.ProgrammeId, out var pn) ? pn : "",
                    DurationDays = e != null ? TrainingSupport.TotalDays(e) : 0, StartDate = e?.StartDate,
                    CompletionDate = e?.ActualEndDate ?? fa?.ApprovedOn ?? fa?.AssessmentDate,
                    HandlerName = e?.HandlerUserId != null && handlers.TryGetValue(e.HandlerUserId, out var hn) ? hn : null,
                    OverallScore = fa?.OverallScore, IsRevoked = c.IsRevoked, RevokedReason = c.RevokedReason, CompanyId = c.CompanyId,
                };
            }).ToList();
        }

        public static async Task<TrainingCertificateDto?> LoadByEnrollmentAsync(IUnitOfWork uow, int enrollmentId, CancellationToken ct)
        {
            var c = await uow.Repository<DogTrainingCertificate>().TableNoTracking.Where(x => x.EnrollmentId == enrollmentId && !x.IsRevoked)
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
            return c == null ? null : (await MapAsync(uow, new List<DogTrainingCertificate> { c }, ct)).First();
        }
    }

    internal static class TrainingExtension
    {
        /// <summary>Adds days to the training (counted from today if the original end has passed), extends the dog's plan and puts it back in training.</summary>
        public static async Task ApplyAsync(IUnitOfWork uow, DogTrainingEnrollment e, int days, string? additionalActivities, CancellationToken ct)
        {
            var oldWeeks = TrainingSupport.TotalWeeks(e);
            var baseEnd = e.PlannedEndDate.HasValue && e.PlannedEndDate.Value.Date > DateTime.Today ? e.PlannedEndDate.Value.Date : DateTime.Today;
            var newEnd = baseEnd.AddDays(days);
            var ext = (newEnd - e.StartDate!.Value.Date).Days + 1 - e.DurationDays;
            // Normally the original end has passed, so this is "days since the planned end + the extra days". If the
            // dates are ever inconsistent, never lose the extension: add the days to what was already granted.
            e.ExtensionDays = ext > e.ExtensionDays ? ext : e.ExtensionDays + days;
            e.PlannedEndDate = e.StartDate!.Value.Date.AddDays(e.DurationDays + e.ExtensionDays - 1);
            e.Status = TrainingStatus.InTraining;
            await uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);

            var newWeeks = TrainingSupport.TotalWeeks(e);
            var plans = await uow.Repository<DogTrainingWeeklyPlan>().Table.AsTracking().Where(p => p.EnrollmentId == e.Id).OrderBy(p => p.WeekNumber).ToListAsync(ct);
            var last = plans.LastOrDefault();
            var extra = TrainingSupport.Trim(additionalActivities);

            if (newWeeks > oldWeeks)
            {
                var rows = new List<DogTrainingWeeklyPlan>();
                for (var w = oldWeeks + 1; w <= newWeeks; w++)
                {
                    var start = e.StartDate!.Value.AddDays((w - 1) * 7);
                    var end = start.AddDays(6);
                    if (end > newEnd) end = newEnd;
                    rows.Add(new DogTrainingWeeklyPlan
                    {
                        CompanyId = e.CompanyId, EnrollmentId = e.Id, WeekNumber = w, Title = $"Additional training – Week {w}",
                        Focus = "Additional training", Targets = extra, ScheduleJson = last?.ScheduleJson, StartDate = start, EndDate = end,
                    });
                }
                await uow.Repository<DogTrainingWeeklyPlan>().AddRangeAsync(rows, ct);
            }
            else if (last != null)
            {
                if (extra != null) last.Targets = string.IsNullOrWhiteSpace(last.Targets) ? extra : $"{last.Targets}\nAdditional: {extra}";
                last.EndDate = newEnd;
                await uow.Repository<DogTrainingWeeklyPlan>().UpdateAsync(last, ct);
            }
        }
    }

    public class GetFinalAssessmentsHandler : IRequestHandler<GetFinalAssessmentsQuery, ApiResponse<List<FinalAssessmentDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetFinalAssessmentsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<FinalAssessmentDto>>> Handle(GetFinalAssessmentsQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var enrQ = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);
            if (request.EnrollmentId.HasValue) enrQ = enrQ.Where(e => e.Id == request.EnrollmentId.Value);

            var q = _uow.Repository<DogTrainingFinalAssessment>().TableNoTracking.Where(a => enrQ.Any(e => e.Id == a.EnrollmentId));
            if (!string.IsNullOrWhiteSpace(request.Result)) q = q.Where(a => a.Result == request.Result);
            var list = await q.OrderByDescending(a => a.AssessmentDate).ThenByDescending(a => a.Id).Take(500).ToListAsync(ct);
            return ApiResponse<List<FinalAssessmentDto>>.Ok(await FinalAssessmentMapper.MapAsync(_uow, list, ct));
        }
    }

    public class GetFinalAssessmentByIdHandler : IRequestHandler<GetFinalAssessmentByIdQuery, ApiResponse<FinalAssessmentDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetFinalAssessmentByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<FinalAssessmentDto>> Handle(GetFinalAssessmentByIdQuery request, CancellationToken ct)
        {
            var a = await _uow.Repository<DogTrainingFinalAssessment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (a == null) return ApiResponse<FinalAssessmentDto>.Fail("Assessment not found.");
            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == a.EnrollmentId, ct);
            if (e == null || !TrainingSupport.CanAccess(request.Actor, e)) return ApiResponse<FinalAssessmentDto>.Fail("You do not have access to this assessment.");
            return ApiResponse<FinalAssessmentDto>.Ok((await FinalAssessmentMapper.MapAsync(_uow, new List<DogTrainingFinalAssessment> { a }, ct)).First());
        }
    }

    public class SaveFinalAssessmentHandler : IRequestHandler<SaveFinalAssessmentCommand, ApiResponse<FinalAssessmentDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveFinalAssessmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<FinalAssessmentDto>> Handle(SaveFinalAssessmentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            await TrainingSupport.SyncProgressAsync(_uow, null, ct);

            var e = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.EnrollmentId, ct);
            if (e == null) return ApiResponse<FinalAssessmentDto>.Fail("Enrolment not found.");
            if (e.Status != TrainingStatus.AwaitingFinalAssessment)
                return ApiResponse<FinalAssessmentDto>.Fail("The final assessment can only be done once the training period is complete.");
            if (d.Result is not ("Passed" or "Failed" or "RequiresAdditionalTraining"))
                return ApiResponse<FinalAssessmentDto>.Fail("Choose Passed, Failed or Requires Additional Training.");
            if (d.Items.Count == 0) return ApiResponse<FinalAssessmentDto>.Fail("Score at least one assessment area.");
            if (d.Items.Any(i => i.Score is < 0 or > 10)) return ApiResponse<FinalAssessmentDto>.Fail("Scores must be between 0 and 10.");
            if (d.ExtensionDays is < 0 or > 90) return ApiResponse<FinalAssessmentDto>.Fail("Extension must be between 0 and 90 days.");
            if (d.Result == "RequiresAdditionalTraining" && d.ExtensionDays <= 0)
                return ApiResponse<FinalAssessmentDto>.Fail("Enter how many extra training days are needed.");

            var attempts = await _uow.Repository<DogTrainingFinalAssessment>().TableNoTracking.CountAsync(a => a.EnrollmentId == e.Id, ct);
            var overall = Math.Round((decimal)d.Items.Average(i => i.Score), 1);

            var a = new DogTrainingFinalAssessment
            {
                CompanyId = e.CompanyId, EnrollmentId = e.Id, AttemptNumber = attempts + 1, AssessmentDate = d.AssessmentDate,
                AssessorUserId = request.Actor.UserId, AssessorName = request.Actor.Name,
                ItemsJson = TrainingSupport.ToJson(d.Items.Select(i => new AssessmentAreaDto { Area = i.Area.Trim(), Score = i.Score, Comments = TrainingSupport.Trim(i.Comments) }).ToList()),
                OverallScore = overall, Result = d.Result, Comments = TrainingSupport.Trim(d.Comments),
                ExtensionDays = d.ExtensionDays, AdditionalActivities = TrainingSupport.Trim(d.AdditionalActivities),
                ApprovalStatus = d.Result == "Passed" ? "Pending" : "NotRequired",
            };
            await _uow.Repository<DogTrainingFinalAssessment>().AddAsync(a, ct);

            string message;
            if (d.Result == "Passed")
            {
                e.Status = TrainingStatus.AwaitingApproval;
                await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);
                message = "Assessment passed. It now needs management approval before a certificate can be issued.";
            }
            else if (d.ExtensionDays > 0)
            {
                await TrainingExtension.ApplyAsync(_uow, e, d.ExtensionDays, d.AdditionalActivities, ct);
                message = $"Training extended by {d.ExtensionDays} day(s) with the additional activities.";
            }
            else
            {
                // Failed with no extension yet: stays awaiting assessment; the Training Manager can extend training or re-assess.
                message = "Assessment recorded as failed. Extend the training to give the dog more time, or re-assess.";
            }

            return ApiResponse<FinalAssessmentDto>.Ok((await FinalAssessmentMapper.MapAsync(_uow, new List<DogTrainingFinalAssessment> { a }, ct)).First(), message);
        }
    }

    public class ExtendTrainingHandler : IRequestHandler<ExtendTrainingCommand, ApiResponse<EnrollmentDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        public ExtendTrainingHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<EnrollmentDetailDto>> Handle(ExtendTrainingCommand request, CancellationToken ct)
        {
            if (request.ExtensionDays is < 1 or > 90) return ApiResponse<EnrollmentDetailDto>.Fail("Extension must be between 1 and 90 days.");
            var e = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.EnrollmentId, ct);
            if (e == null) return ApiResponse<EnrollmentDetailDto>.Fail("Enrolment not found.");
            if (e.Status is not (TrainingStatus.AwaitingFinalAssessment or TrainingStatus.InTraining))
                return ApiResponse<EnrollmentDetailDto>.Fail("Training can only be extended while it is active or awaiting its final assessment.");
            if (e.StartDate == null) return ApiResponse<EnrollmentDetailDto>.Fail("Training has not started.");

            await TrainingExtension.ApplyAsync(_uow, e, request.ExtensionDays, request.AdditionalActivities, ct);
            return ApiResponse<EnrollmentDetailDto>.Ok((await EnrollmentDetailBuilder.BuildAsync(_uow, e.Id, ct))!,
                $"Training extended by {request.ExtensionDays} day(s).");
        }
    }

    public class ApproveFinalAssessmentHandler : IRequestHandler<ApproveFinalAssessmentCommand, ApiResponse<FinalAssessmentDto>>
    {
        private readonly IUnitOfWork _uow;
        public ApproveFinalAssessmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<FinalAssessmentDto>> Handle(ApproveFinalAssessmentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var a = await _uow.Repository<DogTrainingFinalAssessment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.AssessmentId, ct);
            if (a == null) return ApiResponse<FinalAssessmentDto>.Fail("Assessment not found.");
            if (a.Result != "Passed" || a.ApprovalStatus != "Pending")
                return ApiResponse<FinalAssessmentDto>.Fail("Only a passed assessment that is awaiting approval can be approved.");
            if (!d.Approve && string.IsNullOrWhiteSpace(d.Comments))
                return ApiResponse<FinalAssessmentDto>.Fail("Give a reason when rejecting an assessment.");

            var e = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == a.EnrollmentId, ct);
            if (e == null) return ApiResponse<FinalAssessmentDto>.Fail("Enrolment not found.");

            a.ApprovalStatus = d.Approve ? "Approved" : "Rejected";
            a.ApprovedByUserId = request.Actor.UserId;
            a.ApprovedOn = DateTime.UtcNow;
            a.ApprovalComments = TrainingSupport.Trim(d.Comments);
            await _uow.Repository<DogTrainingFinalAssessment>().UpdateAsync(a, ct);

            if (d.Approve)
            {
                e.Status = TrainingStatus.Completed;
                e.ActualEndDate = DateTime.Today;
            }
            else
            {
                e.Status = TrainingStatus.AwaitingFinalAssessment; // must be re-assessed
            }
            await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(e, ct);

            return ApiResponse<FinalAssessmentDto>.Ok((await FinalAssessmentMapper.MapAsync(_uow, new List<DogTrainingFinalAssessment> { a }, ct)).First(),
                d.Approve ? "Training completion approved. A certificate can now be issued." : "Assessment rejected. The dog must be re-assessed.");
        }
    }

    // ── Certificates ─────────────────────────────────────────────────────────
    public class GetTrainingCertificatesHandler : IRequestHandler<GetTrainingCertificatesQuery, ApiResponse<List<TrainingCertificateDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingCertificatesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<TrainingCertificateDto>>> Handle(GetTrainingCertificatesQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var enrQ = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);
            var list = await _uow.Repository<DogTrainingCertificate>().TableNoTracking
                .Where(c => enrQ.Any(e => e.Id == c.EnrollmentId)).OrderByDescending(c => c.IssuedOn).Take(500).ToListAsync(ct);
            return ApiResponse<List<TrainingCertificateDto>>.Ok(await CertificateMapper.MapAsync(_uow, list, ct));
        }
    }

    public class GetTrainingCertificateByIdHandler : IRequestHandler<GetTrainingCertificateByIdQuery, ApiResponse<TrainingCertificateDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingCertificateByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingCertificateDto>> Handle(GetTrainingCertificateByIdQuery request, CancellationToken ct)
        {
            var c = await _uow.Repository<DogTrainingCertificate>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (c == null) return ApiResponse<TrainingCertificateDto>.Fail("Certificate not found.");
            return ApiResponse<TrainingCertificateDto>.Ok((await CertificateMapper.MapAsync(_uow, new List<DogTrainingCertificate> { c }, ct)).First());
        }
    }

    public class IssueTrainingCertificateHandler : IRequestHandler<IssueTrainingCertificateCommand, ApiResponse<TrainingCertificateDto>>
    {
        private readonly IUnitOfWork _uow;
        public IssueTrainingCertificateHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingCertificateDto>> Handle(IssueTrainingCertificateCommand request, CancellationToken ct)
        {
            var a = await _uow.Repository<DogTrainingFinalAssessment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.AssessmentId, ct);
            if (a == null) return ApiResponse<TrainingCertificateDto>.Fail("Assessment not found.");

            // Only an approved, successful assessment can produce a certificate.
            if (a.Result != "Passed" || a.ApprovalStatus != "Approved")
                return ApiResponse<TrainingCertificateDto>.Fail("A certificate can only be issued for a passed assessment that management has approved.");

            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == a.EnrollmentId, ct);
            if (e == null || e.Status != TrainingStatus.Completed) return ApiResponse<TrainingCertificateDto>.Fail("The training must be completed first.");

            var existing = await _uow.Repository<DogTrainingCertificate>().TableNoTracking
                .Where(c => c.EnrollmentId == e.Id || c.FinalAssessmentId == a.Id).Select(c => c.IsRevoked).ToListAsync(ct);
            if (existing.Any(revoked => !revoked)) return ApiResponse<TrainingCertificateDto>.Fail("A certificate has already been issued for this training.");
            if (existing.Count > 0) return ApiResponse<TrainingCertificateDto>.Fail("The certificate for this assessment was revoked. Re-assess the dog to issue a new one.");

            var cert = new DogTrainingCertificate
            {
                CompanyId = e.CompanyId, EnrollmentId = e.Id, FinalAssessmentId = a.Id,
                CertificateNumber = await TrainingSupport.NextNumberAsync(_uow, "DTC", true, ct),
                VerificationToken = Guid.NewGuid().ToString("N"),
                IssuedOn = DateTime.UtcNow, IssuedByUserId = request.Actor.UserId,
            };
            await _uow.Repository<DogTrainingCertificate>().AddAsync(cert, ct);
            return ApiResponse<TrainingCertificateDto>.Ok((await CertificateMapper.MapAsync(_uow, new List<DogTrainingCertificate> { cert }, ct)).First(),
                $"Certificate {cert.CertificateNumber} issued.");
        }
    }

    public class RevokeTrainingCertificateHandler : IRequestHandler<RevokeTrainingCertificateCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public RevokeTrainingCertificateHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(RevokeTrainingCertificateCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Reason)) return ApiResponse<bool>.Fail("A reason is required to revoke a certificate.");
            var c = await _uow.Repository<DogTrainingCertificate>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.CertificateId, ct);
            if (c == null) return ApiResponse<bool>.Fail("Certificate not found.");
            if (c.IsRevoked) return ApiResponse<bool>.Fail("Already revoked.");
            c.IsRevoked = true;
            c.RevokedReason = request.Reason.Trim();
            await _uow.Repository<DogTrainingCertificate>().UpdateAsync(c, ct);
            return ApiResponse<bool>.Ok(true, "Certificate revoked. It will no longer verify.");
        }
    }

    public class VerifyTrainingCertificateHandler : IRequestHandler<VerifyTrainingCertificateQuery, ApiResponse<CertificateVerificationDto>>
    {
        private readonly IUnitOfWork _uow;
        public VerifyTrainingCertificateHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CertificateVerificationDto>> Handle(VerifyTrainingCertificateQuery request, CancellationToken ct)
        {
            var token = (request.Token ?? "").Trim();
            if (token.Length < 8) return ApiResponse<CertificateVerificationDto>.Ok(new CertificateVerificationDto { Valid = false, Message = "This certificate could not be found." });

            var c = await _uow.Repository<DogTrainingCertificate>().TableNoTracking.FirstOrDefaultAsync(x => x.VerificationToken == token, ct);
            if (c == null) return ApiResponse<CertificateVerificationDto>.Ok(new CertificateVerificationDto { Valid = false, Message = "This certificate could not be found." });

            var dto = (await CertificateMapper.MapAsync(_uow, new List<DogTrainingCertificate> { c }, ct)).First();
            var company = await _uow.Repository<Company>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == c.CompanyId, ct);
            // Public page: never expose the verification token itself beyond what the caller already has.
            return ApiResponse<CertificateVerificationDto>.Ok(new CertificateVerificationDto
            {
                Valid = !c.IsRevoked,
                Message = c.IsRevoked ? "This certificate has been revoked and is no longer valid." : "This is a valid certificate of completion.",
                Certificate = dto, CompanyName = company?.CompanyName,
            });
        }
    }
}
