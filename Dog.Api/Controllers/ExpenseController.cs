using Dog.Application.Authorization;
using Dog.Application.Features.Expenses.Commands;
using Dog.Application.Features.Expenses.DTOs;
using Dog.Application.Features.Expenses.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ExpenseController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("expense.view")]
        public async Task<IActionResult> GetExpenses(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetExpensesByCompanyQuery(CompanyId), ct));

        [HttpGet("{id:int}")]
        [HasPermission("expense.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetExpenseByIdQuery(id), ct));

        [HttpGet("total")]
        [HasPermission("expense.view")]
        public async Task<IActionResult> GetTotal(int CompanyId, DateTime? From, DateTime? To, CancellationToken ct)
            => Ok(await _mediator.Send(new GetExpensesTotalQuery(CompanyId, From, To), ct));

        [HttpPost]
        [HasPermission("expense.create")]
        public async Task<IActionResult> Create([FromBody] CreateExpenseDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateExpenseCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("expense.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new UpdateExpenseCommand(id, dto), ct));

        [HttpDelete("{id:int}")]
        [HasPermission("expense.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteExpenseCommand(id), ct));
    }
}
