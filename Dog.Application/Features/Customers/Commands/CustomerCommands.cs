using Dog.Application.Common;
using Dog.Application.Features.Customers.DTOs;
using MediatR;

namespace Dog.Application.Features.Customers.Commands
{
    public record CreateCustomerCommand(CreateCustomerDto Dto) : IRequest<ApiResponse<CustomerDto>>;
    public record UpdateCustomerCommand(int Id, CreateCustomerDto Dto) : IRequest<ApiResponse<CustomerDto>>;
    public record DeleteCustomerCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record ToggleCustomerCommand(int Id) : IRequest<ApiResponse<bool>>;
}
