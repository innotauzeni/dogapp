using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Breeds.Commands;
using Dog.Application.Features.Breeds.DTOs;
using Dog.Application.Features.Breeds.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Breeds.Handlers;


public class GetBreedsByGroupHandler : IRequestHandler<GetBreedByGroupQuery, ApiResponse<List<BreedDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetBreedsByGroupHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<BreedDto>>> Handle(GetBreedByGroupQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<Breed>()
            .TableNoTracking
            .Where(p => p.BreedGroupId == request.BreedGroupId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return ApiResponse<List<BreedDto>>.Ok(list.Select(BreedMapper.MapBreed).ToList());
    }
}

public class GetAllBreedsHandler : IRequestHandler<GetAllBreedsQuery, ApiResponse<List<BreedDto>>>
{
    private readonly IUnitOfWork _uow;
    public GetAllBreedsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<BreedDto>>> Handle(GetAllBreedsQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<Breed>()
            .TableNoTracking
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return ApiResponse<List<BreedDto>>.Ok(list.Select(BreedMapper.MapBreed).ToList());
    }
}

public class GetBreedByIdHandler : IRequestHandler<GetBreedByIdQuery, ApiResponse<BreedDto>>
{
    private readonly IUnitOfWork _uow;
    public GetBreedByIdHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<BreedDto>> Handle(GetBreedByIdQuery request, CancellationToken ct)
    {
        var list = await _uow.Repository<Breed>()
            .TableNoTracking
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

        return ApiResponse<BreedDto>.Ok(BreedMapper.MapBreed(list));
    }
}




public class CreateBreedHandler : IRequestHandler<CreateBreedCommand, ApiResponse<BreedDto>>
{
    private readonly IUnitOfWork _uow;
    public CreateBreedHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<BreedDto>> Handle(CreateBreedCommand request, CancellationToken ct)
    {
        var d = request.Dto;
        var exists = await _uow.Repository<Breed>()
            .TableNoTracking
            .AnyAsync(p => p.BreedGroupId == d.BreedGroupId && p.Name == d.Name && p.CompanyId == d.CompanyId, ct);
        if (exists) return ApiResponse<BreedDto>.Fail($"Breed '{d.Name}' already exists in this group.");

        var entity = new Breed
        {
            Name = d.Name,
            IsActive = d.IsActive,
            BreedGroupId = d.BreedGroupId,
            CompanyId = d.CompanyId,
        };
        await _uow.Repository<Breed>().AddAsync(entity, ct);
        return ApiResponse<BreedDto>.Ok(BreedMapper.MapBreed(entity), "Breed created.");
    }
}

public class UpdateBreedHandler : IRequestHandler<UpdateBreedCommand, ApiResponse<BreedDto>>
{
    private readonly IUnitOfWork _uow;
    public UpdateBreedHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<BreedDto>> Handle(UpdateBreedCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Breed>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<BreedDto>.Fail("Breed not found.");

        entity.Name = request.Dto.Name;
        entity.IsActive = request.Dto.IsActive;
        entity.BreedGroupId = request.Dto.BreedGroupId;
        await repo.UpdateAsync(entity, ct);
        return ApiResponse<BreedDto>.Ok(BreedMapper.MapBreed(entity), "Breed updated.");
    }
}

public class DeleteBreedHandler : IRequestHandler<DeleteBreedCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;
    public DeleteBreedHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(DeleteBreedCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Breed>();
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return ApiResponse<bool>.Fail("Breed not found.");
        await repo.DeleteAsync(entity, ct);
        return ApiResponse<bool>.Ok(true, "Breed deleted.");
    }
}











internal static class BreedMapper
{
    public static BreedDto MapBreed(Breed? c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        BreedGroupId = c.BreedGroupId,
        CompanyId = c.CompanyId,
        IsActive = c.IsActive,
    };
}


