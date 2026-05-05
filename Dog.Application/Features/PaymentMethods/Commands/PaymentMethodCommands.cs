using Dog.Application.Common;
using Dog.Application.Features.PaymentMethods.DTOs;
using MediatR;

namespace Dog.Application.Features.PaymentMethods.Commands
{
    // Payment Method
    public record CreatePaymentMethodCommand(CreatePaymentMethodDto Dto) : IRequest<ApiResponse<PaymentMethodDto>>;
    public record UpdatePaymentMethodCommand(UpdatePaymentMethodDto Dto) : IRequest<ApiResponse<PaymentMethodDto>>;
    public record DeletePaymentMethodCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record TogglePaymentMethodCommand(int Id) : IRequest<ApiResponse<bool>>;
}
