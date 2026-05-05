using Dog.Application.Common;
using Dog.Application.Features.BankTransactions.DTOs;
using MediatR;

namespace Dog.Application.Features.BankTransactions.Commands
{
    public record CreateBankTransactionCommand(CreateBankTransactionDto Dto) : IRequest<ApiResponse<BankTransactionDto>>;
    public record UpdateBankTransactionCommand(UpdateBankTransactionDto Dto) : IRequest<ApiResponse<BankTransactionDto>>;
    public record DeleteBankTransactionCommand(int Id) : IRequest<ApiResponse<bool>>;
}
