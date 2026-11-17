using Dog.Application.Authorization;
using Dog.Application.Features.CustomerContracts.Commands;
using Dog.Application.Features.CustomerContracts.DTOs;
using Dog.Application.Features.CustomerContracts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerContractController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CustomerContractController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("customer.view")]
        public async Task<IActionResult> GetByCustomer(int customerId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetContractsByCustomerQuery(customerId), ct));

        [HttpGet("{id:int}")]
        [HasPermission("customer.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCustomerContractByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> Create([FromBody] CreateCustomerContractDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateCustomerContractCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCustomerContractDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new UpdateCustomerContractCommand(id, dto), ct));

        [HttpDelete("{id:int}")]
        [HasPermission("customer.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCustomerContractCommand(id), ct));
    }
}
