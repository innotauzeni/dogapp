using Dog.Application.Common;
using Dog.Application.Features.Expenses.DTOs;
using MediatR;

namespace Dog.Application.Features.Expenses.Commands
{
    public record CreateExpenseCommand(CreateExpenseDto Dto) : IRequest<ApiResponse<ExpenseDto>>;
    public record UpdateExpenseCommand(int Id, UpdateExpenseDto Dto) : IRequest<ApiResponse<ExpenseDto>>;
    public record DeleteExpenseCommand(int Id) : IRequest<ApiResponse<bool>>;
}
