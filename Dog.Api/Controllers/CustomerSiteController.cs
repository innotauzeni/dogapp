using Dog.Application.Authorization;
using Dog.Application.Features.CustomerSites.Commands;
using Dog.Application.Features.CustomerSites.DTOs;
using Dog.Application.Features.CustomerSites.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerSiteController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CustomerSiteController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("customer.view")]
        public async Task<IActionResult> GetByCustomer(int customerId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetSitesByCustomerQuery(customerId), ct));

        [HttpGet("{id:int}")]
        [HasPermission("customer.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCustomerSiteByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> Create([FromBody] CreateCustomerSiteDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateCustomerSiteCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateCustomerSiteDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new UpdateCustomerSiteCommand(id, dto), ct));

        [HttpDelete("{id:int}")]
        [HasPermission("customer.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCustomerSiteCommand(id), ct));
    }
}
