using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.BankTransactions.Commands;
using Dog.Application.Features.BankTransactions.DTOs;
using Dog.Application.Features.BankTransactions.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.BankTransactions.Handlers
{
    public class GetBankTransactionsByCompanyHandler : IRequestHandler<GetBankTransactionsByCompanyQuery, ApiResponse<List<BankTransactionListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetBankTransactionsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<BankTransactionListDto>>> Handle(GetBankTransactionsByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<BankTransaction>()
                .TableNoTracking
                .Where(p => p.CompanyId == request.CompanyId)
                .Include(q=>q.Currency)
                .Include(q=>q.Customer)
                .Include(q=>q.Bank)
                .Include(q=>q.Company)
                .Include(q=>q.ProofofPayment)
                  .ThenInclude(q=>q.Invoice)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<BankTransactionListDto>>.Ok(list.Select(BankTransactionMapper.MapBankTransactionList).ToList());
        }
    }

    public class CreateBankTransactionHandler : IRequestHandler<CreateBankTransactionCommand, ApiResponse<BankTransactionDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateBankTransactionHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<BankTransactionDto>> Handle(CreateBankTransactionCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var exists = await _uow.Repository<BankTransaction>()
                .TableNoTracking
                .AnyAsync(p => p.CompanyId == d.CompanyId && p.Name == d.Name, ct);
            if (exists) return ApiResponse<BankTransactionDto>.Fail($"Payment method '{d.Name}' already exists.");

            var entity = new BankTransaction
            {
                Name = d.Name,
                Status = d.Status!,
                CompanyId = d.CompanyId,
            };
            await _uow.Repository<BankTransaction>().AddAsync(entity, ct);
            return ApiResponse<BankTransactionDto>.Ok(BankTransactionMapper.MapBankTransaction(entity), "Payment method created.");
        }
    }

    public class UpdateBankTransactionHandler : IRequestHandler<UpdateBankTransactionCommand, ApiResponse<BankTransactionDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateBankTransactionHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<BankTransactionDto>> Handle(UpdateBankTransactionCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<BankTransaction>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, ct);
            if (entity == null) return ApiResponse<BankTransactionDto>.Fail("Payment method not found.");

            entity.Name = request.Dto.Name;
            entity.Status = request.Dto.Status!;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<BankTransactionDto>.Ok(BankTransactionMapper.MapBankTransaction(entity), "Payment method updated.");
        }
    }

    public class DeleteBankTransactionHandler : IRequestHandler<DeleteBankTransactionCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteBankTransactionHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteBankTransactionCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<BankTransaction>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Payment method not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Payment method deleted.");
        }
    }

  
    internal static class BankTransactionMapper
    {
        public static BankTransactionDto MapBankTransaction(BankTransaction p) => new()
        {
            Id = p.Id,
            Name = p.Name ?? string.Empty,
            Status = p.Status,
            CompanyId = p.CompanyId,
        };
      
        public static BankTransactionListDto MapBankTransactionList(BankTransaction p) => new()
        {
            Id = p.Id,
            BankId = p.BankId,
            BankName=p.Bank!.Name,
            CustomerId = p.CustomerId,
            CustomerName=p.Customer!.FullName,
            CurrencyId = p.CurrencyId,
            CurrencyName=p.Currency!.Code,
            Name = p.Name ?? string.Empty,
            ProofofPaymentId = p.ProofofPaymentId,
            ProofofPaymentName = p.ProofofPayment!.Invoice!.InvoiceNumber,
            AccountNumber = p.AccountNumber,
            TransactionDate = p.TransactionDate,
            Description = p.Description,
            SourceReference = p.SourceReference,
            StatementReference = p.StatementReference,
            Amount = p.Amount,
            Status = p.Status,
            CompanyId = p.CompanyId,
        };
    }


}
