using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Companies.DTOs;

namespace Dog.Application.Features.Companies.Queries
{
    public record GetAllCompaniesQuery : IRequest<ApiResponse<List<CompanyDto>>>;
    public record GetCompanyByIdQuery(int Id) : IRequest<ApiResponse<CompanyDto>>;
    public record GetBranchesByCompanyQuery(int ParentId) : IRequest<ApiResponse<List<CompanyDto>>>;
}
