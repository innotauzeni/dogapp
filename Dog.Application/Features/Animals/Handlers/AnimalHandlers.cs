using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Animals.Commands;
using Dog.Application.Features.Animals.DTOs;
using Dog.Application.Features.Animals.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Dog.Application.Features.Animals.Handlers
{
    public class GetAnimalsByCompanyHandler : IRequestHandler<GetAnimalsByCompanyQuery, ApiResponse<List<AnimalListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetAnimalsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<AnimalListDto>>> Handle(GetAnimalsByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Dogtbl>()
                .TableNoTracking
                .Where(p => p.CompanyId == request.CompanyId)
                .Include(q=>q.Sire)
                .Include(q=>q.Dam)
                .Include(q=>q.Company)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            // Get active deployments to show deployment status on each dog
            var dogIds = list.Select(d => d.Id).ToList();
            var deployments = await _uow.Repository<Deployment>()
                .TableNoTracking
                .Where(d => dogIds.Contains(d.DogId) && d.IsActive)
                .ToListAsync(ct);

            var dtos = list.Select(d =>
            {
                var dto = AnimalMapper.MapAnimalList(d);
                var dep = deployments.FirstOrDefault(dep => dep.DogId == d.Id);
                if (dep != null)
                {
                    dto.IsDeployed = true;
                    dto.DeployedLocation = dep.Location;
                    dto.DeployedTo = dep.CompanyDeployedTo;
                }
                return dto;
            }).ToList();

            return ApiResponse<List<AnimalListDto>>.Ok(dtos);
        }
    }

    public class CreateAnimalHandler : IRequestHandler<CreateAnimalCommand, ApiResponse<AnimalDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateAnimalHandler(IUnitOfWork uow) => _uow = uow;

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
                IsActive = d.IsActive ?? true, // New dogs are active by default
                CompanyId = d.CompanyId,
            };
            await _uow.Repository<Dogtbl>().AddAsync(entity, ct);
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
             if (request.Dto.IsActive.HasValue)
             {
                 entity.IsActive = request.Dto.IsActive.Value;
             }
            
            await repo.UpdateAsync(entity, ct);
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

            var profile = new DogProfileDto
            {
                Dog = AnimalMapper.MapAnimalList(dog),
                MatingRecords = matings.Select(m => new MatingRecordDto
                {
                    Id = m.Id,
                    SireId = m.SireId,
                    SireName = m.Sire?.Name,
                    DamId = m.DamId,
                    DamName = m.Dam?.Name,
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
                            DateOfDeath = p.DateOfDeath
                        }).ToList()
                    }).ToList()
                }).ToList(),
                Vaccinations = vaccinations.Select(v => new VaccinationRecordDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    VaccType = v.VaccType,
                    BatchNumber = v.BatchNumber,
                    VetclinicName = v.VetclinicName,
                    VaccinationDate = v.VaccinationDate,
                    NextVaccinationDate = v.NextVaccinationDate,
                    Notes = v.Notes
                }).ToList()
            };

            return ApiResponse<DogProfileDto>.Ok(profile);
        }
    }

    public class AddVaccinationHandler : IRequestHandler<AddVaccinationCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AddVaccinationHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AddVaccinationCommand request, CancellationToken ct)
        {
            var dto = request.Dto;
            var vaccination = new VaccinationRecord
            {
                DogId = dto.DogId,
                CompanyId = dto.CompanyId,
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
            var records = await _uow.Repository<MatingRecord>()
                .TableNoTracking
                .Include(m => m.Sire)
                .Include(m => m.Dam)
                .Where(m => m.CompanyId == request.CompanyId)
                .OrderByDescending(m => m.MatingDate)
                .ToListAsync(ct);

            var result = records.Select(m => new MatingRecordDto
            {
                Id = m.Id,
                SireId = m.SireId,
                SireName = m.Sire?.Name,
                DamId = m.DamId,
                DamName = m.Dam?.Name,
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
            var litters = await _uow.Repository<Litter>()
                .TableNoTracking
                .Include(l => l.MatingRecord)
                    .ThenInclude(m => m.Sire)
                .Include(l => l.MatingRecord)
                    .ThenInclude(m => m.Dam)
                .Where(l => l.CompanyId == request.CompanyId)
                .OrderByDescending(l => l.BirthDate)
                .ToListAsync(ct);

            var result = litters.Select(l => new LitterDto
            {
                Id = l.Id,
                BirthDate = l.BirthDate,
                TotalBorn = l.TotalBorn,
                SurvivedCount = l.SurvivedCount,
                DiedCount = l.DiedCount,
                Notes = l.Notes,
                SireName = l.MatingRecord?.Sire?.Name,
                DamName = l.MatingRecord?.Dam?.Name
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
            var count = await _uow.Repository<VaccinationRecord>()
                .TableNoTracking
                .Select(v => v.DogId)
                .Distinct()
                .CountAsync(ct);

            return ApiResponse<object>.Ok(new { count });
        }
    }

    public class GetVaccinatedDogsHandler : IRequestHandler<GetVaccinatedDogsQuery, ApiResponse<List<VaccinatedDogDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetVaccinatedDogsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<VaccinatedDogDto>>> Handle(GetVaccinatedDogsQuery request, CancellationToken ct)
        {
            var vaccinatedDogs = await _uow.Repository<VaccinationRecord>()
                .TableNoTracking
                .Include(v => v.dog)
                .GroupBy(v => v.DogId)
                .Select(g => new VaccinatedDogDto
                {
                    DogName = g.First().dog.Name,
                    Breed = g.First().dog.Breed,
                    LastVaccinationDate = g.Max(v => v.VaccinationDate),
                    VaccineType = g.OrderByDescending(v => v.VaccinationDate).First().VaccType ?? "Unknown",
                    NextDueDate = g.OrderByDescending(v => v.VaccinationDate).First().NextVaccinationDate ?? DateTime.Now.AddYears(1),
                    VaccinationStatus = g.OrderByDescending(v => v.VaccinationDate).First().NextVaccinationDate > DateTime.Now ? "Up to Date" : "Due"
                })
                .ToListAsync(ct);

            return ApiResponse<List<VaccinatedDogDto>>.Ok(vaccinatedDogs);
        }
    }

    public class GetLittersCountHandler : IRequestHandler<GetLittersCountQuery, ApiResponse<object>>
    {
        private readonly IUnitOfWork _uow;
        public GetLittersCountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<object>> Handle(GetLittersCountQuery request, CancellationToken ct)
        {
            var count = await _uow.Repository<Litter>().TableNoTracking.CountAsync(ct);
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
                SireName = l.MatingRecord?.Sire?.Name ?? "Unknown",
                DamName = l.MatingRecord?.Dam?.Name ?? "Unknown",
                BirthDate = l.BirthDate,
                TotalPuppies = l.TotalBorn,
                AlivePuppies = l.SurvivedCount,
                DeceasedPuppies = l.DiedCount
            }).ToList();

            return ApiResponse<List<LitterReportDto>>.Ok(result);
        }
    }

    public class GetMatingRecordsCountHandler : IRequestHandler<GetMatingRecordsCountQuery, ApiResponse<object>>
    {
        private readonly IUnitOfWork _uow;
        public GetMatingRecordsCountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<object>> Handle(GetMatingRecordsCountQuery request, CancellationToken ct)
        {
            var count = await _uow.Repository<MatingRecord>().TableNoTracking.CountAsync(ct);
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

            var result = records.Select(m => new MatingRecordReportDto
            {
                Id = m.Id,
                SireName = m.Sire?.Name ?? "Unknown",
                DamName = m.Dam?.Name ?? "Unknown",
                MatingDate = m.MatingDate,
                Location = m.Location,
                ExpectedDueDate = m.MatingDate.AddDays(63), // Average gestation period for dogs
                Status = DateTime.Now > m.MatingDate.AddDays(63) ? "Completed" : "Pending"
            }).ToList();

            return ApiResponse<List<MatingRecordReportDto>>.Ok(result);
        }
    }
}
