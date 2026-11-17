using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.CustomerSites.Commands;
using Dog.Application.Features.CustomerSites.DTOs;
using Dog.Application.Features.CustomerSites.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.CustomerSites.Handlers
{
    public class GetSitesByCustomerHandler : IRequestHandler<GetSitesByCustomerQuery, ApiResponse<List<CustomerSiteDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetSitesByCustomerHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CustomerSiteDto>>> Handle(GetSitesByCustomerQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<CustomerSite>()
                .TableNoTracking
                .Include(s => s.Customer)
                .Where(s => s.CustomerId == request.CustomerId)
                .OrderBy(s => s.SiteName)
                .ToListAsync(ct);

            return ApiResponse<List<CustomerSiteDto>>.Ok(list.Select(CustomerSiteMapper.Map).ToList());
        }
    }

    public class GetCustomerSiteByIdHandler : IRequestHandler<GetCustomerSiteByIdQuery, ApiResponse<CustomerSiteDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetCustomerSiteByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerSiteDto>> Handle(GetCustomerSiteByIdQuery request, CancellationToken ct)
        {
            var site = await _uow.Repository<CustomerSite>()
                .TableNoTracking
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

            return site == null
                ? ApiResponse<CustomerSiteDto>.Fail("Site not found.")
                : ApiResponse<CustomerSiteDto>.Ok(CustomerSiteMapper.Map(site));
        }
    }

    public class CreateCustomerSiteHandler : IRequestHandler<CreateCustomerSiteCommand, ApiResponse<CustomerSiteDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateCustomerSiteHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerSiteDto>> Handle(CreateCustomerSiteCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var customerExists = await _uow.Repository<Customer>().TableNoTracking.AnyAsync(c => c.Id == d.CustomerId, ct);
            if (!customerExists) return ApiResponse<CustomerSiteDto>.Fail("Customer not found.");

            var entity = new CustomerSite
            {
                CustomerId = d.CustomerId,
                CompanyId = d.CompanyId,
                SiteName = d.SiteName,
                Address = d.Address,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                ContactPerson = d.ContactPerson,
                ContactPhone = d.ContactPhone,
                IsActive = d.IsActive,
            };
            await _uow.Repository<CustomerSite>().AddAsync(entity, ct);
            return ApiResponse<CustomerSiteDto>.Ok(CustomerSiteMapper.Map(entity), "Site created.");
        }
    }

    public class UpdateCustomerSiteHandler : IRequestHandler<UpdateCustomerSiteCommand, ApiResponse<CustomerSiteDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateCustomerSiteHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerSiteDto>> Handle(UpdateCustomerSiteCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<CustomerSite>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<CustomerSiteDto>.Fail("Site not found.");

            var d = request.Dto;
            entity.SiteName = d.SiteName;
            entity.Address = d.Address;
            entity.Latitude = d.Latitude;
            entity.Longitude = d.Longitude;
            entity.ContactPerson = d.ContactPerson;
            entity.ContactPhone = d.ContactPhone;
            entity.IsActive = d.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<CustomerSiteDto>.Ok(CustomerSiteMapper.Map(entity), "Site updated.");
        }
    }

    public class DeleteCustomerSiteHandler : IRequestHandler<DeleteCustomerSiteCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteCustomerSiteHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteCustomerSiteCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<CustomerSite>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Site not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Site deleted.");
        }
    }

    internal static class CustomerSiteMapper
    {
        public static CustomerSiteDto Map(CustomerSite s) => new()
        {
            Id = s.Id,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer?.FullName ?? $"{s.Customer?.FirstName} {s.Customer?.LastName}".Trim(),
            SiteName = s.SiteName,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            ContactPerson = s.ContactPerson,
            ContactPhone = s.ContactPhone,
            IsActive = s.IsActive,
        };
    }
}
