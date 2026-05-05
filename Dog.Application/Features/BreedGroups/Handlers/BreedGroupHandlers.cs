using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.BreedGroups.Commands;
using Dog.Application.Features.BreedGroups.DTOs;
using Dog.Application.Features.BreedGroups.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.BreedGroups.Handlers;

public class GetBreedGroupsByCompanyHandler : IRequestHandler<GetBreedGroupByCompanyQuery, ApiResponse<List<BreedGroupDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetBreedGroupsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<BreedGroupDto>>> Handle(GetBreedGroupByCompanyQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<BreedGroup>()
            .TableNoTracking
            .Where(p => p.CompanyId == request.CompanyId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return ApiResponse<List<BreedGroupDto>>.Ok(list.Select(BreedGroupMapper.MapBreedGroup).ToList());
    }
}

public class GetAllBreedGroupsHandler : IRequestHandler<GetAllBreedGroupsQuery, ApiResponse<List<BreedGroupDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetAllBreedGroupsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<BreedGroupDto>>> Handle(GetAllBreedGroupsQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<BreedGroup>()
            .TableNoTracking
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return ApiResponse<List<BreedGroupDto>>.Ok(list.Select(BreedGroupMapper.MapBreedGroup).ToList());
    }
}

public class GetBreedGroupByIdHandler : IRequestHandler<GetBreedGroupByIdQuery, ApiResponse<BreedGroupDto>>
{
    private readonly IUnitOfWork _uow;
    public GetBreedGroupByIdHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<BreedGroupDto>> Handle(GetBreedGroupByIdQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<BreedGroup>()
            .TableNoTracking
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

        return ApiResponse<BreedGroupDto>.Ok(BreedGroupMapper.MapBreedGroup(list));
    }
}

public class CreateBreedGroupHandler : IRequestHandler<CreateBreedGroupCommand, ApiResponse<BreedGroupDto>>
{
    private readonly IUnitOfWork _uow;
    public CreateBreedGroupHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<BreedGroupDto>> Handle(CreateBreedGroupCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        var exists = await _uow.Repository<BreedGroup>()
            .TableNoTracking
            .AnyAsync(p => p.CompanyId == d.CompanyId && p.Name == d.Name, ct);
        if (exists) return ApiResponse<BreedGroupDto>.Fail($"Breed Group '{d.Name}' already exists.");

        var entity = new BreedGroup
        {
            Name = d.Name,
            IsActive = d.IsActive,
            CompanyId = d.CompanyId,
        };
        await _uow.Repository<BreedGroup>().AddAsync(entity, ct);
        return ApiResponse<BreedGroupDto>.Ok(BreedGroupMapper.MapBreedGroup(entity), "Breed Group created.");
    }
}

public class UpdateBreedGroupHandler : IRequestHandler<UpdateBreedGroupCommand, ApiResponse<BreedGroupDto>>
{
    private readonly IUnitOfWork _uow;
    public UpdateBreedGroupHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<BreedGroupDto>> Handle(UpdateBreedGroupCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<BreedGroup>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<BreedGroupDto>.Fail("Breed Group not found.");

        entity.Name = request.Dto.Name;
        entity.IsActive = request.Dto.IsActive;
        await repo.UpdateAsync(entity, ct);
        return ApiResponse<BreedGroupDto>.Ok(BreedGroupMapper.MapBreedGroup(entity), "Breed Group updated.");
    }
}

public class DeleteBreedGroupHandler : IRequestHandler<DeleteBreedGroupCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;
    public DeleteBreedGroupHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(DeleteBreedGroupCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<BreedGroup>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<bool>.Fail("Breed Group not found.");
        await repo.DeleteAsync(entity, ct);
        return ApiResponse<bool>.Ok(true, "Breed Group deleted.");
    }
}

internal static class BreedGroupMapper
{
    public static BreedGroupDto MapBreedGroup(BreedGroup? c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        CompanyId = c.CompanyId,
        IsActive = c.IsActive,
    };
}

















