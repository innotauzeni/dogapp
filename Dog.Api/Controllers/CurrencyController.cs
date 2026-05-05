using Dog.Application.Authorization;
using Dog.Application.Features.Currencies.Commands;
using Dog.Application.Features.Currencies.DTOs;
using Dog.Application.Features.Currencies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CurrencyController(IMediator mediator) => _mediator = mediator;

        [HttpGet("currencies")]
        [HasPermission("currency.view")]
        public async Task<IActionResult> GetCurrencies(CancellationToken ct)
           => Ok(await _mediator.Send(new GetAllCurrenciesQuery(), ct));


        [HttpGet("GetPubicCurrencies")]

        [AllowAnonymous]
        public async Task<IActionResult> GetPubicCurrencies(CancellationToken ct)
          => Ok(await _mediator.Send(new GetAllCurrenciesQuery(), ct));


        [HttpPost("CreateCurrency")]
        [HasPermission("currency.create")]
        public async Task<IActionResult> CreateCurrency([FromBody] CreateCurrencyDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateCurrencyCommand(dto), ct));


        [HttpPut("currencies/{id:int}")]
        [HasPermission("currency.edit")]
        public async Task<IActionResult> UpdateCurrency(int id, [FromBody] CreateCurrencyDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateCurrencyCommand(id,dto), ct));
        }

        [HttpDelete("currencies/{id:int}")]
        [HasPermission("currency.delete")]
        public async Task<IActionResult> DeleteCurrency(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCurrencyCommand(id), ct));


        [HttpPost("currencies/{id:int}/toggle")]
        [HasPermission("currency.edit")]
        public async Task<IActionResult> ToggleCurrency(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleCurrencyCommand(id), ct));
    }



}
