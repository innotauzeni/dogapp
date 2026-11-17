using Dog.Application.Authorization;
using Dog.Api.Services;
using Dog.Application.Common.Payments;
using Dog.Application.Features.Invoices.Billing;
using System.Security.Claims;
using Dog.Application.Features.Companies.Queries;
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
        private readonly BillingDocuments _docs;
        public InvoiceController(IMediator mediator, BillingDocuments docs) { _mediator = mediator; _docs = docs; }

        private PaymentActor Actor()
        {
            int.TryParse(User.FindFirstValue("companyId"), out var cid);
            return new PaymentActor(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", User.FindFirstValue(ClaimTypes.Name) ?? "", cid,
                User.Claims.Any(c => c.Type == "permission" && c.Value == "invoice.verifypayment"));
        }

        // ── INVOICES ──────────────────────────────────────────────────────────

        [HttpGet]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetInvoices(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetInvoicesByCompanyQuery(CompanyId), ct));

        [HttpGet("{id:int}")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetInvoice(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetInvoiceByIdQuery(id), ct));

        /// <summary>Professional invoice PDF (company letterhead, bill-to company, VAT, payments, bank details) for web + mobile.</summary>
        [HttpGet("{id:int}/pdf")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetInvoicePdf(int id, CancellationToken ct)
        {
            var built = await _docs.InvoicePdfAsync(id, ct);
            if (built == null) return NotFound("Invoice not found.");
            return File(built.Value.Pdf, "application/pdf", $"Invoice_{built.Value.Invoice.InvoiceNumber}.pdf");
        }

        /// <summary>Email the invoice PDF to the billed company through the company's own mail account.</summary>
        [HttpPost("{id:int}/email")]
        [HasPermission("invoice.email")]
        public async Task<IActionResult> EmailInvoice(int id, [FromBody] EmailInvoiceRequest req, CancellationToken ct)
        {
            var (ok, msg) = await _docs.EmailInvoiceAsync(id, req ?? new EmailInvoiceRequest(), User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindFirstValue(ClaimTypes.Name), ct);
            return Ok(new { success = ok, message = msg });
        }

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

        /// <summary>The payment methods and exactly which details each one must capture.</summary>
        [HttpGet("payment-methods")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> PaymentMethods(CancellationToken ct)
            => Ok(await _mediator.Send(new GetPaymentMethodsQuery(), ct));

        /// <summary>Record a payment (cash, EcoCash/OneMoney/InnBucks, bank transfer, card, cheque, other) with full trace details.</summary>
        [HttpPost("pay")]
        [HasPermission("invoice.pay")]
        public async Task<IActionResult> RecordPayment([FromBody] RecordInvoicePaymentDto dto, CancellationToken ct)
        {
            var actor = Actor();
            if (!actor.CanVerify) dto.VerifiedOnStatement = false;
            return Ok(await _mediator.Send(new RecordInvoicePaymentCommand(actor, dto), ct));
        }

        [HttpGet("payments")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> Payments(int CompanyId, int? invoiceId, string? status, CancellationToken ct)
            => Ok(await _mediator.Send(new GetInvoicePaymentsQuery(CompanyId, invoiceId, status), ct));

        [HttpPost("payments/{paymentId:int}/confirm")]
        [HasPermission("invoice.verifypayment")]
        public async Task<IActionResult> ConfirmPayment(int paymentId, [FromBody] ReviewPaymentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new ConfirmInvoicePaymentCommand(Actor(), paymentId, dto?.StatementReference), ct));

        [HttpPost("payments/{paymentId:int}/reject")]
        [HasPermission("invoice.verifypayment")]
        public async Task<IActionResult> RejectPayment(int paymentId, [FromBody] ReviewPaymentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new RejectInvoicePaymentCommand(Actor(), paymentId, dto?.Reason), ct));

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
