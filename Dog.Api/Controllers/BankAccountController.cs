using Dog.Application.Authorization;
using Dog.Application.Features.BankAccountAccounts.Commands;
using Dog.Application.Features.BankAccounts.DTOs;
using Dog.Application.Features.Banks.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankAccountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BankAccountController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("bank.view")]
        public async Task<IActionResult> GetBankAccounts(int BankId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetBankByCompanyQuery(BankId), ct));

        [HttpGet("GetAccountByCompany")]
        [HasPermission("bank.view")]
        public async Task<IActionResult> GetAccountByCompany(int CompanyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetBankAccountByCompanyQuery(CompanyId), ct));

        


        [HttpPost("CreateBankAccount")]
        [HasPermission("bank.create")]
        public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateBankAccountCommand(dto), ct));


        [HttpPut("BankAccount/{id:int}")]
        [HasPermission("bank.edit")]
        public async Task<IActionResult> UpdateBankAccount(int id, [FromBody] CreateBankAccountDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateBankAccountCommand(id, dto), ct));
        }

        [HttpDelete("BankAccount/{id:int}")]
        [HasPermission("bank.delete")]
        public async Task<IActionResult> DeleteBankAccount(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteBankAccountCommand(id), ct));


        [HttpPost("BankAccount/{id:int}/toggle")]
        [HasPermission("bank.edit")]
        public async Task<IActionResult> ToggleCBank(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleBankAccountCommand(id), ct));
    }


}
