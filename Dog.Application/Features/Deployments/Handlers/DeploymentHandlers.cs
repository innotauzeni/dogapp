using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Deployments.Commands;
using Dog.Application.Features.Deployments.DTOs;
using Dog.Application.Features.Deployments.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Deployments.Handlers;


public class GetDeploymentsByCompanyHandler : IRequestHandler<GetDeploymentByCompanyQuery, ApiResponse<List<DeploymentDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetDeploymentsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<DeploymentDto>>> Handle(GetDeploymentByCompanyQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<Deployment>()
            .TableNoTracking
            .Where(p => p.CompanyId == request.CompanyId)
            .OrderBy(p => p.SecurityGuardAssigned)
            .ToListAsync(ct);

        return ApiResponse<List<DeploymentDto>>.Ok(list.Select(DeploymentMapper.MapDeployment).ToList());
    }
}

public class CreateDeploymentHandler : IRequestHandler<CreateDeploymentCommand, ApiResponse<DeploymentDto>>
{
    private readonly IUnitOfWork _uow;
    public CreateDeploymentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<DeploymentDto>> Handle(CreateDeploymentCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        var exists = await _uow.Repository<Deployment>()
            .TableNoTracking
            .AnyAsync(p => p.CompanyId == d.CompanyId && p.CompanyDeployedTo == d.CompanyDeployedTo, ct);
        if (exists) return ApiResponse<DeploymentDto>.Fail($"Deployment method '{d.SecurityGuardAssigned}' already exists.");
       
        var entity = new Deployment
        {
            DogId = d.DogId,
            CompanyDeployedTo = d.CompanyDeployedTo,
            SecurityGuardAssigned = d.SecurityGuardAssigned,
            Location = d.Location,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            Notes = d.Notes,
            IsActive = d.IsActive,
            CompanyId = d.CompanyId,
        };
        await _uow.Repository<Deployment>().AddAsync(entity, ct);
        return ApiResponse<DeploymentDto>.Ok(DeploymentMapper.MapDeployment(entity), "Deployment method created.");
    }
}

public class UpdateDeploymentHandler : IRequestHandler<UpdateDeploymentCommand, ApiResponse<DeploymentDto>>
{
    private readonly IUnitOfWork _uow;
    public UpdateDeploymentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<DeploymentDto>> Handle(UpdateDeploymentCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Deployment>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<DeploymentDto>.Fail("Deployment method not found.");

        entity.DogId = request.Dto.DogId;
        entity.Location = request.Dto.Location;
        entity.CompanyDeployedTo = request.Dto.CompanyDeployedTo;
        entity.SecurityGuardAssigned = request.Dto.SecurityGuardAssigned;
        entity.StartDate = request.Dto.StartDate;
        entity.EndDate = request.Dto.EndDate;
        entity.Notes = request.Dto.Notes;
        entity.IsActive = request.Dto.IsActive;
        await repo.UpdateAsync(entity, ct);
        return ApiResponse<DeploymentDto>.Ok(DeploymentMapper.MapDeployment(entity), "Deployment method updated.");
    }
}

public class DeleteDeploymentHandler : IRequestHandler<DeleteDeploymentCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;
    public DeleteDeploymentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(DeleteDeploymentCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Deployment>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<bool>.Fail("Deployment method not found.");
        await repo.DeleteAsync(entity, ct);
        return ApiResponse<bool>.Ok(true, "Deployment method deleted.");
    }
}



internal static class DeploymentMapper
{
    public static DeploymentDto MapDeployment(Deployment d) => new()
    {
        Id = d.Id,
        DogId = d.DogId,
        CompanyDeployedTo = d.CompanyDeployedTo,
        SecurityGuardAssigned = d.SecurityGuardAssigned,
        Location = d.Location,
        StartDate = d.StartDate,
        EndDate = d.EndDate,
        Notes = d.Notes,
        IsActive = d.IsActive,
        CompanyId = d.CompanyId,
    };
}

