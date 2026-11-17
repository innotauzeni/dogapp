using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Animals.Commands;
using Dog.Application.Features.Animals.DTOs;
using Dog.Application.Features.Animals.Queries;
using Dog.Application.Features.Deployments.DTOs;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace Dog.Application.Features.Animals.Handlers
{
    public class GetAnimalsByCompanyHandler : IRequestHandler<GetAnimalsByCompanyQuery, ApiResponse<List<AnimalListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetAnimalsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<AnimalListDto>>> Handle(GetAnimalsByCompanyQuery request, CancellationToken ct)
        {
            // A head-office user's default (no branch switched) view sees itself plus
            // every branch; a branch only ever sees itself.
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);

            // ── 1. Dogs that directly belong to this company (or its branches) ─
            var ownedList = await _uow.Repository<Dogtbl>()
                .TableNoTracking
                .Where(p => companyIds.Contains(p.CompanyId))
                .Include(q => q.Sire)
                .Include(q => q.Dam)
                .Include(q => q.Company)
                .Include(q => q.Customer)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            // ── 2. Dogs assigned to this company/branch via DogAssignment ─────
            //    Match on BranchId == companyId  OR  handler belongs to this company
            var ownedIds = ownedList.Select(d => d.Id).ToHashSet();

            // Get user IDs that belong to this company (for handler-based matching)
            var usersInCompany = await _uow.Repository<ApplicationUser>()
                .TableNoTracking
                .Where(u => companyIds.Contains(u.CompanyId))
                .Select(u => u.Id)
                .ToListAsync(ct);

            var assignedDogIds = await _uow.Repository<DogAssignment>()
                .TableNoTracking
                .Where(a => a.EndedOn == null &&
                    ((a.BranchId != null && companyIds.Contains(a.BranchId.Value)) ||
                     (a.HandlerUserId != null && usersInCompany.Contains(a.HandlerUserId))))
                .Select(a => a.DogId)
                .Distinct()
                .ToListAsync(ct);

            var assignedList = new List<Dogtbl>();
            if (assignedDogIds.Count > 0)
            {
                assignedList = await _uow.Repository<Dogtbl>()
                    .TableNoTracking
                    .Where(p => assignedDogIds.Contains(p.Id) && !ownedIds.Contains(p.Id))
                    .Include(q => q.Sire)
                    .Include(q => q.Dam)
                    .Include(q => q.Company)
                    .Include(q => q.Customer)
                    .ToListAsync(ct);
            }

            var combinedList = ownedList.Concat(assignedList).ToList();

            // ── 3. Active deployments for status badge ────────────────────────
            var allDogIds = combinedList.Select(d => d.Id).ToList();
            var deployments = await _uow.Repository<Deployment>()
                .TableNoTracking
                .Where(d => allDogIds.Contains(d.DogId) && d.IsActive)
                .ToListAsync(ct);

            // ── 4. Active assignments for the assigned-to-branch flag ─────────
            var activeAssignments = assignedDogIds.Count > 0
                ? await _uow.Repository<DogAssignment>()
                    .TableNoTracking
                    .Where(a => assignedDogIds.Contains(a.DogId) && a.EndedOn == null)
                    .Include(a => a.HandlerUser)
                    .ToListAsync(ct)
                : new List<DogAssignment>();

            var dtos = combinedList.Select(d =>
            {
                var dto = AnimalMapper.MapAnimalList(d);

                // Deployment badge
                var dep = deployments.FirstOrDefault(dep => dep.DogId == d.Id);
                if (dep != null)
                {
                    dto.IsDeployed = true;
                    dto.DeployedLocation = dep.Location;
                    dto.DeployedTo = dep.CompanyDeployedTo;
                }

                // Mark dogs that are here via branch assignment (not owned)
                if (!ownedIds.Contains(d.Id))
                {
                    dto.IsAssignedToBranch = true;
                    var asgn = activeAssignments.FirstOrDefault(a => a.DogId == d.Id);
                    dto.AssignedHandlerName = asgn?.HandlerUser != null
                        ? $"{asgn.HandlerUser.Name} {asgn.HandlerUser.Surname}".Trim()
                        : null;
                }

                return dto;
            }).ToList();

            return ApiResponse<List<AnimalListDto>>.Ok(dtos);
        }
    }

    public class CreateAnimalHandler : IRequestHandler<CreateAnimalCommand, ApiResponse<AnimalDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IWhatsAppNotificationService _whatsAppNotify;
        private readonly IConfiguration _config;
        
        public CreateAnimalHandler(IUnitOfWork uow, IWhatsAppNotificationService whatsAppNotify, IConfiguration config)
        {
            _uow = uow;
            _whatsAppNotify = whatsAppNotify;
            _config = config;
        }

        public async Task<ApiResponse<AnimalDto>> Handle(CreateAnimalCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var exists = await _uow.Repository<Dogtbl>()
                .TableNoTracking
                .AnyAsync(p => p.CompanyId == d.CompanyId && p.Name == d.Name, ct);
            if (exists) return ApiResponse<AnimalDto>.Fail($"Dog '{d.Name}' already exists.");
           
            var entity = new Dogtbl
            {
                Name = d.Name,
                Breed = d.Breed,
                Color = d.Color,
                DateOfBirth = d.DateOfBirth,
                DateOfDeath = d.DateOfDeath,
                Gender = d.Gender,
                MicrochipNumber = d.MicrochipNumber,
                PhotoUrl = d.PhotoUrl,
                SireId = d.SireId,
                DamId = d.DamId,
                CustomerId = d.CustomerId,
                IsActive = d.IsActive ?? true, // New dogs are active by default
                CompanyId = d.CompanyId,
            };
            await _uow.Repository<Dogtbl>().AddAsync(entity, ct);
            
            // Get company name for notification
            var company = await _uow.Repository<Company>()
                .TableNoTracking
                .FirstOrDefaultAsync(c => c.Id == d.CompanyId, ct);
            var companyName = company?.CompanyName ?? "Unknown Company";
            
            // Send WhatsApp notification (after save completes)
            try
            {
                // Get notification numbers from config
                var numbersConfig = _config["NotifyWhatsapp:WhatsAppNumbers"] ?? "";
                if (!string.IsNullOrWhiteSpace(numbersConfig))
                {
                    await _whatsAppNotify.NotifyDogAddedAsync(entity, companyName);
                }
            }
            catch (Exception)
            {
                // Silently fail - don't block the main operation
            }
            
            return ApiResponse<AnimalDto>.Ok(AnimalMapper.MapAnimal(entity), "Dog created.");
        }
    }

    public class UpdateAnimalHandler : IRequestHandler<UpdateAnimalCommand, ApiResponse<AnimalDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateAnimalHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<AnimalDto>> Handle(UpdateAnimalCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Dogtbl>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, ct);
            if (entity == null) return ApiResponse<AnimalDto>.Fail("Dog not found.");

             var wasDeceased = entity.DateOfDeath.HasValue;
             entity.Name = request.Dto.Name;
             entity.Breed = request.Dto.Breed;
             entity.Color = request.Dto.Color;
             entity.DateOfBirth = request.Dto.DateOfBirth;
             entity.DateOfDeath = request.Dto.DateOfDeath;
             entity.Gender = request.Dto.Gender;
             entity.MicrochipNumber = request.Dto.MicrochipNumber;
             entity.PhotoUrl = request.Dto.PhotoUrl;
             entity.SireId = request.Dto.SireId;
             entity.DamId = request.Dto.DamId;
             entity.CustomerId = request.Dto.CustomerId;
             if (request.Dto.IsActive.HasValue)
             {
                 entity.IsActive = request.Dto.IsActive.Value;
             }
            
            await repo.UpdateAsync(entity, ct);

            // If this dog was a puppy promoted from a litter, keep the litter's counts in step:
            // a death deducts one from Survived and adds one to Died (and the reverse if undone).
            var isDeceased = entity.DateOfDeath.HasValue;
            if (isDeceased != wasDeceased)
            {
                try
                {
                    var puppy = await _uow.Repository<Puppy>().Table
                        .FirstOrDefaultAsync(p => p.RegisteredDogId == entity.Id, ct);
                    if (puppy != null)
                    {
                        var litter = await _uow.Repository<Litter>().GetByIdAsync(puppy.LitterId, ct);
                        puppy.DateOfDeath = entity.DateOfDeath;
                        await _uow.Repository<Puppy>().UpdateAsync(puppy, ct);
                        if (litter != null)
                        {
                            if (isDeceased)
                            {
                                litter.SurvivedCount = Math.Max(0, litter.SurvivedCount - 1);
                                litter.DiedCount = Math.Min(litter.TotalBorn, litter.DiedCount + 1);
                            }
                            else
                            {
                                litter.DiedCount = Math.Max(0, litter.DiedCount - 1);
                                litter.SurvivedCount = Math.Min(litter.TotalBorn, litter.SurvivedCount + 1);
                            }
                            await _uow.Repository<Litter>().UpdateAsync(litter, ct);
                        }
                    }
                }
                catch (Exception)
                {
                    // Don't block the dog update if the litter sync fails
                }
            }

            return ApiResponse<AnimalDto>.Ok(AnimalMapper.MapAnimal(entity), "Dog updated.");
        }
    }

    public class DeleteAnimalHandler : IRequestHandler<DeleteAnimalCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteAnimalHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteAnimalCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Dogtbl>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Dog not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Dog deleted.");
        }
    }

    public class ToggleAnimalHandler : IRequestHandler<ToggleAnimalCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public ToggleAnimalHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(ToggleAnimalCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Dogtbl>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Dog not found.");
            entity.Name = entity.Name;
            entity.Breed = entity.Breed;
            entity.Color = entity.Color;
            entity.DateOfBirth = entity.DateOfBirth;
            entity.DateOfDeath = entity.DateOfDeath;
            entity.Gender = entity.Gender;
            entity.MicrochipNumber = entity.MicrochipNumber;
            entity.PhotoUrl = entity.PhotoUrl;
            entity.SireId = entity.SireId;
            entity.DamId = entity.DamId;
            entity.CompanyId = entity.CompanyId;
            entity.IsActive = !entity.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, $"Dog {(entity.IsActive ? "activated" : "deactivated")}.");
        }
    }

    public class AssignAnimalCustomerHandler : IRequestHandler<AssignAnimalCustomerCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AssignAnimalCustomerHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AssignAnimalCustomerCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Dogtbl>();
            var dog = await repo.GetByIdAsync(request.DogId, ct);
            if (dog == null) return ApiResponse<bool>.Fail("Dog not found.");

            if (request.CustomerId.HasValue)
            {
                var customer = await _uow.Repository<Customer>()
                    .TableNoTracking
                    .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && c.CompanyId == dog.CompanyId, ct);
                if (customer == null) return ApiResponse<bool>.Fail("Customer not found for this company.");
            }

            dog.CustomerId = request.CustomerId;
            await repo.UpdateAsync(dog, ct);
            return ApiResponse<bool>.Ok(true, request.CustomerId.HasValue ? "Dog assigned to customer." : "Dog unassigned.");
        }
    }

    public class GetDogProfileHandler : IRequestHandler<GetDogProfileQuery, ApiResponse<DogProfileDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetDogProfileHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<DogProfileDto>> Handle(GetDogProfileQuery request, CancellationToken ct)
        {
            var dog = await _uow.Repository<Dogtbl>()
                .TableNoTracking
                .Include(d => d.Sire)
                .Include(d => d.Dam)
                .Include(d => d.Company)
                .Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.Id == request.DogId, ct);

            if (dog == null) return ApiResponse<DogProfileDto>.Fail("Dog not found.");

            var matings = await _uow.Repository<MatingRecord>()
                .TableNoTracking
                .Where(m => (m.SireId == request.DogId || m.DamId == request.DogId) && m.CompanyId == dog.CompanyId)
                .Include(m => m.Sire)
                .Include(m => m.Dam)
                .Include(m => m.Litters)
                    .ThenInclude(l => l.Puppies)
                .OrderByDescending(m => m.MatingDate)
                .ToListAsync(ct);

            var vaccinations = await _uow.Repository<VaccinationRecord>()
                .TableNoTracking
                .Where(v => v.DogId == request.DogId && v.CompanyId == dog.CompanyId)
                .OrderByDescending(v => v.VaccinationDate)
                .ToListAsync(ct);

            var assignment = await _uow.Repository<DogAssignment>()
                .TableNoTracking
                .Include(a => a.HandlerUser)
                .Include(a => a.Branch)
                .Where(a => a.DogId == request.DogId && a.EndedOn == null)
                .OrderByDescending(a => a.AssignedOn)
                .FirstOrDefaultAsync(ct);

            var careRecords = await _uow.Repository<DogCareRecord>()
                .TableNoTracking
                .Include(r => r.RecordedByUser)
                .Where(r => r.DogId == request.DogId && r.CompanyId == dog.CompanyId)
                .OrderByDescending(r => r.OccurredOn)
                .Take(50)
                .ToListAsync(ct);

            // Who/where this dog is currently deployed, if at all — the most recent
            // deployment with no end date (or a still-future one) counts as "current".
            var deployment = await _uow.Repository<Deployment>()
                .TableNoTracking
                .Include(dp => dp.Customer)
                .Include(dp => dp.CustomerSite)
                .Where(dp => dp.DogId == request.DogId && dp.IsActive && (dp.EndDate == null || dp.EndDate > DateTime.UtcNow))
                .OrderByDescending(dp => dp.StartDate)
                .FirstOrDefaultAsync(ct);

            var profile = new DogProfileDto
            {
                Dog = AnimalMapper.MapAnimalList(dog),
                CurrentDeployment = deployment == null ? null : new DeploymentDto
                {
                    Id = deployment.Id,
                    DogId = deployment.DogId,
                    DogName = dog.Name,
                    CompanyId = deployment.CompanyId,
                    Location = deployment.Location,
                    CompanyDeployedTo = deployment.CompanyDeployedTo,
                    SecurityGuardAssigned = deployment.SecurityGuardAssigned,
                    StartDate = deployment.StartDate,
                    EndDate = deployment.EndDate,
                    Notes = deployment.Notes,
                    IsActive = deployment.IsActive,
                    CustomerId = deployment.CustomerId,
                    CustomerName = deployment.Customer?.FullName ?? (deployment.Customer == null ? null : $"{deployment.Customer.FirstName} {deployment.Customer.LastName}".Trim()),
                    CustomerSiteId = deployment.CustomerSiteId,
                    CustomerSiteName = deployment.CustomerSite?.SiteName,
                    CustomerSiteAddress = deployment.CustomerSite?.Address,
                },
                MatingRecords = matings.Select(m => new MatingRecordDto
                {
                    Id = m.Id,
                    SireId = m.SireId,
                    SireName = m.Sire != null ? m.Sire.Name : m.SireName,
                    DamId = m.DamId,
                    DamName = m.Dam != null ? m.Dam.Name : m.DamName,
                    MatingDate = m.MatingDate,
                    Location = m.Location,
                    Notes = m.Notes,
                    Litters = m.Litters.Select(l => new LitterDto
                    {
                        Id = l.Id,
                        BirthDate = l.BirthDate,
                        TotalBorn = l.TotalBorn,
                        SurvivedCount = l.SurvivedCount,
                        DiedCount = l.DiedCount,
                        Notes = l.Notes,
                        Puppies = l.Puppies.Select(p => new PuppyDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Color = p.Color,
                            Gender = p.Gender,
                            DateOfBirth = p.DateOfBirth,
                            DateOfDeath = p.DateOfDeath,
                            Vaccinations = new List<VaccinationRecordDto>()
                        }).ToList()
                    }).ToList()
                }).ToList(),
                Vaccinations = vaccinations.Select(v => new VaccinationRecordDto
                {
                    Id = v.Id,
                    DogId = v.DogId,
                    PuppyId = v.PuppyId,
                    Name = v.Name,
                    VaccType = v.VaccType,
                    BatchNumber = v.BatchNumber,
                    VetclinicName = v.VetclinicName,
                    VaccinationDate = v.VaccinationDate,
                    NextVaccinationDate = v.NextVaccinationDate,
                    Notes = v.Notes
                }).ToList(),
                CurrentAssignment = assignment == null ? null : new DogAssignmentDto
                {
                    Id = assignment.Id,
                    HandlerUserId = assignment.HandlerUserId,
                    HandlerName = assignment.HandlerUser == null ? null : $"{assignment.HandlerUser.Name} {assignment.HandlerUser.Surname}".Trim(),
                    BranchId = assignment.BranchId,
                    BranchName = assignment.Branch?.CompanyName,
                    AssignedOn = assignment.AssignedOn,
                    Notes = assignment.Notes
                },
                CareRecords = careRecords.Select(r => new DogCareRecordDto
                {
                    Id = r.Id,
                    EventType = r.EventType,
                    OccurredOn = r.OccurredOn,
                    Details = r.Details,
                    QuantityOrOutcome = r.QuantityOrOutcome,
                    RequiresAttention = r.RequiresAttention,
                    RecordedByName = r.RecordedByUser == null ? null : $"{r.RecordedByUser.Name} {r.RecordedByUser.Surname}".Trim(),
                    VaccineName = r.VaccineName,
                    VaccineBatch = r.VaccineBatch,
                    VaccineNextDue = r.VaccineNextDue,
                    VaccineNotes = r.VaccineNotes,
                    MatingPartnerName = r.MatingPartnerName,
                    MatingDuration = r.MatingDuration,
                    InjurySeverity = r.InjurySeverity,
                    InjuryDetails = r.InjuryDetails,
                    InjuryTreated = r.InjuryTreated,
                    BiteSeverity = r.BiteSeverity,
                    BiteDetails = r.BiteDetails,
                    BiteReportedTo = r.BiteReportedTo,
                    DewormingProduct = r.DewormingProduct,
                    DewormingNextDue = r.DewormingNextDue,
                }).ToList()
            };

            return ApiResponse<DogProfileDto>.Ok(profile);
        }
    }

    public class GetAssignedDogsHandler : IRequestHandler<GetAssignedDogsQuery, ApiResponse<List<AnimalListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetAssignedDogsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<AnimalListDto>>> Handle(GetAssignedDogsQuery request, CancellationToken ct)
        {
            var dogs = await _uow.Repository<DogAssignment>().TableNoTracking
                .Where(a => a.HandlerUserId == request.HandlerUserId && a.EndedOn == null)
                .Include(a => a.Dog).ThenInclude(d => d.Sire)
                .Include(a => a.Dog).ThenInclude(d => d.Dam)
                .Include(a => a.Dog).ThenInclude(d => d.Company)
                .Include(a => a.Dog).ThenInclude(d => d.Customer)
                .OrderByDescending(a => a.AssignedOn)
                .Select(a => a.Dog)
                .ToListAsync(ct);

            return ApiResponse<List<AnimalListDto>>.Ok(dogs.Select(AnimalMapper.MapAnimalList).ToList());
        }
    }

    public class AssignDogHandler : IRequestHandler<AssignDogCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AssignDogHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AssignDogCommand request, CancellationToken ct)
        {
            var dto = request.Dto;
            if (string.IsNullOrWhiteSpace(dto.HandlerUserId) && !dto.BranchId.HasValue)
                return ApiResponse<bool>.Fail("Choose a handler, a branch, or both.");

            var dog = await _uow.Repository<Dogtbl>().GetByIdAsync(dto.DogId, ct);
            if (dog == null) return ApiResponse<bool>.Fail("Dog not found.");

            if (!string.IsNullOrWhiteSpace(dto.HandlerUserId))
            {
                // Accept handlers from:
                // 1. The dog's own company (same company)
                // 2. Any branch of that company (ParentCompanyId == dog.CompanyId)
                var handler = await _uow.Repository<ApplicationUser>().TableNoTracking
                    .FirstOrDefaultAsync(u => u.Id == dto.HandlerUserId, ct);
                if (handler == null) return ApiResponse<bool>.Fail("Handler not found.");

                // Check if the handler's company is the dog's company OR a branch of it
                var handlerCompany = await _uow.Repository<Company>().TableNoTracking
                    .FirstOrDefaultAsync(c => c.Id == handler.CompanyId, ct);
                if (handlerCompany == null) return ApiResponse<bool>.Fail("Handler company not found.");

                bool sameCompany = handler.CompanyId == dog.CompanyId;
                bool isBranchOfDogCompany = handlerCompany.ParentCompanyId == dog.CompanyId;
                bool isDogCompanyBranch = handlerCompany.Id == (
                    await _uow.Repository<Company>().TableNoTracking
                        .Where(c => c.Id == dog.CompanyId)
                        .Select(c => c.ParentCompanyId)
                        .FirstOrDefaultAsync(ct));

                if (!sameCompany && !isBranchOfDogCompany && !isDogCompanyBranch)
                    return ApiResponse<bool>.Fail("Handler must belong to the same company or one of its branches.");
            }
            if (dto.BranchId.HasValue)
            {
                var branch = await _uow.Repository<Company>().TableNoTracking
                    .FirstOrDefaultAsync(c => c.Id == dto.BranchId.Value, ct);
                if (branch == null) return ApiResponse<bool>.Fail("Branch not found.");
            }

            var assignments = await _uow.Repository<DogAssignment>().Table
                .Where(a => a.DogId == dto.DogId && a.EndedOn == null).ToListAsync(ct);
            foreach (var active in assignments)
            {
                active.EndedOn = DateTime.UtcNow;
                await _uow.Repository<DogAssignment>().UpdateAsync(active, ct);
            }

            await _uow.Repository<DogAssignment>().AddAsync(new DogAssignment
            {
                DogId = dto.DogId,
                CompanyId = dog.CompanyId,
                HandlerUserId = dto.HandlerUserId,
                BranchId = dto.BranchId,
                Notes = dto.Notes,
                AssignedOn = DateTime.UtcNow
            }, ct);
            return ApiResponse<bool>.Ok(true, "Dog assignment saved.");
        }
    }

    /// <summary>Rules shared by the add/update care-record handlers.</summary>
    internal static class CareRecordRules
    {
        public const string FeedingEventType = "Feeding";

        public static bool IsFeeding(string? eventType)
            => string.Equals(eventType, FeedingEventType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Adds a care record for a dog (feeding, vaccination, illness, etc.).</summary>
    public class AddDogCareRecordHandler : IRequestHandler<AddDogCareRecordCommand, ApiResponse<bool>>
    {
        private static readonly string[] ValidEventTypes = new[] { "Feeding", "Vaccinated", "Illness", "Injured", "Bite someone", "Heat", "Mating", "Birth", "Litter", "Deworming", "Other" };
        private readonly IUnitOfWork _uow;
        private readonly IWhatsAppNotificationService _whatsAppNotify;
        private readonly IConfiguration _config;
        
        public AddDogCareRecordHandler(IUnitOfWork uow, IWhatsAppNotificationService whatsAppNotify, IConfiguration config)
        {
            _uow = uow;
            _whatsAppNotify = whatsAppNotify;
            _config = config;
        }

        public async Task<ApiResponse<bool>> Handle(AddDogCareRecordCommand request, CancellationToken ct)
        {
            var dto = request.Dto;
            if (!ValidEventTypes.Contains(dto.EventType, StringComparer.OrdinalIgnoreCase))
                return ApiResponse<bool>.Fail("Invalid care event type.");

            var dog = await _uow.Repository<Dogtbl>().TableNoTracking
                .FirstOrDefaultAsync(d => d.Id == dto.DogId, ct);
            if (dog == null) return ApiResponse<bool>.Fail("Dog not found.");

            // A dog is fed once a day, so a second feeding on the same calendar day is a
            // double entry (two handlers logging the same meal), not a real second meal.
            if (CareRecordRules.IsFeeding(dto.EventType))
            {
                var day = dto.OccurredOn.Date;
                var nextDay = day.AddDays(1);
                var alreadyFed = await _uow.Repository<DogCareRecord>().TableNoTracking
                    .AnyAsync(r => r.DogId == dto.DogId
                                && r.EventType == CareRecordRules.FeedingEventType
                                && r.OccurredOn >= day
                                && r.OccurredOn < nextDay, ct);
                if (alreadyFed)
                    return ApiResponse<bool>.Fail($"{dog.Name} already has a feeding record for {day:MMM dd, yyyy}. A dog is fed once a day — edit the existing record instead.");
            }

            // Always use the dog's own company for the care record
            // (branch users record care on dogs owned by head-office — that's valid)
            var effectiveCompanyId = dog.CompanyId;

            // If a recorder user ID is set, verify they have an active assignment or edit permission
            if (!string.IsNullOrWhiteSpace(dto.RecordedByUserId))
            {
                var recorder = await _uow.Repository<ApplicationUser>().TableNoTracking
                    .FirstOrDefaultAsync(u => u.Id == dto.RecordedByUserId, ct);
                if (recorder != null)
                {
                    // Use the recorder's company for the record if they are a branch user
                    // but keep effectiveCompanyId as the dog's company for foreign-key correctness
                }
            }

            // Create core care record
            var care = new DogCareRecord
            {
                DogId = dto.DogId,
                CompanyId = effectiveCompanyId,
                EventType = dto.EventType,
                OccurredOn = dto.OccurredOn,
                Details = dto.Details,
                QuantityOrOutcome = dto.QuantityOrOutcome,
                RequiresAttention = dto.RequiresAttention,
                RecordedByUserId = dto.RecordedByUserId,
                // These extended fields are stored on the care record itself so the
                // dog's Care History can show full details without joining out to
                // VaccinationRecord/MatingRecord (which are created separately below
                // for their own dashboard/report use).
                VaccineName = dto.VaccineName,
                VaccineBatch = dto.VaccineBatch,
                VaccineNextDue = dto.VaccineNextDue,
                VaccineNotes = dto.VaccineNotes,
                MatingPartnerName = dto.MatingPartnerName,
                MatingDuration = dto.MatingDuration,
                InjurySeverity = dto.InjurySeverity,
                InjuryDetails = dto.InjuryDetails,
                InjuryTreated = dto.InjuryTreated ?? false,
                BiteSeverity = dto.BiteSeverity,
                BiteDetails = dto.BiteDetails,
                BiteReportedTo = dto.BiteReportedTo,
                DewormingProduct = dto.DewormingProduct,
                DewormingNextDue = dto.DewormingNextDue,
            };

            await _uow.Repository<DogCareRecord>().AddAsync(care, ct);

            // If vaccination data present, create a VaccinationRecord
            if (string.Equals(dto.EventType, "Vaccinated", StringComparison.OrdinalIgnoreCase) && (!string.IsNullOrWhiteSpace(dto.VaccineName) || dto.VaccineNextDue.HasValue))
            {
                var vaccination = new VaccinationRecord
                {
                    DogId = dto.DogId,
                    CompanyId = effectiveCompanyId,
                    Name = dto.VaccineName,
                    BatchNumber = dto.VaccineBatch,
                    VaccinationDate = dto.OccurredOn,
                    NextVaccinationDate = dto.VaccineNextDue,
                    Notes = dto.VaccineNotes
                };
                await _uow.Repository<VaccinationRecord>().AddAsync(vaccination, ct);
            }

            // Create a MatingRecord when event is Mating and a partner is given — the partner
            // may be a registered dog (MatingPartnerId) OR an unregistered dog entered by name.
            if (string.Equals(dto.EventType, "Mating", StringComparison.OrdinalIgnoreCase)
                && (dto.MatingPartnerId.HasValue || !string.IsNullOrWhiteSpace(dto.MatingPartnerName)))
            {
                bool subjectIsMale = dog.Gender?.Equals("Male", StringComparison.OrdinalIgnoreCase) == true;
                // Subject dog goes on its own gender side; the partner takes the opposite side.
                var mating = new MatingRecord
                {
                    SireId = subjectIsMale ? dog.Id : dto.MatingPartnerId,
                    DamId = subjectIsMale ? dto.MatingPartnerId : dog.Id,
                    SireName = subjectIsMale ? null : (dto.MatingPartnerId.HasValue ? null : dto.MatingPartnerName),
                    DamName = subjectIsMale ? (dto.MatingPartnerId.HasValue ? null : dto.MatingPartnerName) : null,
                    CompanyId = effectiveCompanyId,
                    MatingDate = dto.OccurredOn,
                    Location = null,
                    Notes = string.IsNullOrWhiteSpace(dto.MatingDuration) ? dto.MatingPartnerName : ($"Partner: {dto.MatingPartnerName}; Duration: {dto.MatingDuration}")
                };

                await _uow.Repository<MatingRecord>().AddAsync(mating, ct);
            }

            // Get handler name for notification
            var handlerName = "System";
            if (!string.IsNullOrWhiteSpace(dto.RecordedByUserId))
            {
                var handler = await _uow.Repository<ApplicationUser>().TableNoTracking
                    .FirstOrDefaultAsync(u => u.Id == dto.RecordedByUserId, ct);
                if (handler != null)
                {
                    handlerName = $"{handler.Name} {handler.Surname}".Trim();
                    if (string.IsNullOrWhiteSpace(handlerName))
                        handlerName = handler.UserName ?? "Handler";
                }
            }

            // Send WhatsApp notification based on event type (after save completes)
            try
            {
                // Check if notifications are configured
                var numbersConfig = _config["NotifyWhatsapp:WhatsAppNumbers"] ?? "";
                if (!string.IsNullOrWhiteSpace(numbersConfig))
                {
                    var eventTypeLower = dto.EventType?.ToLower() ?? "";
                    
                    switch (eventTypeLower)
                    {
                        case "feeding":
                            await _whatsAppNotify.NotifyDogFedAsync(dog, care, handlerName);
                            break;
                        case "vaccinated":
                            await _whatsAppNotify.NotifyVaccinationAsync(dog, care, handlerName);
                            break;
                        case "mating":
                            var partnerName = dto.MatingPartnerName ?? "Unknown";
                            await _whatsAppNotify.NotifyMatingAsync(dog, care, partnerName, handlerName);
                            break;
                        case "illness":
                            await _whatsAppNotify.NotifyIllnessAsync(dog, care, handlerName);
                            break;
                        case "injured":
                            await _whatsAppNotify.NotifyInjuryAsync(dog, care, handlerName);
                            break;
                        case "bite someone":
                            await _whatsAppNotify.NotifyBiteIncidentAsync(dog, care, handlerName);
                            break;
                        case "deworming":
                            await _whatsAppNotify.NotifyDewormingAsync(dog, care, handlerName);
                            break;
                        case "litter":
                        case "birth":
                            await _whatsAppNotify.NotifyLitterAsync(dog, care, handlerName);
                            break;
                        default:
                            await _whatsAppNotify.NotifyGeneralCareAsync(dog, care, dto.EventType ?? "Other", handlerName);
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail - don't block the main operation
            }

            return ApiResponse<bool>.Ok(true, "Dog care record added.");
        }
    }

    /// <summary>Returns every care activity a handler recorded, newest first, enriched with dog info.</summary>
    public class GetHandlerActivitiesHandler : IRequestHandler<GetHandlerActivitiesQuery, ApiResponse<List<HandlerActivityDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetHandlerActivitiesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<HandlerActivityDto>>> Handle(GetHandlerActivitiesQuery request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.HandlerUserId))
                return ApiResponse<List<HandlerActivityDto>>.Ok(new List<HandlerActivityDto>());

            var records = await _uow.Repository<DogCareRecord>()
                .TableNoTracking
                .Include(r => r.Dog)
                .Where(r => r.RecordedByUserId == request.HandlerUserId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(500)
                .ToListAsync(ct);

            var list = records.Select(r => new HandlerActivityDto
            {
                Id = r.Id,
                DogId = r.DogId,
                DogName = r.Dog != null ? r.Dog.Name : "Unknown dog",
                DogPhotoUrl = r.Dog?.PhotoUrl,
                EventType = r.EventType,
                OccurredOn = r.OccurredOn,
                Details = r.Details,
                QuantityOrOutcome = r.QuantityOrOutcome,
                RequiresAttention = r.RequiresAttention,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                CanEdit = true
            }).ToList();

            return ApiResponse<List<HandlerActivityDto>>.Ok(list);
        }
    }

    /// <summary>Dogs whose most-recent vaccination has a next-due date that is overdue or within N days.</summary>
    public class GetVaccineDueHandler : IRequestHandler<GetVaccineDueQuery, ApiResponse<List<VaccineDueDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetVaccineDueHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<VaccineDueDto>>> Handle(GetVaccineDueQuery request, CancellationToken ct)
        {
            var today = DateTime.UtcNow.Date;
            var cutoff = today.AddDays(request.Days <= 0 ? 30 : request.Days);
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);

            var records = await _uow.Repository<VaccinationRecord>()
                .TableNoTracking
                .Include(v => v.Dog)
                .Where(v => companyIds.Contains(v.CompanyId) && v.NextVaccinationDate != null && v.DogId != null)
                .ToListAsync(ct);

            // One alert per dog: use that dog's most recent vaccination, then keep it only
            // if its next-due date is overdue or falls within the window.
            var list = records
                .GroupBy(v => v.DogId!.Value)
                .Select(g => g.OrderByDescending(v => v.VaccinationDate).First())
                .Where(v => v.NextVaccinationDate!.Value.Date <= cutoff
                            && (v.Dog == null || v.Dog.DateOfDeath == null))
                .OrderBy(v => v.NextVaccinationDate)
                .Select(v => new VaccineDueDto
                {
                    DogId = v.DogId!.Value,
                    DogName = v.Dog != null ? v.Dog.Name : "Unknown dog",
                    DogPhotoUrl = v.Dog?.PhotoUrl,
                    VaccineName = v.Name ?? v.VaccType ?? "Vaccination",
                    NextDue = v.NextVaccinationDate!.Value,
                    IsOverdue = v.NextVaccinationDate!.Value.Date < today,
                    DaysUntilDue = (int)(v.NextVaccinationDate!.Value.Date - today).TotalDays
                })
                .ToList();

            return ApiResponse<List<VaccineDueDto>>.Ok(list);
        }
    }

    public class GetPuppyVaccinationsHandler : IRequestHandler<GetPuppyVaccinationsQuery, ApiResponse<List<VaccinationRecordDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetPuppyVaccinationsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<VaccinationRecordDto>>> Handle(GetPuppyVaccinationsQuery request, CancellationToken ct)
        {
            var vaccinations = await _uow.Repository<VaccinationRecord>()
                .TableNoTracking
                .Where(v => v.PuppyId == request.PuppyId)
                .OrderByDescending(v => v.VaccinationDate)
                .ToListAsync(ct);

            var dtos = vaccinations.Select(v => new VaccinationRecordDto
            {
                Id = v.Id,
                DogId = v.DogId,
                PuppyId = v.PuppyId,
                Name = v.Name,
                VaccType = v.VaccType,
                BatchNumber = v.BatchNumber,
                VetclinicName = v.VetclinicName,
                VaccinationDate = v.VaccinationDate,
                NextVaccinationDate = v.NextVaccinationDate,
                Notes = v.Notes
            }).ToList();

            return ApiResponse<List<VaccinationRecordDto>>.Ok(dtos);
        }
    }

    /// <summary>Updates a care record. Only the handler who recorded it may edit it.</summary>
    public class UpdateDogCareRecordHandler : IRequestHandler<UpdateDogCareRecordCommand, ApiResponse<bool>>
    {
        private static readonly string[] ValidEventTypes = new[] { "Feeding", "Vaccinated", "Illness", "Injured", "Bite someone", "Heat", "Mating", "Birth", "Litter", "Deworming", "Other" };

        private readonly IUnitOfWork _uow;
        public UpdateDogCareRecordHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(UpdateDogCareRecordCommand request, CancellationToken ct)
        {
            var dto = request.Dto;
            if (!ValidEventTypes.Contains(dto.EventType, StringComparer.OrdinalIgnoreCase))
                return ApiResponse<bool>.Fail("Invalid care event type.");

            var repo = _uow.Repository<DogCareRecord>();
            var entity = await repo.GetByIdAsync(dto.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Care record not found.");

            // Ownership: a handler may only edit the records they created.
            if (string.IsNullOrWhiteSpace(dto.RecordedByUserId) || entity.RecordedByUserId != dto.RecordedByUserId)
                return ApiResponse<bool>.Fail("You can only edit care records you recorded.");

            // Same once-a-day feeding rule as on create — an edit must not be able to
            // land a second feeding on a day that already has one.
            if (CareRecordRules.IsFeeding(dto.EventType))
            {
                var day = dto.OccurredOn.Date;
                var nextDay = day.AddDays(1);
                var clashes = await repo.TableNoTracking
                    .AnyAsync(r => r.Id != entity.Id
                                && r.DogId == entity.DogId
                                && r.EventType == CareRecordRules.FeedingEventType
                                && r.OccurredOn >= day
                                && r.OccurredOn < nextDay, ct);
                if (clashes)
                    return ApiResponse<bool>.Fail($"There is already a feeding record for {day:MMM dd, yyyy}. A dog is fed once a day.");
            }

            entity.EventType = dto.EventType;
            entity.OccurredOn = dto.OccurredOn;
            entity.Details = dto.Details;
            entity.QuantityOrOutcome = dto.QuantityOrOutcome;
            entity.RequiresAttention = dto.RequiresAttention;

            await repo.UpdateAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Care record updated.");
        }
    }

    public class AddVaccinationHandler : IRequestHandler<AddVaccinationCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AddVaccinationHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AddVaccinationCommand request, CancellationToken ct)
        {
            var dto = request.Dto;

            // Always use the dog/puppy's own company, not whatever CompanyId the caller
            // sent — a head-office user acting on a branch's dog would otherwise stamp
            // the record with their own company, so it never shows up when that branch
            // (or a head-office view scoped to it) queries its vaccinated dogs.
            int effectiveCompanyId;
            if (dto.DogId is int dogId)
            {
                var dog = await _uow.Repository<Dogtbl>().TableNoTracking.FirstOrDefaultAsync(d => d.Id == dogId, ct);
                if (dog == null) return ApiResponse<bool>.Fail("Dog not found.");
                effectiveCompanyId = dog.CompanyId;
            }
            else if (dto.PuppyId is int puppyId)
            {
                var puppy = await _uow.Repository<Puppy>().TableNoTracking.FirstOrDefaultAsync(p => p.Id == puppyId, ct);
                if (puppy == null) return ApiResponse<bool>.Fail("Puppy not found.");
                effectiveCompanyId = puppy.CompanyId;
            }
            else
            {
                return ApiResponse<bool>.Fail("Either DogId or PuppyId is required.");
            }

            var vaccination = new VaccinationRecord
            {
                DogId = dto.DogId,
                PuppyId = dto.PuppyId,
                CompanyId = effectiveCompanyId,
                Name = dto.Name,
                VaccType = dto.VaccType,
                BatchNumber = dto.BatchNumber,
                VetclinicName = dto.VetclinicName,
                VaccinationDate = dto.VaccinationDate,
                NextVaccinationDate = dto.NextVaccinationDate,
                Notes = dto.Notes
            };

            await _uow.Repository<VaccinationRecord>().AddAsync(vaccination, ct);
            return ApiResponse<bool>.Ok(true, "Vaccination record added successfully.");
        }
    }

    public class AddMatingRecordHandler : IRequestHandler<AddMatingRecordCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AddMatingRecordHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AddMatingRecordCommand request, CancellationToken ct)
        {
            var dto = request.Dto;
            var matingRecord = new MatingRecord
            {
                SireId = dto.SireId,
                DamId = dto.DamId,
                // Keep the free-text name only when the dog is external (no id).
                SireName = dto.SireId.HasValue ? null : dto.SireName,
                DamName = dto.DamId.HasValue ? null : dto.DamName,
                CompanyId = dto.CompanyId,
                MatingDate = dto.MatingDate,
                Location = dto.Location,
                Notes = dto.Notes
            };

            await _uow.Repository<MatingRecord>().AddAsync(matingRecord, ct);
            return ApiResponse<bool>.Ok(true, "Mating record added successfully.");
        }
    }

    public class AddLitterHandler : IRequestHandler<AddLitterCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AddLitterHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AddLitterCommand request, CancellationToken ct)
        {
            var dto = request.Dto;
            var litter = new Litter
            {
                MatingRecordId = dto.MatingRecordId,
                CompanyId = dto.CompanyId,
                BirthDate = dto.BirthDate,
                TotalBorn = dto.TotalBorn,
                SurvivedCount = dto.SurvivedCount,
                DiedCount = dto.DiedCount,
                Notes = dto.Notes
            };

            await _uow.Repository<Litter>().AddAsync(litter, ct);
            return ApiResponse<bool>.Ok(true, "Litter record added successfully.");
        }
    }

    public class GetMatingRecordsByCompanyHandler : IRequestHandler<GetMatingRecordsByCompanyQuery, ApiResponse<List<MatingRecordDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetMatingRecordsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<MatingRecordDto>>> Handle(GetMatingRecordsByCompanyQuery request, CancellationToken ct)
        {
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);
            var records = await _uow.Repository<MatingRecord>()
                .TableNoTracking
                .Include(m => m.Sire)
                .Include(m => m.Dam)
                .Where(m => companyIds.Contains(m.CompanyId))
                .OrderByDescending(m => m.MatingDate)
                .ToListAsync(ct);

            var result = records.Select(m => new MatingRecordDto
            {
                Id = m.Id,
                SireId = m.SireId,
                SireName = m.Sire != null ? m.Sire.Name : m.SireName,
                DamId = m.DamId,
                DamName = m.Dam != null ? m.Dam.Name : m.DamName,
                MatingDate = m.MatingDate,
                Location = m.Location,
                Notes = m.Notes
            }).ToList();

            return ApiResponse<List<MatingRecordDto>>.Ok(result);
        }
    }

    public class AddPuppyHandler : IRequestHandler<AddPuppyCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AddPuppyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AddPuppyCommand request, CancellationToken ct)
        {
            var dto = request.Dto;
            var puppy = new Puppy
            {
                LitterId = dto.LitterId,
                CompanyId = dto.CompanyId,
                Name = dto.Name,
                Gender = dto.Gender,
                Color = dto.Color,
                DateOfBirth = dto.DateOfBirth,
                DateOfDeath = dto.DateOfDeath
            };

            await _uow.Repository<Puppy>().AddAsync(puppy, ct);
            return ApiResponse<bool>.Ok(true, "Puppy added successfully.");
        }
    }

    public class GetLittersByCompanyHandler : IRequestHandler<GetLittersByCompanyQuery, ApiResponse<List<LitterDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetLittersByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<LitterDto>>> Handle(GetLittersByCompanyQuery request, CancellationToken ct)
        {
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);
            var litters = await _uow.Repository<Litter>()
                .TableNoTracking
                .Include(l => l.MatingRecord)
                    .ThenInclude(m => m.Sire)
                .Include(l => l.MatingRecord)
                    .ThenInclude(m => m.Dam)
                .Include(l => l.Puppies)
                .Where(l => companyIds.Contains(l.CompanyId))
                .OrderByDescending(l => l.BirthDate)
                .ToListAsync(ct);

            var puppyIds = litters.SelectMany(l => l.Puppies.Select(p => p.Id)).ToList();
            var puppyVaccinations = puppyIds.Any()
                ? await _uow.Repository<VaccinationRecord>()
                    .TableNoTracking
                    .Where(v => v.PuppyId.HasValue && puppyIds.Contains(v.PuppyId.Value))
                    .ToListAsync(ct)
                : new List<VaccinationRecord>();

            var result = litters.Select(l => new LitterDto
            {
                Id = l.Id,
                BirthDate = l.BirthDate,
                TotalBorn = l.TotalBorn,
                SurvivedCount = l.SurvivedCount,
                DiedCount = l.DiedCount,
                Notes = l.Notes,
                SireName = l.MatingRecord == null ? null : (l.MatingRecord.Sire != null ? l.MatingRecord.Sire.Name : l.MatingRecord.SireName),
                DamName = l.MatingRecord == null ? null : (l.MatingRecord.Dam != null ? l.MatingRecord.Dam.Name : l.MatingRecord.DamName),
                Puppies = l.Puppies.Select(p => new PuppyDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Color = p.Color,
                    Gender = p.Gender,
                    DateOfBirth = p.DateOfBirth,
                    DateOfDeath = p.DateOfDeath,
                    Vaccinations = puppyVaccinations
                        .Where(v => v.PuppyId == p.Id)
                        .Select(v => new VaccinationRecordDto
                        {
                            Id = v.Id,
                            DogId = v.DogId,
                            PuppyId = v.PuppyId,
                            Name = v.Name,
                            VaccType = v.VaccType,
                            BatchNumber = v.BatchNumber,
                            VetclinicName = v.VetclinicName,
                            VaccinationDate = v.VaccinationDate,
                            NextVaccinationDate = v.NextVaccinationDate,
                            Notes = v.Notes
                        })
                        .OrderByDescending(v => v.VaccinationDate)
                        .ToList()
                }).ToList()
            }).ToList();

            return ApiResponse<List<LitterDto>>.Ok(result);
        }
    }

    internal static class AnimalMapper
    {
        public static AnimalDto MapAnimal(Dogtbl d) => new()
        {
            Id = d.Id,
            Name = d.Name,
            Breed = d.Breed,
            Color = d.Color,
            DateOfBirth = d.DateOfBirth,
            DateOfDeath = d.DateOfDeath,
            Gender = d.Gender,
            MicrochipNumber = d.MicrochipNumber,
            PhotoUrl = d.PhotoUrl,
            SireId = d.SireId,
            DamId = d.DamId,
            IsActive = d.IsActive,
            CompanyId = d.CompanyId,
            CustomerId = d.CustomerId,
        };

        public static AnimalListDto MapAnimalList(Dogtbl p) => new()
        {
            Id = p.Id,
            Name = p.Name ?? string.Empty,
            IsActive = p.IsActive,
            Breed = p.Breed,
            Color = p.Color,
            DateOfBirth = p.DateOfBirth,
            DateOfDeath = p.DateOfDeath,
            Gender = p.Gender,
            MicrochipNumber = p.MicrochipNumber,
            PhotoUrl = p.PhotoUrl,
            SireId = p.SireId,
            SireName = p.Sire?.Name,
            DamId = p.DamId,
            DamName = p.Dam?.Name,
            CompanyId = p.CompanyId,
            CompanyName = p.Company?.CompanyName,
            CustomerId = p.CustomerId,
            CustomerName = p.Customer == null ? null
                : (string.IsNullOrWhiteSpace(p.Customer.FullName)
                    ? $"{p.Customer.FirstName} {p.Customer.LastName}".Trim()
                    : p.Customer.FullName),
        };
    }

    // Reports handlers
    public class GetAnimalsHandler : IRequestHandler<GetAnimalsQuery, ApiResponse<AnimalPageDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetAnimalsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<AnimalPageDto>> Handle(GetAnimalsQuery request, CancellationToken ct)
        {
            var query = _uow.Repository<Dogtbl>().TableNoTracking.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(d => d.Name.Contains(request.Search));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                var isActive = request.Status.ToLower() == "active";
                query = query.Where(d => d.IsActive == isActive);
            }

            if (!string.IsNullOrEmpty(request.Gender))
            {
                query = query.Where(d => d.Gender == request.Gender);
            }

            var total = await query.CountAsync(ct);

            var animals = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var result = new AnimalPageDto
            {
                Data = animals.Select(d => new AnimalItemDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Breed = d.Breed,
                    Color = d.Color,
                    BirthDate = d.DateOfBirth,
                    Gender = d.Gender,
                    MicrochipNumber = d.MicrochipNumber,
                    OwnerName = d.Company?.CompanyName,
                    IsActive = d.IsActive
                }).ToList(),
                Total = total,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return ApiResponse<AnimalPageDto>.Ok(result);
        }
    }

    public class GetVaccinatedDogsCountHandler : IRequestHandler<GetVaccinatedDogsCountQuery, ApiResponse<object>>
    {
        private readonly IUnitOfWork _uow;
        public GetVaccinatedDogsCountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<object>> Handle(GetVaccinatedDogsCountQuery request, CancellationToken ct)
        {
            // DogId is null for puppy-only vaccinations (recorded before the puppy is
            // promoted to a registered dog) — exclude those so this matches the actual
            // number of registered dogs with a vaccination, not a phantom "null" dog.
            var query = _uow.Repository<VaccinationRecord>().TableNoTracking.Where(v => v.DogId != null);
            if (request.CompanyId is int companyId)
            {
                var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, companyId, ct);
                query = query.Where(v => companyIds.Contains(v.CompanyId));
            }

            var count = await query.Select(v => v.DogId).Distinct().CountAsync(ct);

            return ApiResponse<object>.Ok(new { count });
        }
    }

    public class GetVaccinatedDogsHandler : IRequestHandler<GetVaccinatedDogsQuery, ApiResponse<List<VaccinatedDogDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetVaccinatedDogsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<VaccinatedDogDto>>> Handle(GetVaccinatedDogsQuery request, CancellationToken ct)
        {
            // GroupBy combined with a navigation lookup inside the same projection
            // (g.First().dog.Name) is a pattern EF Core/Pomelo can fail to translate
            // reliably, sometimes returning zero rows instead of throwing. Flatten
            // with a plain Select first (translates fine), then group in memory.
            var query = _uow.Repository<VaccinationRecord>().TableNoTracking.Where(v => v.DogId != null);
            if (request.CompanyId is int companyId)
            {
                var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, companyId, ct);
                query = query.Where(v => companyIds.Contains(v.CompanyId));
            }

            var records = await query
                .Select(v => new
                {
                    DogId = v.DogId!.Value,
                    DogName = v.Dog!.Name,
                    Breed = v.Dog!.Breed,
                    v.VaccinationDate,
                    v.VaccType,
                    v.NextVaccinationDate,
                })
                .ToListAsync(ct);

            var vaccinatedDogs = records
                .GroupBy(v => v.DogId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(v => v.VaccinationDate).First();
                    return new VaccinatedDogDto
                    {
                        DogId = g.Key,
                        DogName = g.First().DogName,
                        Breed = g.First().Breed,
                        LastVaccinationDate = g.Max(v => v.VaccinationDate),
                        VaccineType = latest.VaccType ?? "Unknown",
                        NextDueDate = latest.NextVaccinationDate ?? DateTime.Now.AddYears(1),
                        VaccinationStatus = latest.NextVaccinationDate > DateTime.Now ? "Up to Date" : "Due",
                    };
                })
                .ToList();

            return ApiResponse<List<VaccinatedDogDto>>.Ok(vaccinatedDogs);
        }
    }

    public class GetLittersCountHandler : IRequestHandler<GetLittersCountQuery, ApiResponse<object>>
    {
        private readonly IUnitOfWork _uow;
        public GetLittersCountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<object>> Handle(GetLittersCountQuery request, CancellationToken ct)
        {
            var query = _uow.Repository<Litter>().TableNoTracking;
            if (request.CompanyId is int companyId)
            {
                var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, companyId, ct);
                query = query.Where(l => companyIds.Contains(l.CompanyId));
            }
            var count = await query.CountAsync(ct);
            return ApiResponse<object>.Ok(new { count });
        }
    }

    public class GetLittersForReportHandler : IRequestHandler<GetLittersForReportQuery, ApiResponse<List<LitterReportDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetLittersForReportHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<LitterReportDto>>> Handle(GetLittersForReportQuery request, CancellationToken ct)
        {
            var litters = await _uow.Repository<Litter>()
                .TableNoTracking
                .Include(l => l.MatingRecord)
                    .ThenInclude(m => m.Sire)
                .Include(l => l.MatingRecord)
                    .ThenInclude(m => m.Dam)
                .OrderByDescending(l => l.BirthDate)
                .ToListAsync(ct);

            var result = litters.Select(l => new LitterReportDto
            {
                Id = l.Id,
                SireName = (l.MatingRecord == null ? null : (l.MatingRecord.Sire != null ? l.MatingRecord.Sire.Name : l.MatingRecord.SireName)) ?? "Unknown",
                DamName = (l.MatingRecord == null ? null : (l.MatingRecord.Dam != null ? l.MatingRecord.Dam.Name : l.MatingRecord.DamName)) ?? "Unknown",
                BirthDate = l.BirthDate,
                TotalPuppies = l.TotalBorn,
                AlivePuppies = l.SurvivedCount,
                DeceasedPuppies = l.DiedCount
            }).ToList();

            return ApiResponse<List<LitterReportDto>>.Ok(result);
        }
    }

    public class GetPuppiesForReportHandler : IRequestHandler<GetPuppiesForReportQuery, ApiResponse<List<PuppyReportDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetPuppiesForReportHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<PuppyReportDto>>> Handle(GetPuppiesForReportQuery request, CancellationToken ct)
        {
            var puppies = await _uow.Repository<Puppy>()
                .TableNoTracking
                .Include(p => p.Litter)
                    .ThenInclude(l => l.MatingRecord)
                        .ThenInclude(m => m.Sire)
                .Include(p => p.Litter)
                    .ThenInclude(l => l.MatingRecord)
                        .ThenInclude(m => m.Dam)
                .OrderByDescending(p => p.DateOfBirth)
                .ToListAsync(ct);

            var result = puppies.Select(p => new PuppyReportDto
            {
                Id = p.Id,
                Name = p.Name,
                Color = p.Color,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                DateOfDeath = p.DateOfDeath,
                LitterId = p.LitterId,
                SireName = (p.Litter?.MatingRecord == null ? null : (p.Litter.MatingRecord.Sire != null ? p.Litter.MatingRecord.Sire.Name : p.Litter.MatingRecord.SireName)) ?? "Unknown",
                DamName = (p.Litter?.MatingRecord == null ? null : (p.Litter.MatingRecord.Dam != null ? p.Litter.MatingRecord.Dam.Name : p.Litter.MatingRecord.DamName)) ?? "Unknown",
            }).ToList();

            return ApiResponse<List<PuppyReportDto>>.Ok(result);
        }
    }

    public class GetMatingRecordsCountHandler : IRequestHandler<GetMatingRecordsCountQuery, ApiResponse<object>>
    {
        private readonly IUnitOfWork _uow;
        public GetMatingRecordsCountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<object>> Handle(GetMatingRecordsCountQuery request, CancellationToken ct)
        {
            var query = _uow.Repository<MatingRecord>().TableNoTracking;
            if (request.CompanyId is int companyId)
            {
                var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, companyId, ct);
                query = query.Where(m => companyIds.Contains(m.CompanyId));
            }
            var count = await query.CountAsync(ct);
            return ApiResponse<object>.Ok(new { count });
        }
    }

    public class GetMatingRecordsForReportHandler : IRequestHandler<GetMatingRecordsForReportQuery, ApiResponse<List<MatingRecordReportDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetMatingRecordsForReportHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<MatingRecordReportDto>>> Handle(GetMatingRecordsForReportQuery request, CancellationToken ct)
        {
            var records = await _uow.Repository<MatingRecord>()
                .TableNoTracking
                .Include(m => m.Sire)
                .Include(m => m.Dam)
                .OrderByDescending(m => m.MatingDate)
                .ToListAsync(ct);

            const int gestationDays = 63; // Average gestation period for dogs

            // Expected due date is per dam, per mating period: the first mating starts the period and
            // sets the expected date; further matings of the same dam before that date is reached
            // reuse it. A new expected date only starts once the period has completed.
            var expectedById = new Dictionary<int, DateTime>();
            foreach (var group in records.GroupBy(m => m.DamId.HasValue
                         ? $"id:{m.DamId}"
                         : $"name:{(m.DamName ?? m.Dam?.Name ?? "").Trim().ToLowerInvariant()}"))
            {
                DateTime? periodExpected = null;
                foreach (var m in group.OrderBy(x => x.MatingDate).ThenBy(x => x.Id))
                {
                    if (periodExpected == null || m.MatingDate.Date > periodExpected.Value.Date)
                        periodExpected = m.MatingDate.AddDays(gestationDays);
                    expectedById[m.Id] = periodExpected.Value;
                }
            }

            var result = records.Select(m => new MatingRecordReportDto
            {
                Id = m.Id,
                SireName = (m.Sire != null ? m.Sire.Name : m.SireName) ?? "Unknown",
                DamName = (m.Dam != null ? m.Dam.Name : m.DamName) ?? "Unknown",
                MatingDate = m.MatingDate,
                Location = m.Location,
                ExpectedDueDate = expectedById[m.Id],
                Status = DateTime.Now > expectedById[m.Id] ? "Completed" : "Pending"
            }).ToList();

            return ApiResponse<List<MatingRecordReportDto>>.Ok(result);
        }
    }
}
