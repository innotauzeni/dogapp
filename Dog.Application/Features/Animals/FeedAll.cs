using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dog.Application.Features.Animals.FeedAll
{
    public class FeedAllDogsDto
    {
        public int CompanyId { get; set; }
        /// <summary>What was given, e.g. "Standard portion" or "2 cups".</summary>
        public string? Portion { get; set; }
        public string? Details { get; set; }
        public string? Location { get; set; }
        /// <summary>Dogs out on a deployment are fed by the client; include them only when asked.</summary>
        public bool IncludeDeployed { get; set; }
        /// <summary>When it was fed (the user's own clock). Defaults to now.</summary>
        public DateTime? OccurredOn { get; set; }
        public string? RecordedByUserId { get; set; }
    }

    public class FeedAllResultDto
    {
        public int TotalDogs { get; set; }
        public int Fed { get; set; }
        public List<string> FedNames { get; set; } = new();
        public List<string> AlreadyFed { get; set; } = new();
        public List<string> Deployed { get; set; } = new();
        public string Portion { get; set; } = "Standard portion";
        public bool NotifyingWhatsApp { get; set; }
    }

    public record FeedAllDogsCommand(FeedAllDogsDto Dto) : IRequest<ApiResponse<FeedAllResultDto>>;

    /// <summary>
    /// "Everyone was fed at once": records a Feeding care record for every dog in the kennel in one go - so a handler who has no
    /// dogs allocated (or who fed the whole kennel) does not have to enter them one by one. Dogs already fed today are left alone,
    /// and a WhatsApp notification goes out for every dog (in the background, one after the other) plus one summary.
    /// </summary>
    public class FeedAllDogsHandler : IRequestHandler<FeedAllDogsCommand, ApiResponse<FeedAllResultDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _config;
        public FeedAllDogsHandler(IUnitOfWork uow, IServiceScopeFactory scopes, IConfiguration config) { _uow = uow; _scopes = scopes; _config = config; }

        public async Task<ApiResponse<FeedAllResultDto>> Handle(FeedAllDogsCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var when = d.OccurredOn ?? DateTime.Now;
            if (when.Date > DateTime.Now.Date.AddDays(1)) return ApiResponse<FeedAllResultDto>.Fail("You cannot record feeding for a future date.");
            var portion = string.IsNullOrWhiteSpace(d.Portion) ? "Standard portion" : d.Portion.Trim();
            if (portion.Length > 100) return ApiResponse<FeedAllResultDto>.Fail("The portion description is too long.");

            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, d.CompanyId, ct);
            var dogs = await _uow.Repository<Dogtbl>().TableNoTracking
                .Where(x => companyIds.Contains(x.CompanyId) && x.IsActive && x.DateOfDeath == null)
                .OrderBy(x => x.Name).ToListAsync(ct);
            if (dogs.Count == 0) return ApiResponse<FeedAllResultDto>.Fail("There are no active dogs to feed.");

            var result = new FeedAllResultDto { TotalDogs = dogs.Count, Portion = portion };

            // Out on deployment: not in the kennel.
            if (!d.IncludeDeployed)
            {
                var today = when.Date;
                var ids = dogs.Select(x => x.Id).ToList();
                var away = (await _uow.Repository<Deployment>().TableNoTracking
                        .Where(x => ids.Contains(x.DogId) && x.IsActive && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today))
                        .Select(x => x.DogId).ToListAsync(ct)).ToHashSet();
                result.Deployed = dogs.Where(x => away.Contains(x.Id)).Select(x => x.Name).ToList();
                dogs = dogs.Where(x => !away.Contains(x.Id)).ToList();
            }

            // A dog is fed once a day: skip the ones that already have today's feeding.
            var day = when.Date; var nextDay = day.AddDays(1);
            var dogIds = dogs.Select(x => x.Id).ToList();
            var fedToday = (await _uow.Repository<DogCareRecord>().TableNoTracking
                    .Where(r => dogIds.Contains(r.DogId) && r.EventType == "Feeding" && r.OccurredOn >= day && r.OccurredOn < nextDay)
                    .Select(r => r.DogId).ToListAsync(ct)).ToHashSet();

            var details = string.IsNullOrWhiteSpace(d.Details) ? "Whole kennel fed together." : d.Details!.Trim();
            var fed = new List<(Dogtbl Dog, DogCareRecord Care)>();
            foreach (var dog in dogs)
            {
                if (fedToday.Contains(dog.Id)) { result.AlreadyFed.Add(dog.Name); continue; }
                var care = new DogCareRecord
                {
                    DogId = dog.Id, CompanyId = dog.CompanyId, EventType = "Feeding", OccurredOn = when, QuantityOrOutcome = portion,
                    Details = details, RecordedByUserId = d.RecordedByUserId, RequiresAttention = false,
                };
                await _uow.Repository<DogCareRecord>().AddAsync(care, ct);
                fed.Add((dog, care));
                result.FedNames.Add(dog.Name);
            }
            result.Fed = fed.Count;

            var handlerName = "System";
            if (!string.IsNullOrWhiteSpace(d.RecordedByUserId))
            {
                var u = await _uow.Repository<ApplicationUser>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == d.RecordedByUserId, ct);
                if (u != null)
                {
                    var full = $"{u.Name} {u.Surname}".Trim();
                    handlerName = full.Length > 0 ? full : (u.UserName ?? "Handler");
                }
            }

            // WhatsApp: one message per dog, one after the other, in the background so the handler is not kept waiting.
            if (fed.Count > 0 && !string.IsNullOrWhiteSpace(_config["NotifyWhatsapp:WhatsAppNumbers"]))
            {
                result.NotifyingWhatsApp = true;
                var perDog = !string.Equals(_config["NotifyWhatsapp:FeedAllPerDog"], "false", StringComparison.OrdinalIgnoreCase);
                var batch = fed.ToList(); var skipped = result.AlreadyFed.Count; var away = result.Deployed.Count;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var notify = scope.ServiceProvider.GetRequiredService<IWhatsAppNotificationService>();
                        if (perDog)
                            foreach (var (dog, care) in batch)
                            {
                                try { await notify.NotifyDogFedAsync(dog, care, handlerName); } catch { /* one failed message must not stop the rest */ }
                                await Task.Delay(1200);
                            }
                        await notify.NotifyAllDogsFedAsync(batch.Count, portion, handlerName, when, batch.Select(b => b.Dog.Name).ToList(), skipped, away);
                    }
                    catch { /* notifications are best effort */ }
                });
            }

            return ApiResponse<FeedAllResultDto>.Ok(result,
                result.Fed == 0 ? "Every dog already has today's feeding record." : $"{result.Fed} dog(s) recorded as fed ({portion}).");
        }
    }
}
