# 1. Add GetDogProfileHandler to AnimalHandlers.cs
$handlersPath = "Dog.Application/Features/Animals/Handlers/AnimalHandlers.cs"
$handlers = Get-Content $handlersPath -Raw

$insertBefore = "    internal static class AnimalMapper"
$newHandler = @"
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
                .Where(m => m.SireId == request.DogId || m.DamId == request.DogId)
                .Include(m => m.Sire)
                .Include(m => m.Dam)
                .Include(m => m.Litters)
                    .ThenInclude(l => l.Puppies)
                .OrderByDescending(m => m.MatingDate)
                .ToListAsync(ct);

            var vaccinations = await _uow.Repository<VaccinationRecord>()
                .TableNoTracking
                .Where(v => v.DogtId == request.DogId)
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

"@

$handlers = $handlers.Replace($insertBefore, $newHandler + $insertBefore)
[System.IO.File]::WriteAllText((Resolve-Path $handlersPath), $handlers, [System.Text.UTF8Encoding]::new($false))
Write-Host "Handlers updated"

# 2. Add GetDogProfile portal action to DogsController.cs
$controllerPath = "Dog.Portal/Controllers/DogsController.cs"
$controller = Get-Content $controllerPath -Raw

$insertBefore2 = "        // GET /Dogs/GetAnimalsForDropdown"
$newAction = @"
        // GET /Dogs/GetDogProfile/{id}
        [HttpGet]
        public async Task<IActionResult> GetDogProfile(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>(`$"api/animal/GetDogProfile/{id}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data, message = resp?.Message });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

"@

$controller = $controller.Replace($insertBefore2, $newAction + $insertBefore2)
[System.IO.File]::WriteAllText((Resolve-Path $controllerPath), $controller, [System.Text.UTF8Encoding]::new($false))
Write-Host "Controller updated"

Write-Host "ALL DONE"
