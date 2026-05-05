using Dog.Application.Authorization;
using Dog.Application.Features.ExchangeRates.Commands;
using Dog.Application.Features.ExchangeRates.DTOs;
using Dog.Application.Features.ExchangeRates.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeRateController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExchangeRateController(IMediator mediator) => _mediator = mediator;

        [HttpGet("exchange-rates/{companyId:int}")]
        [HasPermission("exchangerate.view")]
        public async Task<IActionResult> GetExchangeRates(int companyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetExchangeRatesByCompanyQuery((int)companyId), ct));


        [HttpPost("exchange-rates")]
        [HasPermission("exchangerate.create")]
        public async Task<IActionResult> Create([FromBody] CreateExchangeRateDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateExchangeRateCommand(dto), ct));


        [HttpPut("exchange-rates/{id:int}")]
        [HasPermission("exchangerate.edit")]
        public async Task<IActionResult> UpdateExchangeRate(int id, [FromBody] UpdateExchangeRateDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateExchangeRateCommand(dto), ct));
        }

        [HttpDelete("exchange-rates/{id:int}")]
        [HasPermission("exchangerate.delete")]
        public async Task<IActionResult> DeleteExchangeRate(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteExchangeRateCommand(id), ct));


        
    }

}
