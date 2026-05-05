using Dog.Application.Common;
using Dog.Application.Features.BankAccounts.DTOs;
using MediatR;

namespace Dog.Application.Features.BankAccountAccounts.Commands
{
    public record CreateBankAccountCommand(CreateBankAccountDto Dto) : IRequest<ApiResponse<BankAccountDto>>;
    public record UpdateBankAccountCommand(int Id, CreateBankAccountDto Dto) : IRequest<ApiResponse<BankAccountDto>>;
    public record DeleteBankAccountCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record ToggleBankAccountCommand(int Id) : IRequest<ApiResponse<bool>>;
}
