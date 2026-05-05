using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Permissions.Commands;
using Dog.Application.Features.Permissions.DTOs;
using Dog.Application.Features.Permissions.Queries;
using Dog.Core.Entities;

namespace Dog.Application.Features.Permissions.Handlers
{
    public class GetAllPermissionsHandler : IRequestHandler<GetAllPermissionsQuery, ApiResponse<List<PermissionDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllPermissionsHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<List<PermissionDto>>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
        {
            var items = await _uow.Repository<Permission>().TableNoTracking.OrderBy(p => p.Module).ThenBy(p => p.Name).ToListAsync(cancellationToken);
            return ApiResponse<List<PermissionDto>>.Ok(_mapper.Map<List<PermissionDto>>(items));
        }
    }

    public class GetPermissionsByModuleHandler : IRequestHandler<GetPermissionsByModuleQuery, ApiResponse<List<PermissionDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetPermissionsByModuleHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<List<PermissionDto>>> Handle(GetPermissionsByModuleQuery request, CancellationToken cancellationToken)
        {
            var items = await _uow.Repository<Permission>()
                .TableNoTracking
                .Where(p => p.Module == request.Module)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return ApiResponse<List<PermissionDto>>.Ok(_mapper.Map<List<PermissionDto>>(items));
        }
    }

    public class GetPermissionByIdHandler : IRequestHandler<GetPermissionByIdQuery, ApiResponse<PermissionDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetPermissionByIdHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<PermissionDto>> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _uow.Repository<Permission>().GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<PermissionDto>.Fail($"Permission with id {request.Id} not found.");

            return ApiResponse<PermissionDto>.Ok(_mapper.Map<PermissionDto>(entity));
        }
    }

    public class CreatePermissionHandler : IRequestHandler<CreatePermissionCommand, ApiResponse<PermissionDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CreatePermissionHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<PermissionDto>> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Permission>(request.Dto);
            await _uow.Repository<Permission>().AddAsync(entity, cancellationToken);
            return ApiResponse<PermissionDto>.Ok(_mapper.Map<PermissionDto>(entity), "Permission created successfully.");
        }
    }

    public class UpdatePermissionHandler : IRequestHandler<UpdatePermissionCommand, ApiResponse<PermissionDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public UpdatePermissionHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<PermissionDto>> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
        {
            var repo = _uow.Repository<Permission>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<PermissionDto>.Fail($"Permission with id {request.Dto.Id} not found.");

            _mapper.Map(request.Dto, entity);
            await repo.UpdateAsync(entity, cancellationToken);
            return ApiResponse<PermissionDto>.Ok(_mapper.Map<PermissionDto>(entity), "Permission updated successfully.");
        }
    }

    public class DeletePermissionHandler : IRequestHandler<DeletePermissionCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;

        public DeletePermissionHandler(IUnitOfWork uow) { _uow = uow; }

        public async Task<ApiResponse<bool>> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
        {
            var repo = _uow.Repository<Permission>();
            var entity = await repo.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<bool>.Fail($"Permission with id {request.Id} not found.");

            await repo.DeleteAsync(entity, cancellationToken);
            return ApiResponse<bool>.Ok(true, "Permission deleted successfully.");
        }
    }
}
