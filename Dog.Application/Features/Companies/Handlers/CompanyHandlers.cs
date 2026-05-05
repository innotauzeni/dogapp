using MediatR;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Companies.Commands;
using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.Companies.Queries;
using Dog.Core.Entities;

namespace Dog.Application.Features.Companies.Handlers
{
    public class GetAllCompaniesHandler : IRequestHandler<GetAllCompaniesQuery, ApiResponse<List<CompanyDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetAllCompaniesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CompanyDto>>> Handle(GetAllCompaniesQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Company>()
                .TableNoTracking
                .Include(c => c.Country).Include(c => c.Province).Include(c => c.City)
                .Include(c => c.DefaultCurrency).Include(c => c.ParentCompany).Include(c => c.Branches)
                .OrderBy(c => c.ParentCompanyId == null ? 0 : 1).ThenBy(c => c.CompanyName)
                .ToListAsync(ct);
            return ApiResponse<List<CompanyDto>>.Ok(list.Select(CompanyMapper.Map).ToList());
        }
    }

    public class GetCompanyByIdHandler : IRequestHandler<GetCompanyByIdQuery, ApiResponse<CompanyDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetCompanyByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CompanyDto>> Handle(GetCompanyByIdQuery request, CancellationToken ct)
        {
            var c = await _uow.Repository<Company>().TableNoTracking
                .Include(x => x.Country).Include(x => x.Province).Include(x => x.City)
                .Include(x => x.DefaultCurrency).Include(x => x.ParentCompany).Include(x => x.Branches)
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            return c == null ? ApiResponse<CompanyDto>.Fail("Company not found.") : ApiResponse<CompanyDto>.Ok(CompanyMapper.Map(c));
        }
    }

    public class GetBranchesByCompanyHandler : IRequestHandler<GetBranchesByCompanyQuery, ApiResponse<List<CompanyDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetBranchesByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CompanyDto>>> Handle(GetBranchesByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Company>().TableNoTracking
                .Include(c => c.Country).Include(c => c.City)
                .Where(c => c.ParentCompanyId == request.ParentId)
                .OrderBy(c => c.CompanyName).ToListAsync(ct);
            return ApiResponse<List<CompanyDto>>.Ok(list.Select(CompanyMapper.Map).ToList());
        }
    }

    public class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, ApiResponse<CompanyDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CompanyDto>> Handle(CreateCompanyCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var e = new Company
            {
                CompanyName = d.CompanyName, CompanyCode = d.CompanyCode,
                EmailAddress = d.EmailAddress, PhoneNumber = d.PhoneNumber,
                Website = d.Website, Address = d.Address, Location = d.Location,
                CountryId = d.CountryId, ProvinceId = d.ProvinceId, CityId = d.CityId,
                DefaultCurrencyId = d.DefaultCurrencyId, ParentCompanyId = d.ParentCompanyId,
                IsActive = d.IsActive,
            };
            await _uow.Repository<Company>().AddAsync(e, ct);
            return ApiResponse<CompanyDto>.Ok(CompanyMapper.Map(e), "Company created.");
        }
    }

    public class UpdateCompanyHandler : IRequestHandler<UpdateCompanyCommand, ApiResponse<CompanyDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CompanyDto>> Handle(UpdateCompanyCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Company>();
            var e = await repo.GetByIdAsync(request.Dto.Id, ct);
            if (e == null) return ApiResponse<CompanyDto>.Fail("Company not found.");
            var d = request.Dto;
            e.CompanyName = d.CompanyName; e.CompanyCode = d.CompanyCode;
            e.EmailAddress = d.EmailAddress; e.PhoneNumber = d.PhoneNumber;
            e.Website = d.Website; e.Address = d.Address; e.Location = d.Location;
            e.CountryId = d.CountryId; e.ProvinceId = d.ProvinceId; e.CityId = d.CityId;
            e.DefaultCurrencyId = d.DefaultCurrencyId; e.ParentCompanyId = d.ParentCompanyId;
            e.IsActive = d.IsActive;
            await repo.UpdateAsync(e, ct);
            return ApiResponse<CompanyDto>.Ok(CompanyMapper.Map(e), "Company updated.");
        }
    }

    public class DeleteCompanyHandler : IRequestHandler<DeleteCompanyCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteCompanyCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Company>();
            var e = await repo.GetByIdAsync(request.Id, ct);
            if (e == null) return ApiResponse<bool>.Fail("Company not found.");
            await repo.DeleteAsync(e, ct);
            return ApiResponse<bool>.Ok(true, "Company deleted.");
        }
    }

    public class ToggleCompanyStatusHandler : IRequestHandler<ToggleCompanyStatusCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public ToggleCompanyStatusHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(ToggleCompanyStatusCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<Company>();
            var e = await repo.GetByIdAsync(request.Id, ct);
            if (e == null) return ApiResponse<bool>.Fail("Company not found.");
            e.IsActive = !e.IsActive;
            await repo.UpdateAsync(e, ct);
            return ApiResponse<bool>.Ok(true, $"Company {(e.IsActive ? "activated" : "deactivated")}.");
        }
    }

    internal static class CompanyMapper
    {
        public static CompanyDto Map(Company c) => new()
        {
            Id = c.Id, CompanyName = c.CompanyName, CompanyCode = c.CompanyCode,
            EmailAddress = c.EmailAddress, PhoneNumber = c.PhoneNumber,
            Website = c.Website, Address = c.Address, Location = c.Location,
            CountryId = c.CountryId, CountryName = c.Country?.Name,
            ProvinceId = c.ProvinceId, ProvinceName = c.Province?.Name,
            CityId = c.CityId, CityName = c.City?.Name,
            DefaultCurrencyId = c.DefaultCurrencyId, CurrencyCode = c.DefaultCurrency?.Code,
            ParentCompanyId = c.ParentCompanyId, ParentCompanyName = c.ParentCompany?.CompanyName,
            IsActive = c.IsActive, BranchCount = c.Branches?.Count ?? 0,
        };
    }
}
