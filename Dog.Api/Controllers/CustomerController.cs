using Dog.Application.Authorization;
using Dog.Application.Features.Customers.Commands;
using Dog.Application.Features.Customers.DTOs;
using Dog.Application.Features.Customers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomerController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("customer.view")]
        public async Task<IActionResult> GetCustomers(int CompanyId,CancellationToken ct)
           => Ok(await _mediator.Send(new GetCustomerByCompanyQuery(CompanyId), ct));

        [HttpGet("GetCustomerById")]
        [HasPermission("customer.view")]
        public async Task<IActionResult> GetCustomerById(int Id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCustomerByIdQuery(Id), ct));

        [HttpGet("GetPubicCustomers")]
        public async Task<IActionResult> GetPubicCustomers(CancellationToken ct)
          => Ok(await _mediator.Send(new GetCustomersQuery(), ct));


        [HttpPost("CreateCustomer")]
        [HasPermission("customer.create")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateCustomerCommand(dto), ct));


        [HttpPut("Customer/{id:int}")]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateCustomerCommand(id, dto), ct));
        }

        [HttpDelete("Customer/{id:int}")]
        [HasPermission("customer.delete")]
        public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCustomerCommand(id), ct));


        [HttpPost("Customer/{id:int}/toggle")]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> ToggleCCustomer(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleCustomerCommand(id), ct));
    }



}
