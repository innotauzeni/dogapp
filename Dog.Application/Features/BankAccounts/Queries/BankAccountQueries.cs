using Dog.Application.Common;
using Dog.Application.Features.BankAccounts.DTOs;
using MediatR;

namespace Dog.Application.Features.Banks.Queries
{
    public record GetBankAccountByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<BankAccountListDto>>>;
    public record GetBankAccountByBankQuery(int BankId) : IRequest<ApiResponse<List<BankAccountListDto>>>;
}
