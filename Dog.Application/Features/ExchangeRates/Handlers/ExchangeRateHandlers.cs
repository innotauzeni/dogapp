using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Currencies.Handlers;
using Dog.Application.Features.ExchangeRates.Commands;
using Dog.Application.Features.ExchangeRates.DTOs;
using Dog.Application.Features.ExchangeRates.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.ExchangeRates.Handlers
{
    // ── EXCHANGE RATE ─────────────────────────────────────────────────────────
    public class GetExchangeRatesByCompanyHandler : IRequestHandler<GetExchangeRatesByCompanyQuery, ApiResponse<List<ExchangeRateDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetExchangeRatesByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ExchangeRateDto>>> Handle(GetExchangeRatesByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<ExchangeRate>()
                .TableNoTracking
                .Include(r => r.PrimaryCurrency)
                .Include(r => r.SecondaryCurrency)
                .Where(r => r.CompanyId == request.CompanyId)
                .OrderBy(r => r.PrimaryCurrency!.Code)
                .ToListAsync(ct);

            return ApiResponse<List<ExchangeRateDto>>.Ok(list.Select(ExchangeRateMapper.MapRate).ToList());
        }
    }

    public class CreateExchangeRateHandler : IRequestHandler<CreateExchangeRateCommand, ApiResponse<ExchangeRateDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateExchangeRateHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ExchangeRateDto>> Handle(CreateExchangeRateCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var entity = new ExchangeRate
            {
                PrimaryCurrencyId = d.PrimaryCurrencyId,
                SecondaryCurrencyId = d.SecondaryCurrencyId,
                Rate = d.Rate,
                IsActive = d.IsActive,
                CompanyId = d.CompanyId,
            };
            await _uow.Repository<ExchangeRate>().AddAsync(entity, ct);
            return ApiResponse<ExchangeRateDto>.Ok(ExchangeRateMapper.MapRate(entity), "Exchange rate created.");
        }
    }

    public class UpdateExchangeRateHandler : IRequestHandler<UpdateExchangeRateCommand, ApiResponse<ExchangeRateDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateExchangeRateHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ExchangeRateDto>> Handle(UpdateExchangeRateCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<ExchangeRate>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, ct);
            if (entity == null) return ApiResponse<ExchangeRateDto>.Fail("Exchange rate not found.");

            entity.PrimaryCurrencyId = request.Dto.PrimaryCurrencyId;
            entity.SecondaryCurrencyId = request.Dto.SecondaryCurrencyId;
            entity.Rate = request.Dto.Rate;
            entity.IsActive = request.Dto.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<ExchangeRateDto>.Ok(ExchangeRateMapper.MapRate(entity), "Exchange rate updated.");
        }
    }



    public class DeleteExchangeRateHandler : IRequestHandler<DeleteExchangeRateCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteExchangeRateHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteExchangeRateCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<ExchangeRate>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Exchange rate not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Exchange rate deleted.");
        }
    }


    internal static class ExchangeRateMapper
    {
        public static ExchangeRateDto MapRate(ExchangeRate r) => new()
        {
            Id = r.Id,
            PrimaryCurrencyId = r.PrimaryCurrencyId,
            PrimaryCurrencyCode = r.PrimaryCurrency?.Code,
            SecondaryCurrencyId = r.SecondaryCurrencyId,
            SecondaryCurrencyCode = r.SecondaryCurrency?.Code,
            Rate = r.Rate,
            IsActive = r.IsActive,
            CompanyId = r.CompanyId,
            CreatedAt = r.CreatedAt,
        };
    }


}
