using Dog.Application.Common;
using Dog.Application.Features.BankTransactions.DTOs;
using MediatR;

namespace Dog.Application.Features.BankTransactions.Queries
{
    public record GetBankTransactionsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<BankTransactionListDto>>>;
}
