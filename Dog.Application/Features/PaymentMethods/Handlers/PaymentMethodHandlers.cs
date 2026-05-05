using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.PaymentMethods.Commands;
using Dog.Application.Features.PaymentMethods.DTOs;
using Dog.Application.Features.PaymentMethods.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.PaymentMethods.Handlers
{

    // ── PAYMENT METHOD ────────────────────────────────────────────────────────
    public class GetPaymentMethodsByCompanyHandler : IRequestHandler<GetPaymentMethodsByCompanyQuery, ApiResponse<List<PaymentMethodDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetPaymentMethodsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<PaymentMethodDto>>> Handle(GetPaymentMethodsByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<PaymentMethod>()
                .TableNoTracking
                .Where(p => p.CompanyId == request.CompanyId)
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            return ApiResponse<List<PaymentMethodDto>>.Ok(list.Select(PaymentMethodMapper.MapPaymentMethod).ToList());
        }
    }

    public class CreatePaymentMethodHandler : IRequestHandler<CreatePaymentMethodCommand, ApiResponse<PaymentMethodDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreatePaymentMethodHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<PaymentMethodDto>> Handle(CreatePaymentMethodCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var exists = await _uow.Repository<PaymentMethod>()
                .TableNoTracking
                .AnyAsync(p => p.CompanyId == d.CompanyId && p.Name == d.Name, ct);
            if (exists) return ApiResponse<PaymentMethodDto>.Fail($"Payment method '{d.Name}' already exists.");

            var entity = new PaymentMethod
            {
                Name = d.Name,
                IsActive = d.IsActive,
                CompanyId = d.CompanyId,
            };
            await _uow.Repository<PaymentMethod>().AddAsync(entity, ct);
            return ApiResponse<PaymentMethodDto>.Ok(PaymentMethodMapper.MapPaymentMethod(entity), "Payment method created.");
        }
    }

    public class UpdatePaymentMethodHandler : IRequestHandler<UpdatePaymentMethodCommand, ApiResponse<PaymentMethodDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdatePaymentMethodHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<PaymentMethodDto>> Handle(UpdatePaymentMethodCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<PaymentMethod>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, ct);
            if (entity == null) return ApiResponse<PaymentMethodDto>.Fail("Payment method not found.");

            entity.Name = request.Dto.Name;
            entity.IsActive = request.Dto.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<PaymentMethodDto>.Ok(PaymentMethodMapper.MapPaymentMethod(entity), "Payment method updated.");
        }
    }

    public class DeletePaymentMethodHandler : IRequestHandler<DeletePaymentMethodCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeletePaymentMethodHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeletePaymentMethodCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<PaymentMethod>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Payment method not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Payment method deleted.");
        }
    }

    public class TogglePaymentMethodHandler : IRequestHandler<TogglePaymentMethodCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public TogglePaymentMethodHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(TogglePaymentMethodCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<PaymentMethod>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Payment method not found.");
            entity.IsActive = !entity.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, $"Payment method {(entity.IsActive ? "activated" : "deactivated")}.");
        }
    }

    internal static class PaymentMethodMapper
    {
        public static PaymentMethodDto MapPaymentMethod(PaymentMethod p) => new()
        {
            Id = p.Id,
            Name = p.Name ?? string.Empty,
            IsActive = p.IsActive,
            CompanyId = p.CompanyId,
        };
    }


}
