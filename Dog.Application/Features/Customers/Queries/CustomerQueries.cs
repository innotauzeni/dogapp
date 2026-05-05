using Dog.Application.Common;
using Dog.Application.Features.Customers.DTOs;
using MediatR;

namespace Dog.Application.Features.Customers.Queries
{
    public record GetCustomerByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<CustomerDto>>>;

    public record GetCustomersQuery() : IRequest<ApiResponse<List<CustomerDto>>>;
    public record GetCustomerByIdQuery(int Id) : IRequest<ApiResponse<CustomerDto>>;
}
