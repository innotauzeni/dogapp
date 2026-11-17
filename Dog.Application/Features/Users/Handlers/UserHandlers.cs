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
    internal static class UserRoleGuard
    {
        /// SystemOwner and Admin are platform-level roles — only an existing SystemOwner
        /// may grant either, whether creating a new user or changing an existing one's role.
        public static bool IsRestrictedRole(string? roleName) =>
            string.Equals(roleName, "SystemOwner", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
    }

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
            if (!request.CallerIsSystemOwner && UserRoleGuard.IsRestrictedRole(request.Dto.RoleName))
                return ApiResponse<UserDto>.Fail("Only a SystemOwner can assign the SystemOwner or Admin role.");

            if (await _userManager.FindByEmailAsync(request.Dto.Email) != null)
                return ApiResponse<UserDto>.Fail("Email already in use.");

            var username = string.IsNullOrWhiteSpace(request.Dto.Username) ? request.Dto.Email : request.Dto.Username.Trim();
            if (await _userManager.FindByNameAsync(username) != null)
                return ApiResponse<UserDto>.Fail("Username already in use.");

            var user = new ApplicationUser
            {
                UserName       = username,
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
            if (!request.CallerIsSystemOwner && UserRoleGuard.IsRestrictedRole(request.Dto.RoleName))
                return ApiResponse<UserDto>.Fail("Only a SystemOwner can assign the SystemOwner or Admin role.");

            var user = await _userManager.FindByIdAsync(request.Dto.Id);
            if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

            if (!string.IsNullOrWhiteSpace(request.Dto.Username) && request.Dto.Username.Trim() != user.UserName)
            {
                var newUsername = request.Dto.Username.Trim();
                var existing = await _userManager.FindByNameAsync(newUsername);
                if (existing != null && existing.Id != user.Id)
                    return ApiResponse<UserDto>.Fail("Username already in use.");

                var renameResult = await _userManager.SetUserNameAsync(user, newUsername);
                if (!renameResult.Succeeded)
                    return ApiResponse<UserDto>.Fail("Failed to update username.", renameResult.Errors.Select(e => e.Description).ToList());
            }

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
        private readonly IUnitOfWork _uow;

        public DeleteUserHandler(UserManager<ApplicationUser> userManager, IUnitOfWork uow)
        {
            _userManager = userManager;
            _uow = uow;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteUserCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null) return ApiResponse<bool>.Fail("User not found.");

            try
            {
                // Detach this user from records that reference them instead of blocking the
                // delete on them — the care logs / assignments / documents are real business
                // history and should survive; only the "who did this" attribution is lost.
                // Login sessions are hard-deleted outright since they're meaningless without
                // the user. This runs before UserManager.DeleteAsync so no FK reference is left
                // for MySQL to reject the delete over.
                await _uow.Repository<RefreshToken>().Table
                    .Where(t => t.UserId == request.Id)
                    .ExecuteDeleteAsync(ct);
                await _uow.Repository<DogCareRecord>().Table
                    .Where(r => r.RecordedByUserId == request.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.RecordedByUserId, (string?)null), ct);
                await _uow.Repository<DogAssignment>().Table
                    .Where(a => a.HandlerUserId == request.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.HandlerUserId, (string?)null), ct);
                await _uow.Repository<DocumentAttachment>().Table
                    .Where(d => d.UploadedByUserId == request.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.UploadedByUserId, (string?)null), ct);

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return ApiResponse<bool>.Fail("Failed to delete user.", result.Errors.Select(e => e.Description).ToList());

                return ApiResponse<bool>.Ok(true, "User deleted.");
            }
            catch (DbUpdateException)
            {
                // Some other, unanticipated reference still points at this user — MySQL rejects
                // the delete via a foreign-key constraint. Previously this bubbled up as an
                // opaque 500 that looked like the delete silently did nothing.
                return ApiResponse<bool>.Fail("Can't delete this user — they still have related records that couldn't be automatically reassigned. Contact support.");
            }
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
            Username      = u.UserName,
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
