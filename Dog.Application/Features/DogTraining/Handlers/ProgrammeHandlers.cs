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
    internal static class ProgrammeMapper
    {
        public static ProgrammeDto Map(DogTrainingProgramme p, IEnumerable<DogTrainingProgrammeWeek> weeks,
            IEnumerable<DogTrainingProgrammeActivity> acts, int enrolments) => new()
        {
            Id = p.Id, CompanyId = p.CompanyId, Name = p.Name, Description = p.Description,
            DurationDays = p.DurationDays, Fee = p.Fee, IsActive = p.IsActive, EnrolmentCount = enrolments,
            Weeks = weeks.OrderBy(w => w.WeekNumber).Select(w => new ProgrammeWeekDto
                { WeekNumber = w.WeekNumber, Title = w.Title, Focus = w.Focus, Targets = w.Targets }).ToList(),
            Activities = acts.OrderBy(a => a.WeekNumber).ThenBy(a => a.SortOrder).ThenBy(a => a.TimeOfDay).Select(a => new ProgrammeActivityDto
                { WeekNumber = a.WeekNumber, TimeOfDay = a.TimeOfDay, Title = a.Title, Purpose = a.Purpose, SortOrder = a.SortOrder }).ToList(),
        };

        public static async Task<ProgrammeDto?> LoadAsync(IUnitOfWork uow, int id, CancellationToken ct)
        {
            var p = await uow.Repository<DogTrainingProgramme>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p == null) return null;
            var weeks = await uow.Repository<DogTrainingProgrammeWeek>().TableNoTracking.Where(w => w.ProgrammeId == id).ToListAsync(ct);
            var acts = await uow.Repository<DogTrainingProgrammeActivity>().TableNoTracking.Where(a => a.ProgrammeId == id).ToListAsync(ct);
            var count = await uow.Repository<DogTrainingEnrollment>().TableNoTracking.CountAsync(e => e.ProgrammeId == id, ct);
            return Map(p, weeks, acts, count);
        }
    }

    public class GetProgrammesHandler : IRequestHandler<GetProgrammesQuery, ApiResponse<List<ProgrammeDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetProgrammesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ProgrammeDto>>> Handle(GetProgrammesQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.CompanyId, ct);
            var q = _uow.Repository<DogTrainingProgramme>().TableNoTracking.Where(p => companyIds.Contains(p.CompanyId));
            if (request.ActiveOnly) q = q.Where(p => p.IsActive);
            var programmes = await q.OrderBy(p => p.Name).ToListAsync(ct);
            var ids = programmes.Select(p => p.Id).ToList();

            var weeks = await _uow.Repository<DogTrainingProgrammeWeek>().TableNoTracking.Where(w => ids.Contains(w.ProgrammeId)).ToListAsync(ct);
            var acts = await _uow.Repository<DogTrainingProgrammeActivity>().TableNoTracking.Where(a => ids.Contains(a.ProgrammeId)).ToListAsync(ct);
            var counts = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => ids.Contains(e.ProgrammeId))
                .GroupBy(e => e.ProgrammeId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

            var result = programmes.Select(p => ProgrammeMapper.Map(p,
                weeks.Where(w => w.ProgrammeId == p.Id), acts.Where(a => a.ProgrammeId == p.Id),
                counts.TryGetValue(p.Id, out var n) ? n : 0)).ToList();
            return ApiResponse<List<ProgrammeDto>>.Ok(result);
        }
    }

    public class SaveProgrammeHandler : IRequestHandler<SaveProgrammeCommand, ApiResponse<ProgrammeDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveProgrammeHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProgrammeDto>> Handle(SaveProgrammeCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (string.IsNullOrWhiteSpace(d.Name)) return ApiResponse<ProgrammeDto>.Fail("Programme name is required.");
            if (d.DurationDays < 1 || d.DurationDays > 365) return ApiResponse<ProgrammeDto>.Fail("Duration must be between 1 and 365 days.");
            if (d.Fee < 0) return ApiResponse<ProgrammeDto>.Fail("Fee cannot be negative.");

            var repo = _uow.Repository<DogTrainingProgramme>();
            DogTrainingProgramme programme;

            if (d.Id.HasValue && d.Id.Value > 0)
            {
                var existing = await repo.Table.AsTracking().FirstOrDefaultAsync(p => p.Id == d.Id.Value, ct);
                if (existing == null) return ApiResponse<ProgrammeDto>.Fail("Programme not found.");
                existing.Name = d.Name.Trim();
                existing.Description = TrainingSupport.Trim(d.Description);
                existing.DurationDays = d.DurationDays;
                existing.Fee = d.Fee;
                existing.IsActive = d.IsActive;
                await repo.UpdateAsync(existing, ct);
                programme = existing;

                // Weeks and daily schedule are replaced as a set. Running enrolments are unaffected —
                // they hold their own copy of the plan, taken when training was activated.
                var oldWeeks = await _uow.Repository<DogTrainingProgrammeWeek>().Table.AsTracking().Where(w => w.ProgrammeId == programme.Id).ToListAsync(ct);
                if (oldWeeks.Count > 0) await _uow.Repository<DogTrainingProgrammeWeek>().DeleteRangeAsync(oldWeeks, ct);
                var oldActs = await _uow.Repository<DogTrainingProgrammeActivity>().Table.AsTracking().Where(a => a.ProgrammeId == programme.Id).ToListAsync(ct);
                if (oldActs.Count > 0) await _uow.Repository<DogTrainingProgrammeActivity>().DeleteRangeAsync(oldActs, ct);
            }
            else
            {
                programme = new DogTrainingProgramme
                {
                    CompanyId = d.CompanyId,
                    Name = d.Name.Trim(),
                    Description = TrainingSupport.Trim(d.Description),
                    DurationDays = d.DurationDays,
                    Fee = d.Fee,
                    IsActive = d.IsActive,
                };
                await repo.AddAsync(programme, ct);
            }

            var weeks = d.Weeks.Where(w => w.WeekNumber > 0 && !string.IsNullOrWhiteSpace(w.Title))
                .GroupBy(w => w.WeekNumber).Select(g => g.First())
                .Select(w => new DogTrainingProgrammeWeek
                {
                    CompanyId = programme.CompanyId, ProgrammeId = programme.Id, WeekNumber = w.WeekNumber,
                    Title = w.Title.Trim(), Focus = TrainingSupport.Trim(w.Focus), Targets = TrainingSupport.Trim(w.Targets)
                }).ToList();
            if (weeks.Count > 0) await _uow.Repository<DogTrainingProgrammeWeek>().AddRangeAsync(weeks, ct);

            var order = 0;
            var acts = d.Activities.Where(a => !string.IsNullOrWhiteSpace(a.Title))
                .Select(a => new DogTrainingProgrammeActivity
                {
                    CompanyId = programme.CompanyId, ProgrammeId = programme.Id, WeekNumber = Math.Max(0, a.WeekNumber),
                    TimeOfDay = (a.TimeOfDay ?? "").Trim(), Title = a.Title.Trim(), Purpose = TrainingSupport.Trim(a.Purpose),
                    SortOrder = a.SortOrder > 0 ? a.SortOrder : ++order
                }).ToList();
            if (acts.Count > 0) await _uow.Repository<DogTrainingProgrammeActivity>().AddRangeAsync(acts, ct);

            var dto = await ProgrammeMapper.LoadAsync(_uow, programme.Id, ct);
            return ApiResponse<ProgrammeDto>.Ok(dto!, "Programme saved.");
        }
    }

    public class DeleteProgrammeHandler : IRequestHandler<DeleteProgrammeCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteProgrammeHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteProgrammeCommand request, CancellationToken ct)
        {
            var programme = await _uow.Repository<DogTrainingProgramme>().Table.AsTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
            if (programme == null) return ApiResponse<bool>.Fail("Programme not found.");
            var used = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.CountAsync(e => e.ProgrammeId == request.Id, ct);
            if (used > 0)
                return ApiResponse<bool>.Fail($"This programme is used by {used} enrolment(s) and cannot be deleted. Deactivate it instead.");
            await _uow.Repository<DogTrainingProgramme>().DeleteAsync(programme, ct);
            return ApiResponse<bool>.Ok(true, "Programme deleted.");
        }
    }

    public class SeedDefaultProgrammeHandler : IRequestHandler<SeedDefaultProgrammeCommand, ApiResponse<ProgrammeDto>>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;
        public SeedDefaultProgrammeHandler(IMediator mediator, IUnitOfWork uow) { _mediator = mediator; _uow = uow; }

        public async Task<ApiResponse<ProgrammeDto>> Handle(SeedDefaultProgrammeCommand request, CancellationToken ct)
        {
            var dto = DefaultProgramme.Create(request.CompanyId);
            var exists = await _uow.Repository<DogTrainingProgramme>().TableNoTracking
                .AnyAsync(p => p.CompanyId == request.CompanyId && p.Name == dto.Name, ct);
            if (exists) return ApiResponse<ProgrammeDto>.Fail("The default 21-day programme already exists for this company.");
            return await _mediator.Send(new SaveProgrammeCommand(dto), ct);
        }
    }
}
