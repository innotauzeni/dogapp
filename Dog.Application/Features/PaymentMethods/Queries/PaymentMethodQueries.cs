using Dog.Application.Common;
using Dog.Application.Features.PaymentMethods.DTOs;
using MediatR;

namespace Dog.Application.Features.PaymentMethods.Queries
{
    public record GetPaymentMethodsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<PaymentMethodDto>>>;
}
