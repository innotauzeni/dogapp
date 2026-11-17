using Dog.Core.Entities;
using Dog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dog.Api.BackgroundJobs
{
    /// <summary>
    /// Promotes puppies to full registered dogs once they reach 6 weeks of age.
    /// Until then a puppy is tracked under its mother's litter; afterwards it becomes
    /// its own dog record with independent vaccine tracking.
    /// </summary>
    public class PuppyPromotionService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(12);
        private const int WeeksToRegister = 6;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PuppyPromotionService> _logger;

        public PuppyPromotionService(IServiceScopeFactory scopeFactory, ILogger<PuppyPromotionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Let the app finish booting & migrating before the first run.
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await PromoteDuePuppiesAsync(stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "Puppy promotion run failed."); }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task PromoteDuePuppiesAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow.Date.AddDays(-7 * WeeksToRegister);

            var duePuppies = await db.ka_puppies
                .Include(p => p.Litter).ThenInclude(l => l.MatingRecord).ThenInclude(m => m.Sire)
                .Include(p => p.Litter).ThenInclude(l => l.MatingRecord).ThenInclude(m => m.Dam)
                .Where(p => p.RegisteredDogId == null
                            && p.DateOfDeath == null
                            && p.DateOfBirth <= cutoff)
                .Take(200)
                .ToListAsync(ct);

            if (duePuppies.Count == 0) return;

            var promoted = 0;
            foreach (var pup in duePuppies)
            {
                try
                {
                    var mating = pup.Litter?.MatingRecord;
                    var breed = mating?.Sire?.Breed ?? mating?.Dam?.Breed ?? "Unknown";
                    var companyId = pup.CompanyId != 0
                        ? pup.CompanyId
                        : (pup.Litter?.CompanyId ?? mating?.CompanyId ?? 0);

                    var dog = new Dogtbl
                    {
                        Name = string.IsNullOrWhiteSpace(pup.Name) ? "Puppy" : pup.Name,
                        Breed = string.IsNullOrWhiteSpace(breed) ? "Unknown" : breed,
                        Color = string.IsNullOrWhiteSpace(pup.Color) ? "Unknown" : pup.Color,
                        DateOfBirth = pup.DateOfBirth,
                        Gender = pup.Gender,
                        SireId = mating?.SireId,
                        DamId = mating?.DamId,
                        CompanyId = companyId,
                        IsActive = true,
                    };

                    db.ka_dogs.Add(dog);
                    await db.SaveChangesAsync(ct); // materialise dog.Id

                    pup.RegisteredDogId = dog.Id;
                    db.ka_puppies.Update(pup);
                    await db.SaveChangesAsync(ct);

                    // Re-link any vaccines recorded under this puppy to the new dog.
                    var puppyVaccines = await db.ka_vaccinationrecords
                        .Where(v => v.PuppyId == pup.Id)
                        .ToListAsync(ct);
                    foreach (var vac in puppyVaccines)
                    {
                        vac.DogId = dog.Id;
                        vac.PuppyId = null;
                    }
                    if (puppyVaccines.Count > 0)
                        await db.SaveChangesAsync(ct);

                    promoted++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to promote puppy {PuppyId}.", pup.Id);
                }
            }

            if (promoted > 0)
                _logger.LogInformation("Promoted {Count} puppies to registered dogs.", promoted);
        }
    }
}
