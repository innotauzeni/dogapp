using Dog.Application.Common;
using Dog.Application.Features.CustomerContracts.DTOs;
using MediatR;

namespace Dog.Application.Features.CustomerContracts.Commands
{
    public record CreateCustomerContractCommand(CreateCustomerContractDto Dto) : IRequest<ApiResponse<CustomerContractDto>>;
    public record UpdateCustomerContractCommand(int Id, CreateCustomerContractDto Dto) : IRequest<ApiResponse<CustomerContractDto>>;
    public record DeleteCustomerContractCommand(int Id) : IRequest<ApiResponse<bool>>;
}
