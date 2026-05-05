using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.BankAccountAccounts.Commands;
using Dog.Application.Features.BankAccounts.DTOs;
using Dog.Application.Features.Banks.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.BankAccounts.Handlers
{
    public class GetBankAccountsByCompanyHandler : IRequestHandler<GetBankAccountByCompanyQuery, ApiResponse<List<BankAccountListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetBankAccountsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<BankAccountListDto>>> Handle(GetBankAccountByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<BankAccount>()
                .TableNoTracking
                .Where(p => p.CompanyId == request.CompanyId)
                .Include(q=>q.Bank)
                .Include(q=>q.Company)
                .Include(q=>q.Currency)
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            return ApiResponse<List<BankAccountListDto>>.Ok(list.Select(BankAccountMapper.MapBankAccountList).ToList());
        }
    }


    public class GetBankAccountsByBankHandler : IRequestHandler<GetBankAccountByBankQuery, ApiResponse<List<BankAccountListDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetBankAccountsByBankHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<BankAccountListDto>>> Handle(GetBankAccountByBankQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<BankAccount>()
                .TableNoTracking
                .Where(p => p.BankId == request.BankId)
                .Include(q => q.Bank)
                .Include(q => q.Company)
                .Include(q => q.Currency)
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            return ApiResponse<List<BankAccountListDto>>.Ok(list.Select(BankAccountMapper.MapBankAccountList).ToList());
        }
    }




    public class CreateBankAccountHandler : IRequestHandler<CreateBankAccountCommand, ApiResponse<BankAccountDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateBankAccountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<BankAccountDto>> Handle(CreateBankAccountCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var exists = await _uow.Repository<BankAccount>()
                .TableNoTracking
                .AnyAsync(p => p.CompanyId == d.CompanyId && p.Name == d.Name && p.BankId==d.BankId, ct);
            if (exists) return ApiResponse<BankAccountDto>.Fail($"Bank Account method '{d.Name}' already exists.");
          
            var entity = new BankAccount
            {
                Name = d.Name ?? string.Empty,
                CurrencyId = d.CurrencyId,
                BankId = d.BankId,
                IsActive = d.IsActive,
                BranchName = d.BranchName ?? string.Empty,
                BranchCode = d.BranchCode ?? string.Empty,
                SwiftCode = d.SwiftCode ?? string.Empty,
                AccountNumber = d.AccountNumber ?? string.Empty,
                CompanyId = d.CompanyId
            };
            await _uow.Repository<BankAccount>().AddAsync(entity, ct);
            return ApiResponse<BankAccountDto>.Ok(BankAccountMapper.MapBankAccount(entity), "Bank Account method created.");
        }
    }

    public class UpdateBankAccountHandler : IRequestHandler<UpdateBankAccountCommand, ApiResponse<BankAccountDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateBankAccountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<BankAccountDto>> Handle(UpdateBankAccountCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<BankAccount>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<BankAccountDto>.Fail("Bank Account method not found.");

            entity.Name = request.Dto.Name;
            entity.CurrencyId = request.Dto.CurrencyId;
            entity.BranchCode = request.Dto.BranchCode;
            entity.BranchName = request.Dto.BranchName;
            entity.BankId = request.Dto.BankId;
            entity.SwiftCode = request.Dto.SwiftCode;
            entity.AccountNumber = request.Dto.AccountNumber;
            entity.IsActive = request.Dto.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<BankAccountDto>.Ok(BankAccountMapper.MapBankAccount(entity), "Bank Account method updated.");
        }
    }

    public class DeleteBankAccountHandler : IRequestHandler<DeleteBankAccountCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteBankAccountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteBankAccountCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<BankAccount>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Bank Account method not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Bank Account method deleted.");
        }
    }

    public class ToggleBankAccountHandler : IRequestHandler<ToggleBankAccountCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public ToggleBankAccountHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(ToggleBankAccountCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<BankAccount>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Bank Account method not found.");
            entity.IsActive = !entity.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, $"Bank Account method {(entity.IsActive ? "activated" : "deactivated")}.");
        }
    }

    internal static class BankAccountMapper
    {
        public static BankAccountDto MapBankAccount(BankAccount d) => new()
        {
            Id = d.Id,
            Name = d.Name ?? string.Empty,
            CurrencyId = d.CurrencyId,
            BankId = d.BankId,
            IsActive = d.IsActive,
            BranchName = d.BranchName ?? string.Empty,
            BranchCode = d.BranchCode ?? string.Empty,
            SwiftCode = d.SwiftCode ?? string.Empty,
            AccountNumber = d.AccountNumber ?? string.Empty,
            CompanyId = d.CompanyId
        };

        public static BankAccountListDto MapBankAccountList(BankAccount d) => new()
        {
            Id = d.Id,
            Name = d.Name ?? string.Empty,
            CurrencyId = d.CurrencyId,
            CurrencyName=d.Currency!.Name,
            BankId = d.BankId,
            BankName=d.Bank!.Name,
            IsActive = d.IsActive,
            BranchName = d.BranchName ?? string.Empty,
            BranchCode = d.BranchCode ?? string.Empty,
            SwiftCode = d.SwiftCode ?? string.Empty,
            AccountNumber = d.AccountNumber ?? string.Empty,
            CompanyId = d.CompanyId,
            CompanyName=d.Company!.CompanyName,
            
        };
    }

}
