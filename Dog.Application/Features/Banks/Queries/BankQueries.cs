using Dog.Application.Common;
using Dog.Application.Features.Banks.DTOs;
using MediatR;

namespace Dog.Application.Features.Banks.Queries
{
    public record GetBankByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<BankDto>>>;
    public record GetBanksQuery() : IRequest<ApiResponse<List<BankDto>>>;
}
