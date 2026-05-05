using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Banks.Commands;
using Dog.Application.Features.Banks.DTOs;
using Dog.Application.Features.Banks.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Banks.Handlers
{
    public class GetBanksByCompanyHandler : IRequestHandler<GetBankByCompanyQuery, ApiResponse<List<BankDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetBanksByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<BankDto>>> Handle(GetBankByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Bank>()
                .TableNoTracking
                .Where(p => p.CompanyId == request.CompanyId)
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            return ApiResponse<List<BankDto>>.Ok(list.Select(BankMapper.MapBank).ToList());
        }
    }


    public class GetBanksQueryHandler : IRequestHandler<GetBankByCompanyQuery, ApiResponse<List<BankDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetBanksQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<BankDto>>> Handle(GetBankByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Bank>()
                .TableNoTracking
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            return ApiResponse<List<BankDto>>.Ok(list.Select(BankMapper.MapBank).ToList());
        }
    }

    public class CreateBankHandler : IRequestHandler<CreateBankCommand, ApiResponse<BankDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateBankHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<BankDto>> Handle(CreateBankCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var exists = await _uow.Repository<Bank>()
                .TableNoTracking
                .AnyAsync(p => p.CompanyId == d.CompanyId && p.Name == d.Name, ct);
            if (exists) return ApiResponse<BankDto>.Fail($"Bank method '{d.Name}' already exists.");

            var entity = new Bank
            {
                Name = d.Name,
                AccountName = d.AccountName,
                IsActive = d.IsActive,
                CompanyId = d.CompanyId,
            };
            await _uow.Repository<Bank>().AddAsync(entity, ct);
            return ApiResponse<BankDto>.Ok(BankMapper.MapBank(entity), "Bank method created.");
        }
    }

    public class UpdateBankHandler : IRequestHandler<UpdateBankCommand, ApiResponse<BankDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateBankHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<BankDto>> Handle(UpdateBankCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Bank>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<BankDto>.Fail("Bank method not found.");

            entity.Name = request.Dto.Name;
            entity.IsActive = request.Dto.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<BankDto>.Ok(BankMapper.MapBank(entity), "Bank method updated.");
        }
    }

    public class DeleteBankHandler : IRequestHandler<DeleteBankCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteBankHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteBankCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Bank>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Bank method not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Bank method deleted.");
        }
    }

    public class ToggleBankHandler : IRequestHandler<ToggleBankCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public ToggleBankHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(ToggleBankCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Bank>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Bank method not found.");
            entity.IsActive = !entity.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, $"Bank method {(entity.IsActive ? "activated" : "deactivated")}.");
        }
    }

    internal static class BankMapper
    {
        public static BankDto MapBank(Bank p) => new()
        {
            Id = p.Id,
            Name = p.Name ?? string.Empty,
            IsActive = p.IsActive,
            CompanyId = p.CompanyId,
        };
    }

}
