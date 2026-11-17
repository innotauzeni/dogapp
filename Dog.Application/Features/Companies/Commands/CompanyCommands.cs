using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Companies.DTOs;

namespace Dog.Application.Features.Companies.Commands
{
    public record CreateCompanyCommand(CreateCompanyDto Dto) : IRequest<ApiResponse<CompanyDto>>;
    public record UpdateCompanyCommand(UpdateCompanyDto Dto) : IRequest<ApiResponse<CompanyDto>>;
    public record DeleteCompanyCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record ToggleCompanyStatusCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record UpdateCompanyBrandColorCommand(int Id, string? BrandColor) : IRequest<ApiResponse<string?>>;
}
