using Dog.Application.Common;
using Dog.Application.Features.Banks.DTOs;
using MediatR;

namespace Dog.Application.Features.Banks.Commands
{
    public record CreateBankCommand(CreateBankDto Dto) : IRequest<ApiResponse<BankDto>>;
    public record UpdateBankCommand(int Id, CreateBankDto Dto) : IRequest<ApiResponse<BankDto>>;
    public record DeleteBankCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record ToggleBankCommand(int Id) : IRequest<ApiResponse<bool>>;
}
