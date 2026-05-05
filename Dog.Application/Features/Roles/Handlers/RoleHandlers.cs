using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Roles.Commands;
using Dog.Application.Features.Roles.DTOs;
using Dog.Application.Features.Roles.Queries;
using Dog.Core.Entities;

namespace Dog.Application.Features.Roles.Handlers
{
    public class GetAllRolesHandler : IRequestHandler<GetAllRolesQuery, ApiResponse<List<RoleDto>>>
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;

        public GetAllRolesHandler(RoleManager<Role> roleManager, UserManager<ApplicationUser> userManager, IUnitOfWork uow)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _uow = uow;
        }

        public async Task<ApiResponse<List<RoleDto>>> Handle(GetAllRolesQuery request, CancellationToken ct)
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(ct);

            var result = new List<RoleDto>();
            foreach (var role in roles)
            {
                var permCount = await _uow.Repository<RolePermission>()
                    .TableNoTracking.CountAsync(rp => rp.RoleId == role.Id, ct);

                var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;

                result.Add(new RoleDto
                {
                    Id              = role.Id,
                    Name            = role.Name!,
                    Type            = role.Type,
                    Level           = role.Level,
                    UserCount       = userCount,
                    PermissionCount = permCount,
                });
            }

            return ApiResponse<List<RoleDto>>.Ok(result);
        }
    }

    public class GetRoleByIdHandler : IRequestHandler<GetRoleByIdQuery, ApiResponse<RoleDto>>
    {
        private readonly RoleManager<Role> _roleManager;

        public GetRoleByIdHandler(RoleManager<Role> roleManager) => _roleManager = roleManager;

        public async Task<ApiResponse<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken ct)
        {
            var role = await _roleManager.FindByIdAsync(request.Id);
            if (role == null) return ApiResponse<RoleDto>.Fail("Role not found.");

            return ApiResponse<RoleDto>.Ok(new RoleDto
            {
                Id    = role.Id,
                Name  = role.Name!,
                Type  = role.Type,
                Level = role.Level,
            });
        }
    }

    public class CreateRoleHandler : IRequestHandler<CreateRoleCommand, ApiResponse<RoleDto>>
    {
        private readonly RoleManager<Role> _roleManager;

        public CreateRoleHandler(RoleManager<Role> roleManager) => _roleManager = roleManager;

        public async Task<ApiResponse<RoleDto>> Handle(CreateRoleCommand request, CancellationToken ct)
        {
            if (await _roleManager.RoleExistsAsync(request.Dto.Name))
                return ApiResponse<RoleDto>.Fail($"Role '{request.Dto.Name}' already exists.");

            var role   = new Role { Name = request.Dto.Name, Type = request.Dto.Type, Level = request.Dto.Level };
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
                return ApiResponse<RoleDto>.Fail("Failed to create role.", result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<RoleDto>.Ok(new RoleDto { Id = role.Id, Name = role.Name!, Type = role.Type, Level = role.Level }, "Role created.");
        }
    }

    public class UpdateRoleHandler : IRequestHandler<UpdateRoleCommand, ApiResponse<RoleDto>>
    {
        private readonly RoleManager<Role> _roleManager;

        public UpdateRoleHandler(RoleManager<Role> roleManager) => _roleManager = roleManager;

        public async Task<ApiResponse<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken ct)
        {
            var role = await _roleManager.FindByIdAsync(request.Dto.Id);
            if (role == null) return ApiResponse<RoleDto>.Fail("Role not found.");

            role.Name  = request.Dto.Name;
            role.Type  = request.Dto.Type;
            role.Level = request.Dto.Level;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                return ApiResponse<RoleDto>.Fail("Failed to update role.", result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<RoleDto>.Ok(new RoleDto { Id = role.Id, Name = role.Name!, Type = role.Type, Level = role.Level }, "Role updated.");
        }
    }

    public class DeleteRoleHandler : IRequestHandler<DeleteRoleCommand, ApiResponse<bool>>
    {
        private readonly RoleManager<Role> _roleManager;

        public DeleteRoleHandler(RoleManager<Role> roleManager) => _roleManager = roleManager;

        public async Task<ApiResponse<bool>> Handle(DeleteRoleCommand request, CancellationToken ct)
        {
            var role = await _roleManager.FindByIdAsync(request.Id);
            if (role == null) return ApiResponse<bool>.Fail("Role not found.");

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
                return ApiResponse<bool>.Fail("Failed to delete role.", result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<bool>.Ok(true, "Role deleted.");
        }
    }
}
