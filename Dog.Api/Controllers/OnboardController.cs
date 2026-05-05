using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Features.Countries.Queries;
using Dog.Application.Features.Currencies.Queries;
using Dog.Application.Features.Onboarding.Commands;
using Dog.Application.Features.Onboarding.DTOs;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class OnboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OnboardController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] OnboardRequestDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new OnboardCompanyCommand(dto), ct));

        // ── Public lookups needed by the signup form ──────────────────────
        [HttpGet("countries")]
        public async Task<IActionResult> Countries(CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllCountriesQuery(), ct));

        [HttpGet("provinces/{countryId:int}")]
        public async Task<IActionResult> Provinces(int countryId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetProvincesByCountryQuery(countryId), ct));

        [HttpGet("cities/{provinceId:int}")]
        public async Task<IActionResult> Cities(int provinceId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCitiesByProvinceQuery(provinceId), ct));

        [HttpGet("currencies")]
        public async Task<IActionResult> Currencies(CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllCurrenciesQuery(), ct));
    }
}
