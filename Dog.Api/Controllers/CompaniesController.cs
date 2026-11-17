using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Authorization;
using Dog.Application.Features.Companies.Commands;
using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.Companies.Queries;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CompaniesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("companies.view")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _mediator.Send(new GetAllCompaniesQuery(), ct));

        [HttpGet("{id:int}")]
        [HasPermission("companies.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCompanyByIdQuery(id), ct));

        [HttpGet("{id:int}/branches")]
        [HasPermission("companies.view")]
        public async Task<IActionResult> GetBranches(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetBranchesByCompanyQuery(id), ct));

        [HttpPost]
        [HasPermission("companies.create")]
        public async Task<IActionResult> Create([FromBody] CreateCompanyDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateCompanyCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("companies.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateCompanyCommand(dto), ct));
        }

        [HttpDelete("{id:int}")]
        [HasPermission("companies.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCompanyCommand(id), ct));

        [HttpPost("{id:int}/toggle-status")]
        [HasPermission("companies.edit")]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleCompanyStatusCommand(id), ct));

        /// Lets a company admin re-brand the app's accent color without touching
        /// the rest of the company record. Deliberately narrower than the general
        /// companies.edit permission — only Admin/SystemOwner, not every head-office
        /// user who happens to have companies.edit.
        [HttpPut("{id:int}/brand-color")]
        [HasPermission("companies.edit")]
        [Authorize(Roles = "Admin,SystemOwner")]
        public async Task<IActionResult> UpdateBrandColor(int id, [FromBody] UpdateBrandColorDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new UpdateCompanyBrandColorCommand(id, dto.BrandColor), ct));
    }
}