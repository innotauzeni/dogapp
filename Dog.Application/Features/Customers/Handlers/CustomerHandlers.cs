using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Customers.Commands;
using Dog.Application.Features.Customers.DTOs;
using Dog.Application.Features.Customers.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Customers.Handlers;

public class GetCustomersHandler : IRequestHandler<GetCustomersQuery, ApiResponse<List<CustomerDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetCustomersHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<Customer>()
            .TableNoTracking
            .ToListAsync(ct);

        return ApiResponse<List<CustomerDto>>.Ok(list.Select(CustomerMapper.MapCustomer).ToList());
    }
}

public class GetCustomersByCompanyHandler : IRequestHandler<GetCustomerByCompanyQuery, ApiResponse<List<CustomerDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetCustomersByCompanyHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<CustomerDto>>> Handle(GetCustomerByCompanyQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<Customer>()
            .TableNoTracking
            .Where(p => p.CompanyId == request.CompanyId)
            .OrderBy(p => p.FullName)
            .ToListAsync(ct);

        return ApiResponse<List<CustomerDto>>.Ok(list.Select(CustomerMapper.MapCustomer).ToList());
    }
}

public class GetCustomersByIdHandler : IRequestHandler<GetCustomerByIdQuery, ApiResponse<CustomerDto>>
{
    private readonly IUnitOfWork _uow;
    public GetCustomersByIdHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<Customer>()
            .TableNoTracking
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

        return ApiResponse<CustomerDto>.Ok(CustomerMapper.MapCustomer(list));
    }
}

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, ApiResponse<CustomerDto>>
{
    private readonly IUnitOfWork _uow;
    public CreateCustomerHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        var exists = await _uow.Repository<Customer>()
            .TableNoTracking
            .AnyAsync(p => p.CompanyId == d.CompanyId && p.IDnumber == d.IDnumber, ct);
        if (exists) return ApiResponse<CustomerDto>.Fail($"National Id '{d.IDnumber}' already exists.");
        var existphones = await _uow.Repository<Customer>()
          .TableNoTracking
          .AnyAsync(p => p.CompanyId == d.CompanyId && p.Cellphone == d.Cellphone, ct);

        if (existphones) return ApiResponse<CustomerDto>.Fail($"Phone number  '{d.Cellphone}' already exists.");
        var entity = new Customer
        {
            FirstName = d.FirstName,
            LastName = d.LastName,
            FullName = d.FirstName +" "+ d.LastName,
            Dob = d.Dob,
            IDnumber = d.IDnumber,
            Cellphone = d.Cellphone,
            Email = d.Email,
            Address = d.Address,
            Profile = d.Profile,
            Gender = d.Gender,
            IsActive = d.IsActive,
            CompanyId = d.CompanyId,
        };
        await _uow.Repository<Customer>().AddAsync(entity, ct);
        return ApiResponse<CustomerDto>.Ok(CustomerMapper.MapCustomer(entity), "Customer method created.");
    }
}

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, ApiResponse<CustomerDto>>
{
    private readonly IUnitOfWork _uow;
    public UpdateCustomerHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<CustomerDto>> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Customer>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<CustomerDto>.Fail("Customer method not found.");
        entity.FirstName = request.Dto.FirstName;
        entity.LastName = request.Dto.LastName;
        entity.FullName = request.Dto.FirstName + " " + request.Dto.LastName;
        entity.IsActive = request.Dto.IsActive;
        entity.Dob = request.Dto.Dob;
        entity.IDnumber = request.Dto.IDnumber;
        entity.Cellphone = request.Dto.Cellphone;
        entity.Address = request.Dto.Address;
        entity.Email = request.Dto.Email;
        entity.Profile = request.Dto.Profile;
        entity.Gender = request.Dto.Gender;
        entity.CompanyId = entity.CompanyId;
        await repo.UpdateAsync(entity, ct);
        return ApiResponse<CustomerDto>.Ok(CustomerMapper.MapCustomer(entity), "Customer method updated.");
    }
}

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;
    public DeleteCustomerHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Customer>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<bool>.Fail("Customer method not found.");
        await repo.DeleteAsync(entity, ct);
        return ApiResponse<bool>.Ok(true, "Customer method deleted.");
    }
}

public class ToggleCustomerHandler : IRequestHandler<ToggleCustomerCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;
    public ToggleCustomerHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(ToggleCustomerCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Customer>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<bool>.Fail("Customer method not found.");
        entity.IsActive = !entity.IsActive;
        await repo.UpdateAsync(entity, ct);
        return ApiResponse<bool>.Ok(true, $"Customer method {(entity.IsActive ? "activated" : "deactivated")}.");
    }
}

internal static class CustomerMapper
{
    public static CustomerDto MapCustomer(Customer d) => new()
    {
        Id = d.Id,
        FirstName = d.FirstName,
        LastName = d.LastName,
        FullName = d.FullName,
        Dob = d.Dob,
        IDnumber = d.IDnumber,
        Cellphone = d.Cellphone,
        Email = d.Email,
        Address = d.Address,
        Profile = d.Profile,
        Gender = d.Gender,
        CompanyId = d.CompanyId
    };
}

