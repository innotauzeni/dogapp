using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.RoleAccess.Commands;
using Dog.Application.Features.RoleAccess.DTOs;
using Dog.Application.Features.RoleAccess.Queries;
using Dog.Core.Entities;

namespace Dog.Application.Features.RoleAccess.Handlers
{
    public class AssignMenusToRoleHandler : IRequestHandler<AssignMenusToRoleCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        private readonly RoleManager<Role> _roleManager;

        public AssignMenusToRoleHandler(IUnitOfWork uow, RoleManager<Role> roleManager)
        {
            _uow = uow;
            _roleManager = roleManager;
        }

        public async Task<ApiResponse<bool>> Handle(AssignMenusToRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(request.Dto.RoleId);
            if (role == null) return ApiResponse<bool>.Fail("Role not found.");

            var repo = _uow.Repository<RoleMenu>();

            var existing = repo.Table
                .Where(rm => rm.RoleId == request.Dto.RoleId)
                .ToList();

            if (existing.Any())
                await repo.DeleteRangeAsync(existing, cancellationToken);

            var newMenus = request.Dto.ModuleIds.Select(moduleId => new RoleMenu
            {
                RoleId       = request.Dto.RoleId,
                SystemModuleId = moduleId
            }).ToList();

            if (newMenus.Any())
                await repo.AddRangeAsync(newMenus, cancellationToken);

            return ApiResponse<bool>.Ok(true, "Menus assigned to role successfully.");
        }
    }

    public class AssignPermissionsToRoleHandler : IRequestHandler<AssignPermissionsToRoleCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        private readonly RoleManager<Role> _roleManager;

        public AssignPermissionsToRoleHandler(IUnitOfWork uow, RoleManager<Role> roleManager)
        {
            _uow = uow;
            _roleManager = roleManager;
        }

        public async Task<ApiResponse<bool>> Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(request.Dto.RoleId);
            if (role == null) return ApiResponse<bool>.Fail("Role not found.");

            var repo = _uow.Repository<RolePermission>();

            var existing = repo.Table
                .Where(rp => rp.RoleId == request.Dto.RoleId)
                .ToList();

            if (existing.Any())
                await repo.DeleteRangeAsync(existing, cancellationToken);

            var newPerms = request.Dto.PermissionIds.Select(permId => new RolePermission
            {
                RoleId       = request.Dto.RoleId,
                PermissionId = permId
            }).ToList();

            if (newPerms.Any())
                await repo.AddRangeAsync(newPerms, cancellationToken);

            return ApiResponse<bool>.Ok(true, "Permissions assigned to role successfully.");
        }
    }

    public class GetRoleMenusHandler : IRequestHandler<GetRoleMenusQuery, ApiResponse<RoleMenusDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly RoleManager<Role> _roleManager;

        public GetRoleMenusHandler(IUnitOfWork uow, RoleManager<Role> roleManager)
        {
            _uow = uow;
            _roleManager = roleManager;
        }

        public async Task<ApiResponse<RoleMenusDto>> Handle(GetRoleMenusQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId);
            if (role == null) return ApiResponse<RoleMenusDto>.Fail("Role not found.");

            var assignedIds = await _uow.Repository<RoleMenu>()
                .TableNoTracking
                .Where(rm => rm.RoleId == request.RoleId)
                .Select(rm => rm.SystemModuleId)
                .ToListAsync(cancellationToken);

            return ApiResponse<RoleMenusDto>.Ok(new RoleMenusDto
            {
                RoleId            = role.Id,
                RoleName          = role.Name!,
                AssignedModuleIds = assignedIds
            });
        }
    }

    public class GetRolePermissionsHandler : IRequestHandler<GetRolePermissionsQuery, ApiResponse<RolePermissionsDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly RoleManager<Role> _roleManager;

        public GetRolePermissionsHandler(IUnitOfWork uow, RoleManager<Role> roleManager)
        {
            _uow = uow;
            _roleManager = roleManager;
        }

        public async Task<ApiResponse<RolePermissionsDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId);
            if (role == null) return ApiResponse<RolePermissionsDto>.Fail("Role not found.");

            var assignedIds = await _uow.Repository<RolePermission>()
                .TableNoTracking
                .Where(rp => rp.RoleId == request.RoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken);

            return ApiResponse<RolePermissionsDto>.Ok(new RolePermissionsDto
            {
                RoleId                = role.Id,
                RoleName              = role.Name!,
                AssignedPermissionIds = assignedIds
            });
        }
    }
}
