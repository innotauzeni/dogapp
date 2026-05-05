using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Currencies.Commands;
using Dog.Application.Features.Currencies.DTOs;
using Dog.Application.Features.Currencies.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Currencies.Handlers
{
    // ── CURRENCY ─────────────────────────────────────────────────────────────
    public class GetAllCurrenciesHandler : IRequestHandler<GetAllCurrenciesQuery, ApiResponse<List<CurrencyDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetAllCurrenciesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CurrencyDto>>> Handle(GetAllCurrenciesQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Currency>()
                .TableNoTracking
                .OrderBy(c => c.Code)
                .ToListAsync(ct);

            return ApiResponse<List<CurrencyDto>>.Ok(list.Select(FinanceMapper.Map).ToList());
        }
    }

    public class CreateCurrencyHandler : IRequestHandler<CreateCurrencyCommand, ApiResponse<CurrencyDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateCurrencyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CurrencyDto>> Handle(CreateCurrencyCommand request, CancellationToken ct)
        {
            var exists = await _uow.Repository<Currency>()
                .TableNoTracking.AnyAsync(c => c.Code == request.Dto.Code, ct);
            if (exists) return ApiResponse<CurrencyDto>.Fail($"Currency '{request.Dto.Code}' already exists.");

            var entity = new Currency
            {
                Code = request.Dto.Code,
                Name = request.Dto.Name,
                Symbol = request.Dto.Symbol,
                IsActive = request.Dto.IsActive,
            };
            await _uow.Repository<Currency>().AddAsync(entity, ct);
            return ApiResponse<CurrencyDto>.Ok(FinanceMapper.Map(entity), "Currency created.");
        }
    }

    public class UpdateCurrencyHandler : IRequestHandler<UpdateCurrencyCommand, ApiResponse<CurrencyDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateCurrencyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CurrencyDto>> Handle(UpdateCurrencyCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Currency>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<CurrencyDto>.Fail("Currency not found.");

            entity.Code = request.Dto.Code;
            entity.Name = request.Dto.Name;
            entity.Symbol = request.Dto.Symbol;
            entity.IsActive = request.Dto.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<CurrencyDto>.Ok(FinanceMapper.Map(entity), "Currency updated.");
        }
    }

    public class DeleteCurrencyHandler : IRequestHandler<DeleteCurrencyCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteCurrencyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteCurrencyCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Currency>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Currency not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Currency deleted.");
        }
    }

    public class ToggleCurrencyHandler : IRequestHandler<ToggleCurrencyCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public ToggleCurrencyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(ToggleCurrencyCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Currency>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Currency not found.");
            entity.IsActive = !entity.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, $"Currency {(entity.IsActive ? "activated" : "deactivated")}.");
        }
    }

    internal static class FinanceMapper
    {
        public static CurrencyDto Map(Currency c) => new()
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Symbol = c.Symbol,
            IsActive = c.IsActive,
        };
        
    }


}
