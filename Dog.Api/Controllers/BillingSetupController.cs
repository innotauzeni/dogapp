using Dog.Application.Authorization;
using Dog.Application.Features.BillingSetup;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dog.Application.Contracts;
using System.Security.Claims;

namespace Dog.Api.Controllers
{
    /// <summary>Company billing profile (tax / VAT / fiscal), outgoing email account and the invoice item catalogue.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BillingSetupController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;
        public BillingSetupController(IMediator mediator, IUnitOfWork uow) { _mediator = mediator; _uow = uow; }

        private async Task<int> ScopedAsync(int requested, CancellationToken ct)
        {
            int.TryParse(User.FindFirstValue("companyId"), out var own);
            if (requested <= 0 || requested == own) return own;
            var allowed = await Dog.Application.Common.CompanyScopeResolver.ResolveAsync(_uow, own, ct);
            return allowed.Contains(requested) ? requested : own;
        }


        // ── Billing profile ──
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> Profile(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetBillingProfileQuery(await ScopedAsync(CompanyId, ct)), ct));

        [HttpPut("profile")]
        [HasPermission("billingsettings.edit")]
        public async Task<IActionResult> SaveProfile([FromBody] BillingProfileDto dto, CancellationToken ct)
        {
            dto.CompanyId = await ScopedAsync(dto.CompanyId, ct);
            return Ok(await _mediator.Send(new SaveBillingProfileCommand(dto), ct));
        }

        // ── Email settings ──
        [HttpGet("email")]
        [HasPermission("billingsettings.view")]
        public async Task<IActionResult> Email(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetEmailSettingQuery(await ScopedAsync(CompanyId, ct)), ct));

        [HttpPut("email")]
        [HasPermission("billingsettings.edit")]
        public async Task<IActionResult> SaveEmail([FromBody] EmailSettingDto dto, CancellationToken ct)
        {
            dto.CompanyId = await ScopedAsync(dto.CompanyId, ct);
            return Ok(await _mediator.Send(new SaveEmailSettingCommand(dto), ct));
        }

        public class TestEmailRequest { public int CompanyId { get; set; } public string ToAddress { get; set; } = string.Empty; }

        [HttpPost("email/test")]
        [HasPermission("billingsettings.edit")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest req, CancellationToken ct)
            => Ok(await _mediator.Send(new TestEmailSettingCommand(await ScopedAsync(req.CompanyId, ct), req.ToAddress, User.FindFirstValue(ClaimTypes.Name)), ct));

        [HttpGet("email/logs")]
        [HasPermission("billingsettings.view")]
        public async Task<IActionResult> EmailLogs(int CompanyId, string? relatedType, int? relatedId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetEmailLogsQuery(await ScopedAsync(CompanyId, ct), relatedType, relatedId), ct));

        // ── Invoice item catalogue (used by the invoice builder) ──
        [HttpGet("catalog")]
        [Authorize]
        public async Task<IActionResult> Catalog(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCatalogQuery(await ScopedAsync(CompanyId, ct)), ct));

        [HttpPost("catalog")]
        [HasPermission("billingsettings.edit")]
        public async Task<IActionResult> SaveCatalog([FromBody] CatalogItemDto dto, CancellationToken ct)
        {
            dto.CompanyId = await ScopedAsync(dto.CompanyId, ct);
            return Ok(await _mediator.Send(new SaveCatalogItemCommand(dto), ct));
        }

        [HttpDelete("catalog/{id:int}")]
        [HasPermission("billingsettings.edit")]
        public async Task<IActionResult> DeleteCatalog(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCatalogItemCommand(id), ct));

        // ── Everything a printed invoice needs besides the invoice (tax profile + bank accounts) ──
        [HttpGet("pdf-info")]
        [Authorize]
        public async Task<IActionResult> PdfInfo(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetPdfInfoQuery(await ScopedAsync(CompanyId, ct)), ct));
    }
}
