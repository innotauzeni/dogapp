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
    internal static class HealthMapper
    {
        public const string DocumentEntityType = "TrainingHealth";
        private static readonly string[] Results = { "Pending", "Passed", "Failed", "RequiresFurtherExamination" };
        public static bool IsValidResult(string? r) => r != null && Results.Contains(r);

        public static async Task<List<HealthAssessmentDto>> LoadAsync(IUnitOfWork uow, List<DogHealthAssessment> list, CancellationToken ct)
        {
            if (list.Count == 0) return new();
            var ids = list.Select(a => a.Id).ToList();
            var enrIds = list.Select(a => a.EnrollmentId).Distinct().ToList();
            var dogIds = list.Select(a => a.DogId).Distinct().ToList();

            var items = (await uow.Repository<DogHealthAssessmentItem>().TableNoTracking.Where(i => ids.Contains(i.AssessmentId)).ToListAsync(ct))
                .GroupBy(i => i.AssessmentId).ToDictionary(g => g.Key, g => g.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList());
            var enrs = await uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => enrIds.Contains(e.Id))
                .Select(e => new { e.Id, e.EnrollmentNumber, e.CustomerId }).ToDictionaryAsync(e => e.Id, ct);
            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, ct);
            var custIds = enrs.Values.Where(e => e.CustomerId != null).Select(e => e.CustomerId!.Value).Distinct().ToList();
            var customers = custIds.Count == 0 ? new Dictionary<int, DogTrainingCustomer>() :
                await uow.Repository<DogTrainingCustomer>().TableNoTracking.Where(c => custIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
            var docCounts = await uow.Repository<DocumentAttachment>().TableNoTracking
                .Where(d => d.EntityType == DocumentEntityType && ids.Contains(d.EntityId))
                .GroupBy(d => d.EntityId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

            return list.Select(a =>
            {
                dogs.TryGetValue(a.DogId, out var dog);
                enrs.TryGetValue(a.EnrollmentId, out var enr);
                DogTrainingCustomer? cust = enr?.CustomerId != null && customers.TryGetValue(enr.CustomerId.Value, out var c) ? c : null;
                return new HealthAssessmentDto
                {
                    Id = a.Id, EnrollmentId = a.EnrollmentId, EnrollmentNumber = enr?.EnrollmentNumber, DogId = a.DogId,
                    DogName = dog?.Name ?? "", DogBreed = dog?.Breed, DogGender = dog?.Gender, DogDateOfBirth = dog?.DateOfBirth,
                    CustomerName = TrainingSupport.CustomerName(cust), CustomerAddress = cust?.Address,
                    AssessmentDate = a.AssessmentDate, ExaminerUserId = a.ExaminerUserId, ExaminerName = a.ExaminerName,
                    ForceNumber = a.ForceNumber, ExaminerPhone = a.ExaminerPhone, DogHandlerName = a.DogHandlerName,
                    Species = a.Species, WeightKg = a.WeightKg, PhysicalCondition = a.PhysicalCondition,
                    VaccinationStatus = a.VaccinationStatus, DewormingStatus = a.DewormingStatus, MedicalConditions = a.MedicalConditions,
                    FleaTickTreatmentDate = a.FleaTickTreatmentDate, CertGoodHealth = a.CertGoodHealth,
                    CertVaccinationsCurrent = a.CertVaccinationsCurrent, CertNoParasites = a.CertNoParasites, CertNoBiteHistory = a.CertNoBiteHistory,
                    Recommendation = a.Recommendation, Comments = a.Comments, InspectorSignatureName = a.InspectorSignatureName, SignedOn = a.SignedOn,
                    Result = a.Result, ResultDate = a.ResultDate,
                    DocumentCount = docCounts.TryGetValue(a.Id, out var n) ? n : 0,
                    Items = (items.TryGetValue(a.Id, out var its) ? its : new()).Select(i => new HealthItemDto
                    {
                        Id = i.Id, Category = i.Category, Name = i.Name, Status = i.Status, Notes = i.Notes, Administered = i.Administered,
                        TreatmentDate = i.TreatmentDate, Reason = i.Reason, BesNotified = i.BesNotified, SortOrder = i.SortOrder
                    }).ToList()
                };
            }).ToList();
        }

        public static async Task<List<HealthAssessmentDto>> LoadForEnrollmentAsync(IUnitOfWork uow, int enrollmentId, CancellationToken ct)
        {
            var list = await uow.Repository<DogHealthAssessment>().TableNoTracking.Where(a => a.EnrollmentId == enrollmentId)
                .OrderByDescending(a => a.AssessmentDate).ThenByDescending(a => a.Id).ToListAsync(ct);
            return await LoadAsync(uow, list, ct);
        }
    }

    public class GetHealthAssessmentsHandler : IRequestHandler<GetHealthAssessmentsQuery, ApiResponse<List<HealthAssessmentDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetHealthAssessmentsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<HealthAssessmentDto>>> Handle(GetHealthAssessmentsQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var enrQ = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);
            if (request.EnrollmentId.HasValue) enrQ = enrQ.Where(e => e.Id == request.EnrollmentId.Value);

            var q = _uow.Repository<DogHealthAssessment>().TableNoTracking.Where(a => enrQ.Any(e => e.Id == a.EnrollmentId));
            if (!string.IsNullOrWhiteSpace(request.Result)) q = q.Where(a => a.Result == request.Result);

            var list = await q.OrderByDescending(a => a.AssessmentDate).ThenByDescending(a => a.Id).Take(500).ToListAsync(ct);
            return ApiResponse<List<HealthAssessmentDto>>.Ok(await HealthMapper.LoadAsync(_uow, list, ct));
        }
    }

    public class GetHealthAssessmentByIdHandler : IRequestHandler<GetHealthAssessmentByIdQuery, ApiResponse<HealthAssessmentDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetHealthAssessmentByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<HealthAssessmentDto>> Handle(GetHealthAssessmentByIdQuery request, CancellationToken ct)
        {
            var a = await _uow.Repository<DogHealthAssessment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (a == null) return ApiResponse<HealthAssessmentDto>.Fail("Assessment not found.");
            var enr = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(e => e.Id == a.EnrollmentId, ct);
            if (enr == null || !TrainingSupport.CanAccess(request.Actor, enr)) return ApiResponse<HealthAssessmentDto>.Fail("You do not have access to this assessment.");
            return ApiResponse<HealthAssessmentDto>.Ok((await HealthMapper.LoadAsync(_uow, new List<DogHealthAssessment> { a }, ct)).First());
        }
    }

    public class SaveHealthAssessmentHandler : IRequestHandler<SaveHealthAssessmentCommand, ApiResponse<HealthAssessmentDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveHealthAssessmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<HealthAssessmentDto>> Handle(SaveHealthAssessmentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (d.WeightKg is < 0 or > 200) return ApiResponse<HealthAssessmentDto>.Fail("Weight must be between 0 and 200 kg.");
            if (d.Recommendation is not ("None" or "Warning" or "PositiveChange" or "Withdrawal"))
                return ApiResponse<HealthAssessmentDto>.Fail("Invalid recommendation.");

            var enr = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(e => e.Id == d.EnrollmentId, ct);
            if (enr == null) return ApiResponse<HealthAssessmentDto>.Fail("Enrolment not found.");

            var repo = _uow.Repository<DogHealthAssessment>();
            DogHealthAssessment a;

            if (d.Id.HasValue && d.Id > 0)
            {
                var existing = await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.Id.Value, ct);
                if (existing == null) return ApiResponse<HealthAssessmentDto>.Fail("Assessment not found.");
                if (existing.Result is "Passed" or "Failed")
                    return ApiResponse<HealthAssessmentDto>.Fail("A decided assessment cannot be edited. Start a re-assessment instead.");
                a = existing;
            }
            else
            {
                // A new (re-)assessment is only possible before the dog has passed.
                if (enr.Status is not (TrainingStatus.AwaitingHealthAssessment or TrainingStatus.HealthFailed))
                    return ApiResponse<HealthAssessmentDto>.Fail("This enrolment is not waiting for a health assessment.");
                var undecided = await repo.TableNoTracking.AnyAsync(x => x.EnrollmentId == enr.Id && x.Result == "Pending", ct);
                if (undecided) return ApiResponse<HealthAssessmentDto>.Fail("There is already an assessment awaiting a result. Record its result first.");
                a = new DogHealthAssessment { CompanyId = enr.CompanyId, EnrollmentId = enr.Id, DogId = enr.DogId, Result = "Pending" };
            }

            a.AssessmentDate = d.AssessmentDate;
            a.ExaminerUserId = request.Actor.UserId;
            a.ExaminerName = TrainingSupport.Trim(d.ExaminerName) ?? request.Actor.Name;
            a.ForceNumber = TrainingSupport.Trim(d.ForceNumber);
            a.ExaminerPhone = TrainingSupport.Trim(d.ExaminerPhone);
            a.DogHandlerName = TrainingSupport.Trim(d.DogHandlerName);
            a.Species = TrainingSupport.Trim(d.Species) ?? "Dog";
            a.WeightKg = d.WeightKg;
            a.PhysicalCondition = TrainingSupport.Trim(d.PhysicalCondition);
            a.VaccinationStatus = TrainingSupport.Trim(d.VaccinationStatus);
            a.DewormingStatus = TrainingSupport.Trim(d.DewormingStatus);
            a.MedicalConditions = TrainingSupport.Trim(d.MedicalConditions);
            a.FleaTickTreatmentDate = d.FleaTickTreatmentDate;
            a.CertGoodHealth = d.CertGoodHealth;
            a.CertVaccinationsCurrent = d.CertVaccinationsCurrent;
            a.CertNoParasites = d.CertNoParasites;
            a.CertNoBiteHistory = d.CertNoBiteHistory;
            a.Recommendation = d.Recommendation;
            a.Comments = TrainingSupport.Trim(d.Comments);
            a.InspectorSignatureName = TrainingSupport.Trim(d.InspectorSignatureName);
            a.SignedOn = d.SignedOn;

            if (a.Id == 0) await repo.AddAsync(a, ct); else await repo.UpdateAsync(a, ct);

            // Replace the form lines as a set. Start from the standard form if none were sent.
            var oldItems = await _uow.Repository<DogHealthAssessmentItem>().Table.AsTracking().Where(i => i.AssessmentId == a.Id).ToListAsync(ct);
            if (oldItems.Count > 0) await _uow.Repository<DogHealthAssessmentItem>().DeleteRangeAsync(oldItems, ct);

            var lines = d.Items;
            if (lines.Count == 0)
            {
                lines = TrainingSupport.ExamItems.Select((n, i) => new HealthItemDto { Category = "Exam", Name = n, SortOrder = i + 1 })
                    .Concat(TrainingSupport.TreatmentItems.Select((n, i) => new HealthItemDto { Category = "Treatment", Name = n, SortOrder = i + 1 })).ToList();
            }
            var newItems = lines.Where(i => !string.IsNullOrWhiteSpace(i.Name)).Select((i, idx) => new DogHealthAssessmentItem
            {
                CompanyId = a.CompanyId, AssessmentId = a.Id,
                Category = i.Category == "Treatment" ? "Treatment" : "Exam",
                Name = i.Name.Trim(),
                Status = i.Status is "Normal" or "Abnormal" ? i.Status : null,
                Notes = TrainingSupport.Trim(i.Notes),
                Administered = i.Administered, TreatmentDate = i.TreatmentDate, Reason = TrainingSupport.Trim(i.Reason),
                BesNotified = i.BesNotified, SortOrder = i.SortOrder > 0 ? i.SortOrder : idx + 1
            }).ToList();
            if (newItems.Count > 0) await _uow.Repository<DogHealthAssessmentItem>().AddRangeAsync(newItems, ct);

            // A fresh assessment after a failure re-opens the enrolment.
            if (enr.Status == TrainingStatus.HealthFailed)
            {
                enr.Status = TrainingStatus.AwaitingHealthAssessment;
                await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(enr, ct);
            }

            return ApiResponse<HealthAssessmentDto>.Ok(
                (await HealthMapper.LoadAsync(_uow, new List<DogHealthAssessment> { await repo.TableNoTracking.FirstAsync(x => x.Id == a.Id, ct) }, ct)).First(),
                "Health assessment saved. Record its result to continue.");
        }
    }

    public class DecideHealthAssessmentHandler : IRequestHandler<DecideHealthAssessmentCommand, ApiResponse<HealthAssessmentDto>>
    {
        private readonly IUnitOfWork _uow;
        public DecideHealthAssessmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<HealthAssessmentDto>> Handle(DecideHealthAssessmentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (!HealthMapper.IsValidResult(d.Result) || d.Result == "Pending")
                return ApiResponse<HealthAssessmentDto>.Fail("Choose Passed, Failed or Requires Further Examination.");

            var a = await _uow.Repository<DogHealthAssessment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.AssessmentId, ct);
            if (a == null) return ApiResponse<HealthAssessmentDto>.Fail("Assessment not found.");
            if (a.Result is "Passed" or "Failed") return ApiResponse<HealthAssessmentDto>.Fail("A result has already been recorded for this assessment.");
            if (d.Result == "Passed" && a.Recommendation == "Withdrawal")
                return ApiResponse<HealthAssessmentDto>.Fail("This assessment recommends withdrawal of the dog, so it cannot be passed.");

            var enr = await _uow.Repository<DogTrainingEnrollment>().Table.AsTracking().FirstOrDefaultAsync(e => e.Id == a.EnrollmentId, ct);
            if (enr == null) return ApiResponse<HealthAssessmentDto>.Fail("Enrolment not found.");

            a.Result = d.Result;
            a.ResultDate = DateTime.UtcNow;
            a.DecidedByUserId = request.Actor.UserId;
            if (!string.IsNullOrWhiteSpace(d.Comments))
                a.Comments = string.IsNullOrWhiteSpace(a.Comments) ? d.Comments!.Trim() : $"{a.Comments}\n— Result note: {d.Comments!.Trim()}";
            await _uow.Repository<DogHealthAssessment>().UpdateAsync(a, ct);

            // Only a Passed assessment lets the dog proceed.
            if (enr.Status is TrainingStatus.AwaitingHealthAssessment or TrainingStatus.HealthFailed)
            {
                enr.Status = d.Result switch
                {
                    "Passed" => TrainingStatus.HealthPassed,
                    "Failed" => TrainingStatus.HealthFailed,
                    _ => TrainingStatus.AwaitingHealthAssessment
                };
                await _uow.Repository<DogTrainingEnrollment>().UpdateAsync(enr, ct);
            }

            var message = d.Result == "Passed" ? "Passed. The dog can now be invoiced for training."
                : d.Result == "Failed" ? "Failed. The dog cannot start training — it can be re-assessed."
                : "Marked as requiring further examination. Record a new assessment when ready.";
            return ApiResponse<HealthAssessmentDto>.Ok(
                (await HealthMapper.LoadAsync(_uow, new List<DogHealthAssessment> { await _uow.Repository<DogHealthAssessment>().TableNoTracking.FirstAsync(x => x.Id == a.Id, ct) }, ct)).First(), message);
        }
    }

    // ── Handler capacity / profiles ──────────────────────────────────────────
    public class GetHandlerCapacityHandler : IRequestHandler<GetHandlerCapacityQuery, ApiResponse<List<HandlerCapacityDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetHandlerCapacityHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<HandlerCapacityDto>>> Handle(GetHandlerCapacityQuery request, CancellationToken ct)
            => ApiResponse<List<HandlerCapacityDto>>.Ok(await HandlerCapacityBuilder.BuildAsync(_uow, request.CompanyId, null, ct));
    }

    internal static class HandlerCapacityBuilder
    {
        private static readonly string[] Busy =
        {
            TrainingStatus.Ready, TrainingStatus.InTraining, TrainingStatus.AwaitingFinalAssessment, TrainingStatus.AwaitingApproval
        };

        public static async Task<List<HandlerCapacityDto>> BuildAsync(IUnitOfWork uow, int companyId, string? onlyUserId, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(uow, companyId, ct);

            // Any active system user of the company (or its branches) can be a handler. Customer-portal accounts are left out.
            var customerRole = await uow.Repository<Role>().TableNoTracking.FirstOrDefaultAsync(r => r.Name == "Customer", ct);
            var excluded = customerRole == null ? new List<string>()
                : await uow.Repository<ApplicationRoleUser>().TableNoTracking.Where(ur => ur.RoleId == customerRole.Id).Select(ur => ur.UserId).ToListAsync(ct);
            var users = await uow.Repository<ApplicationUser>().TableNoTracking
                .Where(u => !excluded.Contains(u.Id) && companyIds.Contains(u.CompanyId) && (u.Status == null || u.Status == "ACTIVE") && (onlyUserId == null || u.Id == onlyUserId))
                .OrderBy(u => u.Name).ThenBy(u => u.Surname).ToListAsync(ct);
            var ids = users.Select(u => u.Id).ToList();

            var profiles = await uow.Repository<DogTrainingHandlerProfile>().TableNoTracking.Where(p => ids.Contains(p.UserId)).ToDictionaryAsync(p => p.UserId, ct);
            var enrolments = await uow.Repository<DogTrainingEnrollment>().TableNoTracking
                .Where(e => e.HandlerUserId != null && ids.Contains(e.HandlerUserId) && Busy.Contains(e.Status)).ToListAsync(ct);
            var dogIds = enrolments.Select(e => e.DogId).Distinct().ToList();
            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);

            return users.Select(u =>
            {
                profiles.TryGetValue(u.Id, out var p);
                var mine = enrolments.Where(e => e.HandlerUserId == u.Id).ToList();
                var max = p?.MaxDogs ?? 5;
                var available = p?.IsAvailable ?? true;
                return new HandlerCapacityDto
                {
                    UserId = u.Id, Name = TrainingSupport.FullName(u), Email = u.Email, Phone = u.PhoneNumber,
                    MaxDogs = max, CurrentDogs = mine.Count, Specialisation = p?.Specialisation, IsAvailable = available,
                    CanAccept = available && mine.Count < max, Notes = p?.Notes,
                    Dogs = mine.Select(e => new AssignedDogDto
                    {
                        EnrollmentId = e.Id, DogName = dogs.TryGetValue(e.DogId, out var n) ? n : "", Status = e.Status,
                        CurrentWeek = TrainingSupport.WeekAndDay(e, DateTime.Today).Week
                    }).ToList()
                };
            }).ToList();
        }
    }

    public class SaveHandlerProfileHandler : IRequestHandler<SaveHandlerProfileCommand, ApiResponse<HandlerCapacityDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveHandlerProfileHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<HandlerCapacityDto>> Handle(SaveHandlerProfileCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (d.MaxDogs < 1 || d.MaxDogs > 50) return ApiResponse<HandlerCapacityDto>.Fail("Maximum dogs must be between 1 and 50.");
            var user = await _uow.Repository<ApplicationUser>().TableNoTracking.FirstOrDefaultAsync(u => u.Id == d.UserId, ct);
            if (user == null) return ApiResponse<HandlerCapacityDto>.Fail("Handler not found.");

            var repo = _uow.Repository<DogTrainingHandlerProfile>();
            var profile = await repo.Table.AsTracking().FirstOrDefaultAsync(p => p.UserId == d.UserId, ct);
            if (profile == null)
            {
                await repo.AddAsync(new DogTrainingHandlerProfile
                {
                    CompanyId = user.CompanyId, UserId = d.UserId, MaxDogs = d.MaxDogs,
                    Specialisation = TrainingSupport.Trim(d.Specialisation), IsAvailable = d.IsAvailable, Notes = TrainingSupport.Trim(d.Notes)
                }, ct);
            }
            else
            {
                profile.MaxDogs = d.MaxDogs;
                profile.Specialisation = TrainingSupport.Trim(d.Specialisation);
                profile.IsAvailable = d.IsAvailable;
                profile.Notes = TrainingSupport.Trim(d.Notes);
                await repo.UpdateAsync(profile, ct);
            }

            var list = await HandlerCapacityBuilder.BuildAsync(_uow, user.CompanyId, d.UserId, ct);
            return list.Count > 0 ? ApiResponse<HandlerCapacityDto>.Ok(list[0], "Handler details saved.")
                                  : ApiResponse<HandlerCapacityDto>.Fail("That user could not be found or is not active.");
        }
    }
}
