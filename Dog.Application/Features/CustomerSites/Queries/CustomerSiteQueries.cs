using Dog.Application.Common;
using Dog.Application.Features.CustomerSites.DTOs;
using MediatR;

namespace Dog.Application.Features.CustomerSites.Queries
{
    public record GetSitesByCustomerQuery(int CustomerId) : IRequest<ApiResponse<List<CustomerSiteDto>>>;
    public record GetCustomerSiteByIdQuery(int Id) : IRequest<ApiResponse<CustomerSiteDto>>;
}
