using Dog.Application.Authorization;
using Dog.Application.Features.CustomerCompanies;
using MediatR;
using Dog.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dog.Api.Controllers
{
    /// <summary>Client companies that are billed (their contact people are Customers).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerCompanyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;
        public CustomerCompanyController(IMediator mediator, IUnitOfWork uow) { _mediator = mediator; _uow = uow; }

        private async Task<int> ScopedAsync(int requested, CancellationToken ct)
        {
            int.TryParse(User.FindFirstValue("companyId"), out var own);
            if (requested <= 0 || requested == own) return own;
            var allowed = await Dog.Application.Common.CompanyScopeResolver.ResolveAsync(_uow, own, ct);
            return allowed.Contains(requested) ? requested : own;
        }


        [HttpGet]
        [HasPermission("customercompany.view")]
        public async Task<IActionResult> List(int CompanyId, string? search, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCustomerCompaniesQuery(await ScopedAsync(CompanyId, ct), search), ct));

        // Used by the invoice builder and the customer form (needs only customer/invoice access, not the company screen).
        [HttpGet("Lookup")]
        public async Task<IActionResult> Lookup(int CompanyId, string? search, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCustomerCompaniesQuery(await ScopedAsync(CompanyId, ct), search), ct));

        [HttpGet("{id:int}")]
        [HasPermission("customercompany.view")]
        public async Task<IActionResult> Profile(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCustomerCompanyProfileQuery(id), ct));

        [HttpPost]
        [HasPermission("customercompany.create")]
        public async Task<IActionResult> Create([FromBody] SaveCustomerCompanyDto dto, CancellationToken ct)
        {
            dto.Id = null;
            dto.CompanyId = await ScopedAsync(dto.CompanyId, ct);
            return Ok(await _mediator.Send(new SaveCustomerCompanyCommand(dto), ct));
        }

        [HttpPut("{id:int}")]
        [HasPermission("customercompany.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveCustomerCompanyDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new SaveCustomerCompanyCommand(dto), ct));
        }

        [HttpDelete("{id:int}")]
        [HasPermission("customercompany.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCustomerCompanyCommand(id), ct));
    }
}
