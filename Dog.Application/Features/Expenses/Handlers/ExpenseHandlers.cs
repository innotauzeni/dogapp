using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Expenses.Commands;
using Dog.Application.Features.Expenses.DTOs;
using Dog.Application.Features.Expenses.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Expenses.Handlers
{
    public class GetExpensesByCompanyHandler : IRequestHandler<GetExpensesByCompanyQuery, ApiResponse<List<ExpenseDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetExpensesByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ExpenseDto>>> Handle(GetExpensesByCompanyQuery request, CancellationToken ct)
        {
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);
            var list = await _uow.Repository<Expense>()
                .TableNoTracking
                .Include(e => e.Dog)
                .Where(e => companyIds.Contains(e.CompanyId))
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync(ct);

            return ApiResponse<List<ExpenseDto>>.Ok(list.Select(ExpenseMapper.Map).ToList());
        }
    }

    public class GetExpenseByIdHandler : IRequestHandler<GetExpenseByIdQuery, ApiResponse<ExpenseDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetExpenseByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ExpenseDto>> Handle(GetExpenseByIdQuery request, CancellationToken ct)
        {
            var expense = await _uow.Repository<Expense>()
                .TableNoTracking
                .Include(e => e.Dog)
                .FirstOrDefaultAsync(e => e.Id == request.Id, ct);

            return expense == null
                ? ApiResponse<ExpenseDto>.Fail("Expense not found.")
                : ApiResponse<ExpenseDto>.Ok(ExpenseMapper.Map(expense));
        }
    }

    public class GetExpensesTotalHandler : IRequestHandler<GetExpensesTotalQuery, ApiResponse<decimal>>
    {
        private readonly IUnitOfWork _uow;
        public GetExpensesTotalHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<decimal>> Handle(GetExpensesTotalQuery request, CancellationToken ct)
        {
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);
            var query = _uow.Repository<Expense>().TableNoTracking.Where(e => companyIds.Contains(e.CompanyId));
            if (request.From.HasValue) query = query.Where(e => e.ExpenseDate >= request.From.Value);
            if (request.To.HasValue) query = query.Where(e => e.ExpenseDate <= request.To.Value);

            var total = await query.SumAsync(e => e.Amount, ct);
            return ApiResponse<decimal>.Ok(total);
        }
    }

    public class CreateExpenseHandler : IRequestHandler<CreateExpenseCommand, ApiResponse<ExpenseDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateExpenseHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ExpenseDto>> Handle(CreateExpenseCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var entity = new Expense
            {
                CompanyId = d.CompanyId,
                Category = d.Category,
                Description = d.Description,
                Amount = d.Amount,
                ExpenseDate = d.ExpenseDate,
                DogId = d.DogId,
                Vendor = d.Vendor,
                PaymentMethod = d.PaymentMethod,
                Notes = d.Notes,
            };
            await _uow.Repository<Expense>().AddAsync(entity, ct);
            return ApiResponse<ExpenseDto>.Ok(ExpenseMapper.Map(entity), "Expense recorded.");
        }
    }

    public class UpdateExpenseHandler : IRequestHandler<UpdateExpenseCommand, ApiResponse<ExpenseDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateExpenseHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ExpenseDto>> Handle(UpdateExpenseCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Expense>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<ExpenseDto>.Fail("Expense not found.");

            var d = request.Dto;
            entity.Category = d.Category;
            entity.Description = d.Description;
            entity.Amount = d.Amount;
            entity.ExpenseDate = d.ExpenseDate;
            entity.DogId = d.DogId;
            entity.Vendor = d.Vendor;
            entity.PaymentMethod = d.PaymentMethod;
            entity.Notes = d.Notes;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<ExpenseDto>.Ok(ExpenseMapper.Map(entity), "Expense updated.");
        }
    }

    public class DeleteExpenseHandler : IRequestHandler<DeleteExpenseCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteExpenseHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteExpenseCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Expense>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Expense not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Expense deleted.");
        }
    }

    internal static class ExpenseMapper
    {
        public static ExpenseDto Map(Expense e) => new()
        {
            Id = e.Id,
            CompanyId = e.CompanyId,
            Category = e.Category,
            Description = e.Description,
            Amount = e.Amount,
            ExpenseDate = e.ExpenseDate,
            DogId = e.DogId,
            DogName = e.Dog?.Name,
            Vendor = e.Vendor,
            PaymentMethod = e.PaymentMethod,
            Notes = e.Notes,
        };
    }
}
