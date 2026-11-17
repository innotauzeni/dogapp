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
    internal static class LogMapper
    {
        public const string DocumentEntityType = "TrainingLog";

        public static async Task<List<DailyLogDto>> MapAsync(IUnitOfWork uow, List<DogTrainingDailyLog> logs, CancellationToken ct)
        {
            if (logs.Count == 0) return new();
            var dogIds = logs.Select(l => l.DogId).Distinct().ToList();
            var userIds = logs.Select(l => l.HandlerUserId).Distinct().ToList();
            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);
            var users = (await uow.Repository<ApplicationUser>().TableNoTracking.Where(u => userIds.Contains(u.Id)).ToListAsync(ct))
                .ToDictionary(u => u.Id, u => TrainingSupport.FullName(u));

            return logs.Select(l => new DailyLogDto
            {
                Id = l.Id, EnrollmentId = l.EnrollmentId, DogId = l.DogId, DogName = dogs.TryGetValue(l.DogId, out var dn) ? dn : "",
                HandlerUserId = l.HandlerUserId, HandlerName = users.TryGetValue(l.HandlerUserId, out var hn) ? hn : null,
                TrainingDate = l.TrainingDate, WeekNumber = l.WeekNumber, DayNumber = l.DayNumber,
                ActivitiesPerformed = l.ActivitiesPerformed, CommandsPracticed = l.CommandsPracticed, DurationMinutes = l.DurationMinutes,
                Slots = TrainingSupport.ParseList<LogSlotDto>(l.SlotsJson), AverageSlotRating = l.AverageSlotRating, Performance = l.Performance,
                BehaviourObservations = l.BehaviourObservations, Challenges = l.Challenges, Improvements = l.Improvements,
                Recommendations = l.Recommendations, NextObjectives = l.NextObjectives,
            }).ToList();
        }
    }

    public class GetWeeklyPlansHandler : IRequestHandler<GetWeeklyPlansQuery, ApiResponse<List<WeeklyPlanDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetWeeklyPlansHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<WeeklyPlanDto>>> Handle(GetWeeklyPlansQuery request, CancellationToken ct)
        {
            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.EnrollmentId, ct);
            if (e == null) return ApiResponse<List<WeeklyPlanDto>>.Fail("Enrolment not found.");
            if (!TrainingSupport.CanAccess(request.Actor, e)) return ApiResponse<List<WeeklyPlanDto>>.Fail("You can only open dogs that are allocated to you.");
            var plans = await _uow.Repository<DogTrainingWeeklyPlan>().TableNoTracking.Where(p => p.EnrollmentId == e.Id).OrderBy(p => p.WeekNumber).ToListAsync(ct);
            return ApiResponse<List<WeeklyPlanDto>>.Ok(plans.Select(TrainingSupport.MapPlan).ToList());
        }
    }

    public class SaveWeeklyPlanHandler : IRequestHandler<SaveWeeklyPlanCommand, ApiResponse<WeeklyPlanDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveWeeklyPlanHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<WeeklyPlanDto>> Handle(SaveWeeklyPlanCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (string.IsNullOrWhiteSpace(d.Title)) return ApiResponse<WeeklyPlanDto>.Fail("Week title is required.");
            var plan = await _uow.Repository<DogTrainingWeeklyPlan>().Table.AsTracking().FirstOrDefaultAsync(p => p.Id == d.Id, ct);
            if (plan == null) return ApiResponse<WeeklyPlanDto>.Fail("Weekly plan not found.");

            plan.Title = d.Title.Trim();
            plan.Focus = TrainingSupport.Trim(d.Focus);
            plan.Targets = TrainingSupport.Trim(d.Targets);
            plan.ScheduleJson = TrainingSupport.ToJson(d.Schedule.Where(s => !string.IsNullOrWhiteSpace(s.Title))
                .Select(s => new ScheduleSlotDto { Time = (s.Time ?? "").Trim(), Title = s.Title.Trim(), Purpose = TrainingSupport.Trim(s.Purpose) }).ToList());
            await _uow.Repository<DogTrainingWeeklyPlan>().UpdateAsync(plan, ct);
            return ApiResponse<WeeklyPlanDto>.Ok(TrainingSupport.MapPlan(plan), "This dog's plan for the week was updated.");
        }
    }

    public class GetDailyLogsHandler : IRequestHandler<GetDailyLogsQuery, ApiResponse<List<DailyLogDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetDailyLogsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<DailyLogDto>>> Handle(GetDailyLogsQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var enrQ = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);

            var q = _uow.Repository<DogTrainingDailyLog>().TableNoTracking.Where(l => enrQ.Any(e => e.Id == l.EnrollmentId));
            if (request.EnrollmentId.HasValue) q = q.Where(l => l.EnrollmentId == request.EnrollmentId.Value);
            if (request.Date.HasValue) { var day = request.Date.Value.Date; q = q.Where(l => l.TrainingDate == day); }
            if (request.From.HasValue) { var f = request.From.Value.Date; q = q.Where(l => l.TrainingDate >= f); }
            if (request.To.HasValue) { var t = request.To.Value.Date; q = q.Where(l => l.TrainingDate <= t); }

            var list = await q.OrderByDescending(l => l.TrainingDate).ThenByDescending(l => l.Id).Take(500).ToListAsync(ct);
            return ApiResponse<List<DailyLogDto>>.Ok(await LogMapper.MapAsync(_uow, list, ct));
        }
    }

    public class GetDailyLogForDayHandler : IRequestHandler<GetDailyLogForDayQuery, ApiResponse<DailyLogDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetDailyLogForDayHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<DailyLogDto>> Handle(GetDailyLogForDayQuery request, CancellationToken ct)
        {
            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.EnrollmentId, ct);
            if (e == null) return ApiResponse<DailyLogDto>.Fail("Enrolment not found.");
            if (!TrainingSupport.CanAccess(request.Actor, e)) return ApiResponse<DailyLogDto>.Fail("You can only open dogs that are allocated to you.");

            var day = request.Date.Date;
            var log = await _uow.Repository<DogTrainingDailyLog>().TableNoTracking.FirstOrDefaultAsync(l => l.EnrollmentId == e.Id && l.TrainingDate == day, ct);
            if (log != null) return ApiResponse<DailyLogDto>.Ok((await LogMapper.MapAsync(_uow, new List<DogTrainingDailyLog> { log }, ct)).First());

            // Nothing recorded yet: hand back an empty form pre-filled with that day's schedule from the dog's weekly plan.
            var (week, dayNo) = TrainingSupport.WeekAndDay(e, day);
            var plan = await _uow.Repository<DogTrainingWeeklyPlan>().TableNoTracking.FirstOrDefaultAsync(p => p.EnrollmentId == e.Id && p.WeekNumber == week, ct);
            var dog = await _uow.Repository<DogTrainingDog>().TableNoTracking.FirstOrDefaultAsync(d => d.Id == e.DogId, ct);
            return ApiResponse<DailyLogDto>.Ok(new DailyLogDto
            {
                Id = 0, EnrollmentId = e.Id, DogId = e.DogId, DogName = dog?.Name ?? "", HandlerUserId = request.Actor.UserId,
                TrainingDate = day, WeekNumber = week, DayNumber = dayNo,
                Slots = TrainingSupport.ParseSchedule(plan?.ScheduleJson).Select(s => new LogSlotDto { Time = s.Time, Title = s.Title }).ToList(),
            });
        }
    }

    public class SaveDailyLogHandler : IRequestHandler<SaveDailyLogCommand, ApiResponse<DailyLogDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveDailyLogHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<DailyLogDto>> Handle(SaveDailyLogCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == d.EnrollmentId, ct);
            if (e == null) return ApiResponse<DailyLogDto>.Fail("Enrolment not found.");
            if (!TrainingSupport.CanAccess(request.Actor, e)) return ApiResponse<DailyLogDto>.Fail("You can only record training for dogs allocated to you.");
            if (e.Status is not (TrainingStatus.InTraining or TrainingStatus.AwaitingFinalAssessment))
                return ApiResponse<DailyLogDto>.Fail("Training is not active for this dog.");

            var day = d.TrainingDate.Date;
            if (e.StartDate == null || day < e.StartDate.Value.Date) return ApiResponse<DailyLogDto>.Fail("The date is before training started.");
            if (day > DateTime.Today) return ApiResponse<DailyLogDto>.Fail("You cannot record training for a future date.");
            if (d.DurationMinutes < 0 || d.DurationMinutes > 1440) return ApiResponse<DailyLogDto>.Fail("Training duration is not valid.");
            if (d.Slots.Any(s => s.Rating is < 0 or > 10)) return ApiResponse<DailyLogDto>.Fail("Ratings must be between 0 and 10.");

            var avg = TrainingSupport.Average(d.Slots.Where(s => s.Done).Select(s => s.Rating));
            var performance = TrainingSupport.Trim(d.Performance) ?? (avg.HasValue ? TrainingSupport.PerformanceFromAverage(avg.Value) : "Good");
            if (!TrainingSupport.PerformanceLevels.Contains(performance))
                return ApiResponse<DailyLogDto>.Fail("Performance must be Excellent, Very Good, Good, Needs Improvement or Poor.");

            var (week, dayNo) = TrainingSupport.WeekAndDay(e, day);
            var repo = _uow.Repository<DogTrainingDailyLog>();
            var log = await repo.Table.AsTracking().FirstOrDefaultAsync(l => l.EnrollmentId == e.Id && l.TrainingDate == day, ct);
            var isNew = log == null;
            log ??= new DogTrainingDailyLog { CompanyId = e.CompanyId, EnrollmentId = e.Id, DogId = e.DogId, TrainingDate = day };

            log.HandlerUserId = request.Actor.UserId;
            log.WeekNumber = week;
            log.DayNumber = dayNo;
            log.ActivitiesPerformed = TrainingSupport.Trim(d.ActivitiesPerformed);
            log.CommandsPracticed = TrainingSupport.Trim(d.CommandsPracticed);
            log.DurationMinutes = d.DurationMinutes;
            log.SlotsJson = TrainingSupport.ToJson(d.Slots);
            log.AverageSlotRating = avg;
            log.Performance = performance;
            log.BehaviourObservations = TrainingSupport.Trim(d.BehaviourObservations);
            log.Challenges = TrainingSupport.Trim(d.Challenges);
            log.Improvements = TrainingSupport.Trim(d.Improvements);
            log.Recommendations = TrainingSupport.Trim(d.Recommendations);
            log.NextObjectives = TrainingSupport.Trim(d.NextObjectives);

            if (isNew) await repo.AddAsync(log, ct); else await repo.UpdateAsync(log, ct);
            var fresh = await repo.TableNoTracking.FirstAsync(l => l.Id == log.Id, ct);
            return ApiResponse<DailyLogDto>.Ok((await LogMapper.MapAsync(_uow, new List<DogTrainingDailyLog> { fresh }, ct)).First(),
                isNew ? "Daily training recorded." : "Daily training updated.");
        }
    }

    public class GetMyTrainingDogsHandler : IRequestHandler<GetMyTrainingDogsQuery, ApiResponse<List<EnrollmentListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetMyTrainingDogsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<EnrollmentListDto>>> Handle(GetMyTrainingDogsQuery request, CancellationToken ct)
        {
            await TrainingSupport.SyncProgressAsync(_uow, null, ct);
            var mine = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking
                .Where(e => e.HandlerUserId == request.Actor.UserId &&
                            (e.Status == TrainingStatus.InTraining || e.Status == TrainingStatus.AwaitingFinalAssessment || e.Status == TrainingStatus.Ready))
                .OrderBy(e => e.Status).ThenBy(e => e.StartDate).ToListAsync(ct);
            return ApiResponse<List<EnrollmentListDto>>.Ok(await TrainingSupport.BuildListAsync(_uow, mine, ct));
        }
    }

    // ── Progress reports ─────────────────────────────────────────────────────
    internal static class ReportMapper
    {
        public static async Task<List<ProgressReportDto>> MapAsync(IUnitOfWork uow, List<DogTrainingProgressReport> reports, CancellationToken ct)
        {
            if (reports.Count == 0) return new();
            var enrIds = reports.Select(r => r.EnrollmentId).Distinct().ToList();
            var enrs = await uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => enrIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
            var dogIds = enrs.Values.Select(e => e.DogId).Distinct().ToList();
            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);
            var custIds = enrs.Values.Where(e => e.CustomerId != null).Select(e => e.CustomerId!.Value).Distinct().ToList();
            var customers = custIds.Count == 0 ? new Dictionary<int, string>() :
                (await uow.Repository<DogTrainingCustomer>().TableNoTracking.Where(c => custIds.Contains(c.Id)).ToListAsync(ct)).ToDictionary(c => c.Id, c => TrainingSupport.CustomerName(c));
            var userIds = reports.Select(r => r.HandlerUserId).Concat(reports.Where(r => r.ReviewedByUserId != null).Select(r => r.ReviewedByUserId!)).Distinct().ToList();
            var users = (await uow.Repository<ApplicationUser>().TableNoTracking.Where(u => userIds.Contains(u.Id)).ToListAsync(ct))
                .ToDictionary(u => u.Id, u => TrainingSupport.FullName(u));
            var logs = await uow.Repository<DogTrainingDailyLog>().TableNoTracking.Where(l => enrIds.Contains(l.EnrollmentId))
                .Select(l => new { l.EnrollmentId, l.WeekNumber, l.AverageSlotRating }).ToListAsync(ct);

            return reports.Select(r =>
            {
                enrs.TryGetValue(r.EnrollmentId, out var e);
                var weekLogs = logs.Where(l => l.EnrollmentId == r.EnrollmentId && l.WeekNumber == r.WeekNumber).ToList();
                var rated = weekLogs.Where(l => l.AverageSlotRating.HasValue).Select(l => l.AverageSlotRating!.Value).ToList();
                return new ProgressReportDto
                {
                    Id = r.Id, EnrollmentId = r.EnrollmentId, EnrollmentNumber = e?.EnrollmentNumber,
                    DogName = e != null && dogs.TryGetValue(e.DogId, out var dn) ? dn : "",
                    CustomerName = e?.CustomerId != null && customers.TryGetValue(e.CustomerId.Value, out var cn) ? cn : null,
                    WeekNumber = r.WeekNumber, HandlerUserId = r.HandlerUserId, HandlerName = users.TryGetValue(r.HandlerUserId, out var hn) ? hn : null,
                    Objectives = r.Objectives, ActivitiesCompleted = r.ActivitiesCompleted, ActivitiesOutstanding = r.ActivitiesOutstanding,
                    PerformanceSummary = r.PerformanceSummary, Challenges = r.Challenges, Improvements = r.Improvements, Recommendations = r.Recommendations,
                    Status = r.Status, SubmittedOn = r.SubmittedOn,
                    ReviewedByName = r.ReviewedByUserId != null && users.TryGetValue(r.ReviewedByUserId, out var rn) ? rn : null,
                    ReviewedOn = r.ReviewedOn, ReviewComments = r.ReviewComments,
                    AverageRating = rated.Count == 0 ? null : Math.Round(rated.Average(), 1), LogsInWeek = weekLogs.Count,
                };
            }).ToList();
        }
    }

    public class GetProgressReportsHandler : IRequestHandler<GetProgressReportsQuery, ApiResponse<List<ProgressReportDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetProgressReportsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ProgressReportDto>>> Handle(GetProgressReportsQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var enrQ = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);

            var q = _uow.Repository<DogTrainingProgressReport>().TableNoTracking.Where(r => enrQ.Any(e => e.Id == r.EnrollmentId));
            if (!string.IsNullOrWhiteSpace(request.Status)) q = q.Where(r => r.Status == request.Status);
            if (request.EnrollmentId.HasValue) q = q.Where(r => r.EnrollmentId == request.EnrollmentId.Value);

            var list = await q.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt).Take(500).ToListAsync(ct);
            return ApiResponse<List<ProgressReportDto>>.Ok(await ReportMapper.MapAsync(_uow, list, ct));
        }
    }

    public class GetProgressReportByIdHandler : IRequestHandler<GetProgressReportByIdQuery, ApiResponse<ProgressReportDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetProgressReportByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProgressReportDto>> Handle(GetProgressReportByIdQuery request, CancellationToken ct)
        {
            var r = await _uow.Repository<DogTrainingProgressReport>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (r == null) return ApiResponse<ProgressReportDto>.Fail("Report not found.");
            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == r.EnrollmentId, ct);
            if (e == null || !TrainingSupport.CanAccess(request.Actor, e)) return ApiResponse<ProgressReportDto>.Fail("You do not have access to this report.");
            return ApiResponse<ProgressReportDto>.Ok((await ReportMapper.MapAsync(_uow, new List<DogTrainingProgressReport> { r }, ct)).First());
        }
    }

    public class GetProgressReportDraftHandler : IRequestHandler<GetProgressReportDraftQuery, ApiResponse<ProgressReportDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetProgressReportDraftHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProgressReportDto>> Handle(GetProgressReportDraftQuery request, CancellationToken ct)
        {
            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.EnrollmentId, ct);
            if (e == null) return ApiResponse<ProgressReportDto>.Fail("Enrolment not found.");
            if (!TrainingSupport.CanAccess(request.Actor, e)) return ApiResponse<ProgressReportDto>.Fail("You can only report on dogs allocated to you.");

            // Already started? return what was saved.
            var existing = await _uow.Repository<DogTrainingProgressReport>().TableNoTracking
                .FirstOrDefaultAsync(r => r.EnrollmentId == e.Id && r.WeekNumber == request.WeekNumber, ct);
            if (existing != null) return ApiResponse<ProgressReportDto>.Ok((await ReportMapper.MapAsync(_uow, new List<DogTrainingProgressReport> { existing }, ct)).First());

            var plan = await _uow.Repository<DogTrainingWeeklyPlan>().TableNoTracking.FirstOrDefaultAsync(p => p.EnrollmentId == e.Id && p.WeekNumber == request.WeekNumber, ct);
            var logs = await _uow.Repository<DogTrainingDailyLog>().TableNoTracking
                .Where(l => l.EnrollmentId == e.Id && l.WeekNumber == request.WeekNumber).OrderBy(l => l.TrainingDate).ToListAsync(ct);
            var dog = await _uow.Repository<DogTrainingDog>().TableNoTracking.FirstOrDefaultAsync(d => d.Id == e.DogId, ct);

            // Pre-fill from the week's plan and the daily logs so the handler only has to review and add comments.
            var slots = logs.SelectMany(l => TrainingSupport.ParseList<LogSlotDto>(l.SlotsJson)).ToList();
            var done = slots.Where(s => s.Done).GroupBy(s => s.Title).Select(g => $"{g.Key} ({g.Count()}×)").ToList();
            var missed = slots.Where(s => !s.Done).GroupBy(s => s.Title).Select(g => $"{g.Key} (not done {g.Count()}×)").ToList();
            var rated = logs.Where(l => l.AverageSlotRating.HasValue).Select(l => l.AverageSlotRating!.Value).ToList();
            var byPerformance = logs.GroupBy(l => l.Performance).Select(g => $"{g.Count()} {g.Key}").ToList();

            var summary = logs.Count == 0 ? null :
                $"{logs.Count} training day(s) logged" + (rated.Count > 0 ? $", average rating {Math.Round(rated.Average(), 1)}/10" : "") +
                (byPerformance.Count > 0 ? $". Days: {string.Join(", ", byPerformance)}." : ".");

            return ApiResponse<ProgressReportDto>.Ok(new ProgressReportDto
            {
                Id = 0, EnrollmentId = e.Id, EnrollmentNumber = e.EnrollmentNumber, DogName = dog?.Name ?? "", WeekNumber = request.WeekNumber,
                HandlerUserId = request.Actor.UserId, Status = "Draft",
                Objectives = plan?.Targets,
                ActivitiesCompleted = done.Count > 0 ? string.Join("\n", done) : null,
                ActivitiesOutstanding = missed.Count > 0 ? string.Join("\n", missed) : null,
                PerformanceSummary = summary,
                Challenges = string.Join("\n", logs.Where(l => !string.IsNullOrWhiteSpace(l.Challenges)).Select(l => $"{l.TrainingDate:dd MMM}: {l.Challenges}")),
                Improvements = string.Join("\n", logs.Where(l => !string.IsNullOrWhiteSpace(l.Improvements)).Select(l => $"{l.TrainingDate:dd MMM}: {l.Improvements}")),
                AverageRating = rated.Count == 0 ? null : Math.Round(rated.Average(), 1), LogsInWeek = logs.Count,
            });
        }
    }

    public class SaveProgressReportHandler : IRequestHandler<SaveProgressReportCommand, ApiResponse<ProgressReportDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveProgressReportHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProgressReportDto>> Handle(SaveProgressReportCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var e = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == d.EnrollmentId, ct);
            if (e == null) return ApiResponse<ProgressReportDto>.Fail("Enrolment not found.");
            if (!TrainingSupport.CanAccess(request.Actor, e)) return ApiResponse<ProgressReportDto>.Fail("You can only report on dogs allocated to you.");
            if (e.StartDate == null) return ApiResponse<ProgressReportDto>.Fail("Training has not started yet.");
            if (d.WeekNumber < 1 || d.WeekNumber > TrainingSupport.TotalWeeks(e)) return ApiResponse<ProgressReportDto>.Fail("Invalid week.");

            var weekStart = e.StartDate.Value.Date.AddDays((d.WeekNumber - 1) * 7);
            if (weekStart > DateTime.Today) return ApiResponse<ProgressReportDto>.Fail("That week has not started yet.");
            if (d.Submit && string.IsNullOrWhiteSpace(d.PerformanceSummary))
                return ApiResponse<ProgressReportDto>.Fail("Add a performance summary before submitting the report.");

            var repo = _uow.Repository<DogTrainingProgressReport>();
            var r = await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.EnrollmentId == e.Id && x.WeekNumber == d.WeekNumber, ct);
            if (r != null && r.Status is "Submitted" or "Approved")
                return ApiResponse<ProgressReportDto>.Fail(r!.Status == "Approved" ? "This report has been approved and cannot be changed." : "This report is awaiting review and cannot be changed.");

            var isNew = r == null;
            r ??= new DogTrainingProgressReport { CompanyId = e.CompanyId, EnrollmentId = e.Id, WeekNumber = d.WeekNumber };

            r.HandlerUserId = request.Actor.UserId;
            r.Objectives = TrainingSupport.Trim(d.Objectives);
            r.ActivitiesCompleted = TrainingSupport.Trim(d.ActivitiesCompleted);
            r.ActivitiesOutstanding = TrainingSupport.Trim(d.ActivitiesOutstanding);
            r.PerformanceSummary = TrainingSupport.Trim(d.PerformanceSummary);
            r.Challenges = TrainingSupport.Trim(d.Challenges);
            r.Improvements = TrainingSupport.Trim(d.Improvements);
            r.Recommendations = TrainingSupport.Trim(d.Recommendations);
            r.Status = d.Submit ? "Submitted" : "Draft";
            if (d.Submit) { r.SubmittedOn = DateTime.UtcNow; r.ReviewComments = null; }

            if (isNew) await repo.AddAsync(r, ct); else await repo.UpdateAsync(r, ct);
            var fresh = await repo.TableNoTracking.FirstAsync(x => x.Id == r.Id, ct);
            return ApiResponse<ProgressReportDto>.Ok((await ReportMapper.MapAsync(_uow, new List<DogTrainingProgressReport> { fresh }, ct)).First(),
                d.Submit ? "Report submitted for review." : "Draft saved.");
        }
    }

    public class ReviewProgressReportHandler : IRequestHandler<ReviewProgressReportCommand, ApiResponse<ProgressReportDto>>
    {
        private readonly IUnitOfWork _uow;
        public ReviewProgressReportHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProgressReportDto>> Handle(ReviewProgressReportCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var r = await _uow.Repository<DogTrainingProgressReport>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.ReportId, ct);
            if (r == null) return ApiResponse<ProgressReportDto>.Fail("Report not found.");
            if (r.Status != "Submitted") return ApiResponse<ProgressReportDto>.Fail("Only a submitted report can be reviewed.");
            if (!d.Approve && string.IsNullOrWhiteSpace(d.Comments))
                return ApiResponse<ProgressReportDto>.Fail("Explain what needs to change when returning a report.");

            r.Status = d.Approve ? "Approved" : "Returned";
            r.ReviewedByUserId = request.Actor.UserId;
            r.ReviewedOn = DateTime.UtcNow;
            r.ReviewComments = TrainingSupport.Trim(d.Comments);
            await _uow.Repository<DogTrainingProgressReport>().UpdateAsync(r, ct);

            var fresh = await _uow.Repository<DogTrainingProgressReport>().TableNoTracking.FirstAsync(x => x.Id == r.Id, ct);
            return ApiResponse<ProgressReportDto>.Ok((await ReportMapper.MapAsync(_uow, new List<DogTrainingProgressReport> { fresh }, ct)).First(),
                d.Approve ? "Report approved." : "Report returned to the handler.");
        }
    }
}
