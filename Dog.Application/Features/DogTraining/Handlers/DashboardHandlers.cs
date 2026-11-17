using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.DogTraining.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.DogTraining.Handlers
{
    public class GetTrainingDashboardHandler : IRequestHandler<GetTrainingDashboardQuery, ApiResponse<TrainingDashboardDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingDashboardHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingDashboardDto>> Handle(GetTrainingDashboardQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            await TrainingSupport.SyncPaymentAsync(_uow, null, companyIds, ct);
            await TrainingSupport.SyncProgressAsync(_uow, companyIds, ct);

            var q = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);
            if (!string.IsNullOrWhiteSpace(request.HandlerUserId)) q = q.Where(e => e.HandlerUserId == request.HandlerUserId);
            if (request.ProgrammeId.HasValue) q = q.Where(e => e.ProgrammeId == request.ProgrammeId.Value);
            if (request.From.HasValue) { var f = request.From.Value.Date; q = q.Where(e => e.CreatedAt >= f); }
            if (request.To.HasValue) { var t = request.To.Value.Date.AddDays(1); q = q.Where(e => e.CreatedAt < t); }

            var all = await q.ToListAsync(ct);
            var ids = all.Select(e => e.Id).ToList();

            // Latest final assessment per enrolment decides "requiring additional training".
            var latestFinal = (await _uow.Repository<DogTrainingFinalAssessment>().TableNoTracking
                    .Where(a => ids.Contains(a.EnrollmentId)).Select(a => new { a.EnrollmentId, a.AttemptNumber, a.Result }).ToListAsync(ct))
                .GroupBy(a => a.EnrollmentId).ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AttemptNumber).First().Result);

            var inTraining = all.Where(e => e.Status == TrainingStatus.InTraining).ToList();
            var weeks = inTraining.Select(e => TrainingSupport.WeekAndDay(e, DateTime.Today).Week).ToList();

            var certQ = _uow.Repository<DogTrainingCertificate>().TableNoTracking.Where(c => ids.Contains(c.EnrollmentId) && !c.IsRevoked);
            var reviewQ = _uow.Repository<DogTrainingProgressReport>().TableNoTracking.Where(r => ids.Contains(r.EnrollmentId) && r.Status == "Submitted");

            var dto = new TrainingDashboardDto
            {
                AwaitingHealthAssessment = all.Count(e => e.Status == TrainingStatus.AwaitingHealthAssessment),
                HealthPassed = all.Count(e => e.Status == TrainingStatus.HealthPassed),
                HealthFailed = all.Count(e => e.Status == TrainingStatus.HealthFailed),
                AwaitingPayment = all.Count(e => e.Status == TrainingStatus.AwaitingPayment),
                AwaitingHandler = all.Count(e => e.Status == TrainingStatus.AwaitingHandler),
                ReadyToActivate = all.Count(e => e.Status == TrainingStatus.Ready),
                InTraining = inTraining.Count,
                Week1 = weeks.Count(w => w == 1),
                Week2 = weeks.Count(w => w == 2),
                Week3 = weeks.Count(w => w == 3),
                AwaitingFinalAssessment = all.Count(e => e.Status == TrainingStatus.AwaitingFinalAssessment),
                AwaitingApproval = all.Count(e => e.Status == TrainingStatus.AwaitingApproval),
                RequiringAdditionalTraining = all.Count(e => e.Status != TrainingStatus.Completed && e.Status != TrainingStatus.Cancelled &&
                    latestFinal.TryGetValue(e.Id, out var r) && (r == "Failed" || r == "RequiresAdditionalTraining")),
                Completed = all.Count(e => e.Status == TrainingStatus.Completed),
                Cancelled = all.Count(e => e.Status == TrainingStatus.Cancelled),
                CertificatesIssued = await certQ.CountAsync(ct),
                ReportsAwaitingReview = await reviewQ.CountAsync(ct),
            };

            var recent = all.OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt).Take(10).ToList();
            dto.Recent = await TrainingSupport.BuildListAsync(_uow, recent, ct);
            return ApiResponse<TrainingDashboardDto>.Ok(dto);
        }
    }

    public class GetTrainingReportHandler : IRequestHandler<GetTrainingReportQuery, ApiResponse<TrainingReportDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingReportHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingReportDto>> Handle(GetTrainingReportQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var q = TrainingSupport.ScopeToActor(
                _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId)), request.Actor);
            if (request.From.HasValue) { var f = request.From.Value.Date; q = q.Where(e => e.CreatedAt >= f); }
            if (request.To.HasValue) { var t = request.To.Value.Date.AddDays(1); q = q.Where(e => e.CreatedAt < t); }
            var all = await q.ToListAsync(ct);
            var ids = all.Select(e => e.Id).ToList();

            var assessments = await _uow.Repository<DogTrainingFinalAssessment>().TableNoTracking
                .Where(a => ids.Contains(a.EnrollmentId)).Select(a => new { a.EnrollmentId, a.Result, a.OverallScore }).ToListAsync(ct);
            var invoiceIds = all.Where(e => e.InvoiceId != null).Select(e => e.InvoiceId!.Value).ToList();
            var paid = invoiceIds.Count == 0 ? new Dictionary<int, decimal>() :
                await _uow.Repository<DogTrainingInvoice>().TableNoTracking.Where(i => invoiceIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.PaidAmount, ct);
            var programmes = await _uow.Repository<DogTrainingProgramme>().TableNoTracking
                .Where(p => companyIds.Contains(p.CompanyId)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
            var handlerIds = all.Where(e => e.HandlerUserId != null).Select(e => e.HandlerUserId!).Distinct().ToList();
            var handlers = handlerIds.Count == 0 ? new Dictionary<string, string>() :
                (await _uow.Repository<ApplicationUser>().TableNoTracking.Where(u => handlerIds.Contains(u.Id)).ToListAsync(ct))
                    .ToDictionary(u => u.Id, u => TrainingSupport.FullName(u));

            decimal Revenue(IEnumerable<DogTrainingEnrollment> list) => list.Where(e => e.InvoiceId != null && paid.ContainsKey(e.InvoiceId.Value)).Sum(e => paid[e.InvoiceId!.Value]);
            decimal? AvgScore(IEnumerable<DogTrainingEnrollment> list)
            {
                var set = list.Select(e => e.Id).ToHashSet();
                var scores = assessments.Where(a => set.Contains(a.EnrollmentId) && a.OverallScore.HasValue).Select(a => a.OverallScore!.Value).ToList();
                return scores.Count == 0 ? null : Math.Round(scores.Average(), 1);
            }
            TrainingReportRowDto Row(string label, List<DogTrainingEnrollment> list) => new()
            {
                Label = label, Enrolments = list.Count, Completed = list.Count(e => e.Status == TrainingStatus.Completed),
                InTraining = list.Count(e => e.Status == TrainingStatus.InTraining), AverageScore = AvgScore(list), Revenue = Revenue(list)
            };

            var report = new TrainingReportDto
            {
                TotalEnrolments = all.Count,
                TotalCompleted = all.Count(e => e.Status == TrainingStatus.Completed),
                TotalRevenue = Revenue(all),
                TotalFailedAssessments = assessments.Count(a => a.Result != "Passed"),
                PassRatePercent = assessments.Count == 0 ? 0 : Math.Round(assessments.Count(a => a.Result == "Passed") * 100m / assessments.Count, 1),
                ByProgramme = all.GroupBy(e => e.ProgrammeId).Select(g => Row(programmes.TryGetValue(g.Key, out var n) ? n : "Programme " + g.Key, g.ToList()))
                    .OrderByDescending(r => r.Enrolments).ToList(),
                ByHandler = all.Where(e => e.HandlerUserId != null).GroupBy(e => e.HandlerUserId!)
                    .Select(g => Row(handlers.TryGetValue(g.Key, out var n) ? n : "Handler", g.ToList())).OrderByDescending(r => r.Enrolments).ToList(),
                ByMonth = all.GroupBy(e => new DateTime(e.CreatedAt.Year, e.CreatedAt.Month, 1)).OrderBy(g => g.Key)
                    .Select(g => Row(g.Key.ToString("MMM yyyy"), g.ToList())).ToList(),
            };
            return ApiResponse<TrainingReportDto>.Ok(report);
        }
    }
}
