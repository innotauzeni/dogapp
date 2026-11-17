using Dog.Application.Common;
using Dog.Application.Features.CustomerSites.DTOs;
using MediatR;

namespace Dog.Application.Features.CustomerSites.Commands
{
    public record CreateCustomerSiteCommand(CreateCustomerSiteDto Dto) : IRequest<ApiResponse<CustomerSiteDto>>;
    public record UpdateCustomerSiteCommand(int Id, CreateCustomerSiteDto Dto) : IRequest<ApiResponse<CustomerSiteDto>>;
    public record DeleteCustomerSiteCommand(int Id) : IRequest<ApiResponse<bool>>;
}
