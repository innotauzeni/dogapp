using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.CustomerContracts.Commands;
using Dog.Application.Features.CustomerContracts.DTOs;
using Dog.Application.Features.CustomerContracts.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.CustomerContracts.Handlers
{
    public class GetContractsByCustomerHandler : IRequestHandler<GetContractsByCustomerQuery, ApiResponse<List<CustomerContractDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetContractsByCustomerHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CustomerContractDto>>> Handle(GetContractsByCustomerQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<CustomerContract>()
                .TableNoTracking
                .Include(c => c.Customer)
                .Where(c => c.CustomerId == request.CustomerId)
                .OrderByDescending(c => c.ContractDate)
                .ToListAsync(ct);

            return ApiResponse<List<CustomerContractDto>>.Ok(list.Select(CustomerContractMapper.Map).ToList());
        }
    }

    public class GetCustomerContractByIdHandler : IRequestHandler<GetCustomerContractByIdQuery, ApiResponse<CustomerContractDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetCustomerContractByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerContractDto>> Handle(GetCustomerContractByIdQuery request, CancellationToken ct)
        {
            var contract = await _uow.Repository<CustomerContract>()
                .TableNoTracking
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

            return contract == null
                ? ApiResponse<CustomerContractDto>.Fail("Contract not found.")
                : ApiResponse<CustomerContractDto>.Ok(CustomerContractMapper.Map(contract));
        }
    }

    public class CreateCustomerContractHandler : IRequestHandler<CreateCustomerContractCommand, ApiResponse<CustomerContractDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateCustomerContractHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerContractDto>> Handle(CreateCustomerContractCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var customerExists = await _uow.Repository<Customer>().TableNoTracking.AnyAsync(c => c.Id == d.CustomerId, ct);
            if (!customerExists) return ApiResponse<CustomerContractDto>.Fail("Customer not found.");

            var entity = new CustomerContract
            {
                CustomerId = d.CustomerId,
                CompanyId = d.CompanyId,
                ContractDate = d.ContractDate,
                ExpiryDate = d.ExpiryDate,
                ContractDetails = d.ContractDetails,
                IsActive = d.IsActive,
            };
            await _uow.Repository<CustomerContract>().AddAsync(entity, ct);
            return ApiResponse<CustomerContractDto>.Ok(CustomerContractMapper.Map(entity), "Contract created.");
        }
    }

    public class UpdateCustomerContractHandler : IRequestHandler<UpdateCustomerContractCommand, ApiResponse<CustomerContractDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateCustomerContractHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerContractDto>> Handle(UpdateCustomerContractCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<CustomerContract>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<CustomerContractDto>.Fail("Contract not found.");

            var d = request.Dto;
            entity.ContractDate = d.ContractDate;
            entity.ExpiryDate = d.ExpiryDate;
            entity.ContractDetails = d.ContractDetails;
            entity.IsActive = d.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<CustomerContractDto>.Ok(CustomerContractMapper.Map(entity), "Contract updated.");
        }
    }

    public class DeleteCustomerContractHandler : IRequestHandler<DeleteCustomerContractCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteCustomerContractHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteCustomerContractCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<CustomerContract>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Contract not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Contract deleted.");
        }
    }

    internal static class CustomerContractMapper
    {
        public static CustomerContractDto Map(CustomerContract c) => new()
        {
            Id = c.Id,
            CustomerId = c.CustomerId,
            CustomerName = c.Customer?.FullName ?? $"{c.Customer?.FirstName} {c.Customer?.LastName}".Trim(),
            ContractDate = c.ContractDate,
            ExpiryDate = c.ExpiryDate,
            ContractDetails = c.ContractDetails,
            IsActive = c.IsActive,
        };
    }
}
