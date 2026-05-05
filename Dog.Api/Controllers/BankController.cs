using Dog.Application.Authorization;
using Dog.Application.Features.Banks.Commands;
using Dog.Application.Features.Banks.DTOs;
using Dog.Application.Features.Banks.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BankController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("bank.view")]
        public async Task<IActionResult> GetBanks(int CompanyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetBankByCompanyQuery(CompanyId), ct));

       

        [HttpGet("GetPubicBanks")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPubicBanks(CancellationToken ct)
          => Ok(await _mediator.Send(new GetBanksQuery(), ct));


        [HttpPost("CreateBank")]
        [HasPermission("bank.create")]
        public async Task<IActionResult> CreateBank([FromBody] CreateBankDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateBankCommand(dto), ct));


        [HttpPut("Bank/{id:int}")]
        [HasPermission("bank.edit")]
        public async Task<IActionResult> UpdateBank(int id, [FromBody] CreateBankDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateBankCommand(id, dto), ct));
        }

        [HttpDelete("Bank/{id:int}")]
        [HasPermission("bank.delete")]
        public async Task<IActionResult> DeleteBank(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteBankCommand(id), ct));


        [HttpPost("Bank/{id:int}/toggle")]
        [HasPermission("bank.edit")]
        public async Task<IActionResult> ToggleCBank(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleBankCommand(id), ct));
    }

}
