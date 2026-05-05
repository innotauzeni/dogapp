using Dog.Application.Common;
using Dog.Application.Features.Invoices.DTOs;
using MediatR;

namespace Dog.Application.Features.Invoices.Commands
{
    public record CreateInvoiceCommand(CreateInvoiceDto Dto) : IRequest<ApiResponse<InvoiceDto>>;
    public record UpdateInvoiceCommand(UpdateInvoiceDto Dto) : IRequest<ApiResponse<InvoiceDto>>;
    public record DeleteInvoiceCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record SendInvoiceCommand(int Id) : IRequest<ApiResponse<InvoiceDto>>;
    public record CancelInvoiceCommand(int Id) : IRequest<ApiResponse<InvoiceDto>>;

    // Record a direct payment (cash/ecocash/bank transfer)
    public record RecordPaymentCommand(RecordPaymentDto Dto) : IRequest<ApiResponse<ReceiptDto>>;

    // Submit bank payment with Proof of Payment upload
    public record SubmitBankPaymentCommand(SubmitBankPaymentDto Dto) : IRequest<ApiResponse<ProofOfPaymentDto>>;

    // Finance claims a bank transaction → creates suspense
    public record ClaimBankTransactionCommand(ClaimBankTransactionDto Dto) : IRequest<ApiResponse<SuspenseDto>>;

    // Apply suspense to invoice → generate receipt + mark suspense UTILIZED
    public record ApplySuspenseCommand(ApplySuspenseDto Dto) : IRequest<ApiResponse<ReceiptDto>>;

    // Get proof of payments for a company
    public record GetProofOfPaymentsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<ProofOfPaymentDto>>>;
    public record GetProofOfPaymentsByInvoiceQuery(int InvoiceId) : IRequest<ApiResponse<List<ProofOfPaymentDto>>>;
}
