using Dog.Application.Authorization;
using Dog.Application.Features.Countries.Commands;
using Dog.Application.Features.Countries.DTOs;
using Dog.Application.Features.Countries.Queries;
using Dog.Application.Features.Currencies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CountryController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetCountries")]
        [HasPermission("country.view")]
        public async Task<IActionResult> GetCountries(CancellationToken ct)
           => Ok(await _mediator.Send(new GetAllCountriesQuery(), ct));


        [HttpGet("GetPublicCountries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicCountries(CancellationToken ct)
          => Ok(await _mediator.Send(new GetAllCountriesQuery(), ct));


        [HttpPost("CreateCountry")]
        [HasPermission("country.create")]
        public async Task<IActionResult> CreateCountry([FromBody] CreateCountryDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateCountryCommand(dto), ct));


        [HttpPut("UpdateCountry/{id:int}")]
        [HasPermission("country.edit")]
        public async Task<IActionResult> UpdateCountry(int id, [FromBody] CreateCountryDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateCountryCommand(id, dto), ct));
        }

        [HttpDelete("DeleteCountry/{id:int}")]
        [HasPermission("country.delete")]
        public async Task<IActionResult> DeleteCountry(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCountryCommand(id), ct));

        // ─────────────────────────────────────────────────────────────────────
        // GEO IMPORT ENDPOINTS (Admin only)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Imports all countries and their provinces/states from REST Countries API.
        /// Run this ONCE to seed the database.
        /// </summary>
        [HttpPost("ImportCountriesFromWeb")]
        [HasPermission("country.create")]
        public async Task<IActionResult> ImportCountriesFromWeb(CancellationToken ct)
            => Ok(await _mediator.Send(new ImportCountriesFromWebQuery(), ct));

        /// <summary>
        /// Imports cities for a specific country (by ISO2 code, e.g., "ZW" for Zimbabwe).
        /// </summary>
        [HttpPost("ImportCitiesByCountry/{iso2}")]
        [HasPermission("country.create")]
        public async Task<IActionResult> ImportCitiesByCountry(string iso2, CancellationToken ct)
            => Ok(await _mediator.Send(new ImportCitiesByCountryQuery(iso2), ct));

        /// <summary>
        /// Imports cities for ALL countries already in the database.
        /// WARNING: This can take several minutes. Run once after importing countries.
        /// </summary>
        [HttpPost("ImportAllCities")]
        [HasPermission("country.create")]
        public async Task<IActionResult> ImportAllCities(CancellationToken ct)
            => Ok(await _mediator.Send(new ImportAllCitiesQuery(), ct));

        [HttpGet("GetProvincesByCountry/{countryId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProvincesByCountry(int countryId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetProvincesByCountryQuery(countryId), ct));

        [HttpGet("GetCitiesByProvince/{provinceId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCitiesByProvince(int provinceId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCitiesByProvinceQuery(provinceId), ct));
    }
}
