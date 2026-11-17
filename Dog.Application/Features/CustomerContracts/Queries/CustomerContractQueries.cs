using Dog.Application.Common;
using Dog.Application.Features.CustomerContracts.DTOs;
using MediatR;

namespace Dog.Application.Features.CustomerContracts.Queries
{
    public record GetContractsByCustomerQuery(int CustomerId) : IRequest<ApiResponse<List<CustomerContractDto>>>;
    public record GetCustomerContractByIdQuery(int Id) : IRequest<ApiResponse<CustomerContractDto>>;
}
