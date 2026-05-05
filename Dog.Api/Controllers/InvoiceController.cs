using Dog.Application.Authorization;
using Dog.Application.Features.Invoices.Commands;
using Dog.Application.Features.Invoices.DTOs;
using Dog.Application.Features.Invoices.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IMediator _mediator;
        public InvoiceController(IMediator mediator) => _mediator = mediator;

        // ── INVOICES ──────────────────────────────────────────────────────────

        [HttpGet]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetInvoices(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetInvoicesByCompanyQuery(CompanyId), ct));

        [HttpGet("{id:int}")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetInvoiceByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("invoice.create")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateInvoiceCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("invoice.edit")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] UpdateInvoiceDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateInvoiceCommand(dto), ct));
        }

        [HttpDelete("{id:int}")]
        [HasPermission("invoice.delete")]
        public async Task<IActionResult> DeleteInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteInvoiceCommand(id), ct));

        [HttpPost("{id:int}/send")]
        [HasPermission("invoice.edit")]
        public async Task<IActionResult> SendInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new SendInvoiceCommand(id), ct));

        [HttpPost("{id:int}/cancel")]
        [HasPermission("invoice.edit")]
        public async Task<IActionResult> CancelInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new CancelInvoiceCommand(id), ct));

        // ── PAYMENTS ──────────────────────────────────────────────────────────

        /// <summary>Record a direct payment (Cash / EcoCash / Bank Transfer)</summary>
        [HttpPost("pay")]
        [HasPermission("invoice.pay")]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new RecordPaymentCommand(dto), ct));

        /// <summary>Submit bank payment with Proof of Payment file upload</summary>
        [HttpPost("submit-bank-payment")]
        [HasPermission("invoice.pay")]
        public async Task<IActionResult> SubmitBankPayment([FromBody] SubmitBankPaymentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new SubmitBankPaymentCommand(dto), ct));

        // ── PROOF OF PAYMENT ──────────────────────────────────────────────────

        [HttpGet("proof-of-payments")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetProofOfPayments(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetProofOfPaymentsByCompanyQuery(CompanyId), ct));

        [HttpGet("{id:int}/proof-of-payments")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetProofOfPaymentsByInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetProofOfPaymentsByInvoiceQuery(id), ct));

        // ── BANK CLAIM → SUSPENSE ─────────────────────────────────────────────

        /// <summary>Finance claims a bank transaction → creates suspense entry</summary>
        [HttpPost("claim-bank")]
        [HasPermission("invoice.pay")]
        public async Task<IActionResult> ClaimBankTransaction([FromBody] ClaimBankTransactionDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new ClaimBankTransactionCommand(dto), ct));

        // ── APPLY SUSPENSE → RECEIPT ──────────────────────────────────────────

        /// <summary>Apply suspense to invoice → generate receipt + mark suspense UTILIZED</summary>
        [HttpPost("apply-suspense")]
        [HasPermission("invoice.pay")]
        public async Task<IActionResult> ApplySuspense([FromBody] ApplySuspenseDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new ApplySuspenseCommand(dto), ct));

        // ── SUSPENSE ──────────────────────────────────────────────────────────

        [HttpGet("suspense")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetSuspense(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetSuspenseByCompanyQuery(CompanyId), ct));

        // ── RECEIPTS ──────────────────────────────────────────────────────────

        [HttpGet("receipts")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetReceipts(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetReceiptsByCompanyQuery(CompanyId), ct));

        [HttpGet("{id:int}/receipts")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetReceiptsByInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetReceiptsByInvoiceQuery(id), ct));
    }
}
