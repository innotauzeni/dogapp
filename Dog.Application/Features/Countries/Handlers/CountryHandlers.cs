using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Countries.Commands;
using Dog.Application.Features.Countries.DTOs;
using Dog.Application.Features.Countries.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Dog.Application.Features.Countries.Handlers
{
    // ── COUNTRIES ────────────────────────────────────────────────────────────
    public class GetAllCountriesHandler : IRequestHandler<GetAllCountriesQuery, ApiResponse<List<CountryDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetAllCountriesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CountryDto>>> Handle(GetAllCountriesQuery request, CancellationToken ct)
        {
            var countries = await _uow.Repository<Country>()
                .TableNoTracking
                .Include(c => c.Provinces)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            return ApiResponse<List<CountryDto>>.Ok(countries.Select(c => new CountryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                ProvinceCount = c.Provinces?.Count ?? 0,
            }).ToList());
        }
    }

    public class CreateCountryHandler : IRequestHandler<CreateCountryCommand, ApiResponse<CountryDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateCountryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CountryDto>> Handle(CreateCountryCommand request, CancellationToken ct)
        {
            var entity = new Country { Code = request.Dto.Code, Name = request.Dto.Name };
            await _uow.Repository<Country>().AddAsync(entity, ct);
            return ApiResponse<CountryDto>.Ok(new CountryDto { Id = entity.Id, Code = entity.Code, Name = entity.Name }, "Country created.");
        }
    }

    public class UpdateCountryHandler : IRequestHandler<UpdateCountryCommand, ApiResponse<CountryDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateCountryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CountryDto>> Handle(UpdateCountryCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Country>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<CountryDto>.Fail("Country not found.");
            entity.Code = request.Dto.Code;
            entity.Name = request.Dto.Name;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<CountryDto>.Ok(new CountryDto { Id = entity.Id, Code = entity.Code, Name = entity.Name }, "Country updated.");
        }
    }

    public class DeleteCountryHandler : IRequestHandler<DeleteCountryCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteCountryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteCountryCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Country>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Country not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Country deleted.");
        }
    }

    // ── PROVINCES ────────────────────────────────────────────────────────────
    public class GetProvincesByCountryHandler : IRequestHandler<GetProvincesByCountryQuery, ApiResponse<List<ProvinceDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetProvincesByCountryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ProvinceDto>>> Handle(GetProvincesByCountryQuery request, CancellationToken ct)
        {
            var provinces = await _uow.Repository<Province>()
                .TableNoTracking
                .Where(q=>q.CountryId==request.CountryId)
                .Include(p => p.Country)
                .Include(p => p.Cities)
                .Where(p => true) // Return all provinces for simplicity
                .OrderBy(p => p.Name)
                .ToListAsync(ct);


            return ApiResponse<List<ProvinceDto>>.Ok(provinces.Select(c => new ProvinceDto
            {
                Id = c.Id,
                Name = c.Name,
                CityCount = c.Cities?.Count ?? 0,
            }).ToList());
            
        }
    }

    public class CreateProvinceHandler : IRequestHandler<CreateProvinceCommand, ApiResponse<ProvinceDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateProvinceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProvinceDto>> Handle(CreateProvinceCommand request, CancellationToken ct)
        {
            var entity = new Province { Name = request.Dto.Name, CountryId = request.Dto.CountryId };
            await _uow.Repository<Province>().AddAsync(entity, ct);
            return ApiResponse<ProvinceDto>.Ok(new ProvinceDto { Id = entity.Id, Name = entity.Name, CountryId = entity.CountryId }, "Province created.");
        }
    }

    public class UpdateProvinceHandler : IRequestHandler<UpdateProvinceCommand, ApiResponse<ProvinceDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateProvinceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProvinceDto>> Handle(UpdateProvinceCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Province>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<ProvinceDto>.Fail("Province not found.");
            entity.Name = request.Dto.Name;
            entity.CountryId = request.Dto.CountryId;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<ProvinceDto>.Ok(new ProvinceDto { Id = entity.Id, Name = entity.Name, CountryId = entity.CountryId }, "Province updated.");
        }
    }

    public class DeleteProvinceHandler : IRequestHandler<DeleteProvinceCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteProvinceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteProvinceCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Province>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Province not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Province deleted.");
        }
    }

    // ── CITIES ───────────────────────────────────────────────────────────────
    public class GetCitiesByProvinceHandler : IRequestHandler<GetCitiesByProvinceQuery, ApiResponse<List<CityDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetCitiesByProvinceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CityDto>>> Handle(GetCitiesByProvinceQuery request, CancellationToken ct)
        {
            var cities = await _uow.Repository<City>()
                .TableNoTracking
                 .Where(q => q.ProvinceId == request.ProvinceId)
                .Include(c => c.Province).ThenInclude(p => p!.Country)
                .Where(c => true) // Return all cities for simplicity
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
            return ApiResponse<List<CityDto>>.Ok(cities.Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList());
          
        }
    }

    public class CreateCityHandler : IRequestHandler<CreateCityCommand, ApiResponse<CityDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateCityHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CityDto>> Handle(CreateCityCommand request, CancellationToken ct)
        {
            var entity = new City { Name = request.Dto.Name, ProvinceId = request.Dto.ProvinceId };
            await _uow.Repository<City>().AddAsync(entity, ct);
            return ApiResponse<CityDto>.Ok(new CityDto { Id = entity.Id, Name = entity.Name, ProvinceId = entity.ProvinceId }, "City created.");
        }
    }

    public class UpdateCityHandler : IRequestHandler<UpdateCityCommand, ApiResponse<CityDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateCityHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CityDto>> Handle(UpdateCityCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<City>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<CityDto>.Fail("City not found.");
            entity.Name = request.Dto.Name;
            entity.ProvinceId = request.Dto.ProvinceId;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<CityDto>.Ok(new CityDto { Id = entity.Id, Name = entity.Name, ProvinceId = entity.ProvinceId }, "City updated.");
        }
    }

    public class DeleteCityHandler : IRequestHandler<DeleteCityCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteCityHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteCityCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<City>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("City not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "City deleted.");
        }
    }






    // ─────────────────────────────────────────────────────────────────────────
    // Handler: Import countries + provinces from web
    // ─────────────────────────────────────────────────────────────────────────

    public class ImportCountriesFromWebHandler
        : IRequestHandler<ImportCountriesFromWebQuery, ApiResponse<ImportSummaryDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _httpFactory;

        public ImportCountriesFromWebHandler(IUnitOfWork uow, IHttpClientFactory httpFactory)
        {
            _uow = uow;
            _httpFactory = httpFactory;
        }

        public async Task<ApiResponse<ImportSummaryDto>> Handle(
            ImportCountriesFromWebQuery request, CancellationToken ct)
        {
            var summary = new ImportSummaryDto();
            var http = _httpFactory.CreateClient("GeoClient");

            // 1. Fetch all countries from CountriesNow API
            var countriesResp = await http.GetFromJsonAsync<CountriesNowCountriesResponse>(
                "https://countriesnow.space/api/v0.1/countries", ct);

            if (countriesResp == null || countriesResp.Error || countriesResp.Data == null || countriesResp.Data.Count == 0)
                return ApiResponse<ImportSummaryDto>.Fail("Failed to fetch countries from CountriesNow API.");

            var countryRepo = _uow.Repository<Country>();
            var provinceRepo = _uow.Repository<Province>();

            // Existing countries keyed by code to avoid duplicates
            var existing = await countryRepo.TableNoTracking
                .ToDictionaryAsync(c => c.Code ?? "", c => c.Id, ct);

            foreach (var item in countriesResp.Data.OrderBy(c => c.Country))
            {
                var code = item.Iso2 ?? string.Empty;
                var name = item.Country ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code)) continue;

                int countryId;

                if (existing.TryGetValue(code, out var existingId))
                {
                    countryId = existingId;
                    summary.Skipped++;
                }
                else
                {
                    var country = new Country { Code = code, Name = name };
                    await countryRepo.AddAsync(country, ct);
                    countryId = country.Id;
                    existing[code] = countryId;
                    summary.CountriesImported++;
                }

                // 2. Fetch provinces/states for this country from CountriesNow
                try
                {
                    var statesResp = await http.PostAsJsonAsync(
                        "https://countriesnow.space/api/v0.1/countries/states",
                        new CountriesNowStatesRequest { Country = name }, ct);

                    if (statesResp.IsSuccessStatusCode)
                    {
                        var statesData = await statesResp.Content
                            .ReadFromJsonAsync<CountriesNowStatesResponse>(cancellationToken: ct);

                        if (statesData?.Error == false && statesData.Data?.States != null)
                        {
                            // Existing provinces for this country
                            var existingProvinces = await provinceRepo.TableNoTracking
                                .Where(p => p.CountryId == countryId)
                                .Select(p => p.Name)
                                .ToListAsync(ct);

                            foreach (var state in statesData.Data.States
                                .Where(s => !string.IsNullOrWhiteSpace(s.Name)))
                            {
                                if (existingProvinces.Contains(state.Name)) continue;

                                await provinceRepo.AddAsync(new Province
                                {
                                    Name = state.Name,
                                    CountryId = countryId
                                }, ct);
                                summary.ProvincesImported++;
                            }
                        }
                    }
                }
                catch
                {
                    // Skip provinces for this country if API fails — don't abort the whole import
                }
            }

            summary.Message = $"Import complete. " +
                $"{summary.CountriesImported} countries, " +
                $"{summary.ProvincesImported} provinces imported. " +
                $"{summary.Skipped} countries already existed.";

            return ApiResponse<ImportSummaryDto>.Ok(summary);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DTOs for CountriesNow API
    // ─────────────────────────────────────────────────────────────────────────

    file class CountriesNowCountriesResponse
    {
        [JsonPropertyName("error")]
        public bool Error { get; set; }

        [JsonPropertyName("data")]
        public List<CountriesNowCountryItem>? Data { get; set; }
    }

    file class CountriesNowCountryItem
    {
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("iso2")]
        public string? Iso2 { get; set; }
    }

    file class CountriesNowStatesRequest
    {
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
    }

    file class CountriesNowStatesResponse
    {
        [JsonPropertyName("error")]
        public bool Error { get; set; }

        [JsonPropertyName("data")]
        public CountriesNowStatesData? Data { get; set; }
    }

    file class CountriesNowStatesData
    {
        [JsonPropertyName("states")]
        public List<CountriesNowState>? States { get; set; }
    }

    file class CountriesNowState
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    // CountriesNow API — cities
    file class CountriesNowCitiesResponse
    {
        [JsonPropertyName("error")]
        public bool Error { get; set; }

        [JsonPropertyName("data")]
        public List<string>? Data { get; set; }
    }

    public class ImportCitiesByCountryHandler
        : IRequestHandler<ImportCitiesByCountryQuery, ApiResponse<ImportSummaryDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _httpFactory;

        public ImportCitiesByCountryHandler(IUnitOfWork uow, IHttpClientFactory httpFactory)
        {
            _uow = uow;
            _httpFactory = httpFactory;
        }

        public async Task<ApiResponse<ImportSummaryDto>> Handle(
            ImportCitiesByCountryQuery request, CancellationToken ct)
        {
            var summary = new ImportSummaryDto();
            var http = _httpFactory.CreateClient("GeoClient");

            var country = await _uow.Repository<Country>().TableNoTracking
                .FirstOrDefaultAsync(c => c.Code == request.Iso2.ToUpper(), ct);

            if (country == null)
                return ApiResponse<ImportSummaryDto>.Fail($"Country with ISO2 '{request.Iso2}' not found in DB. Import countries first.");

            summary = await ImportCitiesForCountry(country, http, ct);
            summary.Message = $"Cities import complete for {country.Name}. " +
                $"{summary.CitiesImported} cities imported, {summary.Skipped} skipped.";

            return ApiResponse<ImportSummaryDto>.Ok(summary);
        }

        internal async Task<ImportSummaryDto> ImportCitiesForCountry(
            Country country, HttpClient http, CancellationToken ct)
        {
            var summary = new ImportSummaryDto();

            try
            {
                var resp = await http.PostAsJsonAsync(
                    "https://countriesnow.space/api/v0.1/countries/cities",
                    new CountriesNowStatesRequest { Country = country.Name ?? "" }, ct);

                if (!resp.IsSuccessStatusCode) return summary;

                var data = await resp.Content
                    .ReadFromJsonAsync<CountriesNowCitiesResponse>(cancellationToken: ct);

                if (data?.Error == true || data?.Data == null) return summary;

                // Get all provinces for this country
                var provinces = await _uow.Repository<Province>().TableNoTracking
                    .Where(p => p.CountryId == country.Id)
                    .ToListAsync(ct);

                // Use first province as default bucket, or create a generic one
                Province? defaultProvince = provinces.FirstOrDefault();
                if (defaultProvince == null)
                {
                    defaultProvince = new Province { Name = country.Name, CountryId = country.Id };
                    await _uow.Repository<Province>().AddAsync(defaultProvince, ct);
                    summary.ProvincesImported++;
                }

                // Existing city names to skip duplicates
                var existingCities = await _uow.Repository<City>().TableNoTracking
                    .Where(c => c.Province!.CountryId == country.Id)
                    .Select(c => c.Name)
                    .ToListAsync(ct);

                foreach (var cityName in data.Data.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    if (existingCities.Contains(cityName))
                    {
                        summary.Skipped++;
                        continue;
                    }

                    await _uow.Repository<City>().AddAsync(new City
                    {
                        Name = cityName,
                        ProvinceId = defaultProvince.Id
                    }, ct);
                    summary.CitiesImported++;
                }
            }
            catch { /* skip on error */ }

            return summary;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Handler: Import cities for ALL countries in DB
    // ─────────────────────────────────────────────────────────────────────────

    public class ImportAllCitiesHandler
        : IRequestHandler<ImportAllCitiesQuery, ApiResponse<ImportSummaryDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _httpFactory;

        public ImportAllCitiesHandler(IUnitOfWork uow, IHttpClientFactory httpFactory)
        {
            _uow = uow;
            _httpFactory = httpFactory;
        }

        public async Task<ApiResponse<ImportSummaryDto>> Handle(
            ImportAllCitiesQuery request, CancellationToken ct)
        {
            var total = new ImportSummaryDto();
            var http = _httpFactory.CreateClient("GeoClient");

            var countries = await _uow.Repository<Country>().TableNoTracking.ToListAsync(ct);
            var helper = new ImportCitiesByCountryHandler(_uow, _httpFactory);

            foreach (var country in countries)
            {
                var result = await helper.ImportCitiesForCountry(country, http, ct);
                total.CitiesImported += result.CitiesImported;
                total.ProvincesImported += result.ProvincesImported;
                total.Skipped += result.Skipped;
            }

            total.Message = $"All cities import complete. " +
                $"{total.CitiesImported} cities, {total.ProvincesImported} provinces imported. " +
                $"{total.Skipped} skipped.";

            return ApiResponse<ImportSummaryDto>.Ok(total);
        }
    }




















}
