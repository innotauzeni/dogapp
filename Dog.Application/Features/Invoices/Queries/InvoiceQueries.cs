using Dog.Application.Common;
using Dog.Application.Features.Invoices.DTOs;
using MediatR;

namespace Dog.Application.Features.Invoices.Queries
{
    public record GetInvoicesByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<InvoiceDto>>>;
    public record GetInvoiceByIdQuery(int Id) : IRequest<ApiResponse<InvoiceDto>>;
    public record GetSuspenseByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<SuspenseDto>>>;
    public record GetReceiptsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<ReceiptDto>>>;
    public record GetReceiptsByInvoiceQuery(int InvoiceId) : IRequest<ApiResponse<List<ReceiptDto>>>;
}
