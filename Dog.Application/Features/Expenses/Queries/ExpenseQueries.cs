using Dog.Application.Common;
using Dog.Application.Features.Expenses.DTOs;
using MediatR;

namespace Dog.Application.Features.Expenses.Queries
{
    public record GetExpensesByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<ExpenseDto>>>;
    public record GetExpenseByIdQuery(int Id) : IRequest<ApiResponse<ExpenseDto>>;

    /// <summary>Total expenses for a company, optionally within a date range — used for the profit summary.</summary>
    public record GetExpensesTotalQuery(int CompanyId, DateTime? From, DateTime? To) : IRequest<ApiResponse<decimal>>;
}
