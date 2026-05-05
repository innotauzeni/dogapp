using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Users.Commands;
using Dog.Application.Features.Users.DTOs;
using Dog.Application.Features.Users.Queries;
using Dog.Core.Entities;

namespace Dog.Application.Features.Users.Handlers
{
    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, ApiResponse<List<UserDto>>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetAllUsersHandler(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public async Task<ApiResponse<List<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
        {
            var query = _userManager.Users.AsQueryable();
            if (request.CompanyId.HasValue)
                query = query.Where(u => u.CompanyId == request.CompanyId.Value);

            var users = await query.OrderBy(u => u.Name).ToListAsync(ct);

            var result = new List<UserDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(UserMapper.ToDto(u, roles.FirstOrDefault()));
            }

            return ApiResponse<List<UserDto>>.Ok(result);
        }
    }

    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetUserByIdHandler(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public async Task<ApiResponse<UserDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            return ApiResponse<UserDto>.Ok(UserMapper.ToDto(user, roles.FirstOrDefault()));
        }
    }

    public class CreateUserHandler : IRequestHandler<CreateUserCommand, ApiResponse<UserDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateUserHandler(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public async Task<ApiResponse<UserDto>> Handle(CreateUserCommand request, CancellationToken ct)
        {
            if (await _userManager.FindByEmailAsync(request.Dto.Email) != null)
                return ApiResponse<UserDto>.Fail("Email already in use.");

            var user = new ApplicationUser
            {
                UserName       = request.Dto.Email,
                Email          = request.Dto.Email,
                Name           = request.Dto.FirstName,
                Surname        = request.Dto.LastName,
                PhoneNumber    = request.Dto.PhoneNumber,
                CompanyId      = request.Dto.CompanyId,
                Status         = "ACTIVE",
                EmailConfirmed = true,
                RoleName       = request.Dto.RoleName,
            };

            var result = await _userManager.CreateAsync(user, request.Dto.Password);
            if (!result.Succeeded)
                return ApiResponse<UserDto>.Fail("Failed to create user.", result.Errors.Select(e => e.Description).ToList());

            if (!string.IsNullOrEmpty(request.Dto.RoleName))
                await _userManager.AddToRoleAsync(user, request.Dto.RoleName);

            return ApiResponse<UserDto>.Ok(UserMapper.ToDto(user, request.Dto.RoleName), "User created.");
        }
    }

    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, ApiResponse<UserDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UpdateUserHandler(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public async Task<ApiResponse<UserDto>> Handle(UpdateUserCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.Dto.Id);
            if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

            user.Name        = request.Dto.FirstName;
            user.Surname     = request.Dto.LastName;
            user.PhoneNumber = request.Dto.PhoneNumber;
            user.Status      = request.Dto.Status ?? user.Status;
            user.RoleName    = request.Dto.RoleName;
            if (request.Dto.CompanyId > 0) user.CompanyId = request.Dto.CompanyId;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ApiResponse<UserDto>.Fail("Failed to update user.", result.Errors.Select(e => e.Description).ToList());

            // Sync role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any()) await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(request.Dto.RoleName))
                await _userManager.AddToRoleAsync(user, request.Dto.RoleName);

            return ApiResponse<UserDto>.Ok(UserMapper.ToDto(user, request.Dto.RoleName), "User updated.");
        }
    }

    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, ApiResponse<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteUserHandler(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public async Task<ApiResponse<bool>> Handle(DeleteUserCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return ApiResponse<bool>.Fail("Failed to delete user.", result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<bool>.Ok(true, "User deleted.");
        }
    }

    public class ResetUserPasswordHandler : IRequestHandler<ResetUserPasswordCommand, ApiResponse<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetUserPasswordHandler(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public async Task<ApiResponse<bool>> Handle(ResetUserPasswordCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
                return ApiResponse<bool>.Fail("Failed to reset password.", result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<bool>.Ok(true, "Password reset successfully.");
        }
    }

    public class AssignUserCompanyHandler : IRequestHandler<AssignUserCompanyCommand, ApiResponse<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;

        public AssignUserCompanyHandler(UserManager<ApplicationUser> userManager, IUnitOfWork uow)
        {
            _userManager = userManager;
            _uow = uow;
        }

        public async Task<ApiResponse<bool>> Handle(AssignUserCompanyCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            // Verify company exists
            var company = await _uow.Repository<Company>()
                .GetByIdAsync(request.CompanyId, ct);
            if (company == null) return ApiResponse<bool>.Fail("Company not found.");

            // Assign user to company (or branch)
            user.CompanyId = request.CompanyId;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ApiResponse<bool>.Fail("Failed to assign user to company.", 
                    result.Errors.Select(e => e.Description).ToList());

            var companyType = company.ParentCompanyId == null ? "head office" : "branch";
            return ApiResponse<bool>.Ok(true, $"User assigned to {companyType} '{company.CompanyName}' successfully.");
        }
    }

    internal static class UserMapper
    {
        public static UserDto ToDto(ApplicationUser u, string? role) => new()
        {
            Id            = u.Id,
            Name          = u.Name,
            Surname       = u.Surname,
            Email         = u.Email,
            PhoneNumber   = u.PhoneNumber,
            Status        = u.Status,
            RoleName      = role,
            CompanyId     = u.CompanyId,
            LastLoginDate = u.LastLoginDate,
            CreatedAt     = u.CreatedAt,
        };
    }
}
