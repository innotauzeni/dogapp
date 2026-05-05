using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Auth.Commands;
using Dog.Application.Features.Auth.DTOs;
using Dog.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Dog.Application.Services.IErrorLoggingService;

namespace Dog.Application.Features.Auth.Handlers
{
    public class LoginHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _uow;

        public LoginHandler(UserManager<ApplicationUser> userManager, RoleManager<Role> roleManager,
            IJwtService jwtService, IUnitOfWork uow)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _uow = uow;
        }

        public async Task<ApiResponse<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var identifier = request.Dto.Identifier.Trim();

            // Resolve user by email, username, or phone number
            var user = await _userManager.FindByEmailAsync(identifier)
                    ?? await _userManager.FindByNameAsync(identifier)
                    ?? await _userManager.Users
                           .FirstOrDefaultAsync(u => u.PhoneNumber == identifier, cancellationToken);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Dto.Password))
                return ApiResponse<LoginResponseDto>.Fail("Invalid credentials.");

            if (user.Status != "ACTIVE")
                return ApiResponse<LoginResponseDto>.Fail("Account is inactive. Please contact support.");

            var roleNames = await _userManager.GetRolesAsync(user);

            // Load all role IDs for this user
            var roles = await _roleManager.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .ToListAsync(cancellationToken);

            var roleIds = roles.Select(r => r.Id).ToList();

            // Load permissions via RolePermission → Permission
            var permissions = await _uow.Repository<RolePermission>()
                .TableNoTracking
                .Include(rp => rp.Permission)
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Load menus via RoleMenu → SystemModule → Submodules
            var menus = await _uow.Repository<RoleMenu>()
                .TableNoTracking
                .Include(rm => rm.Menu)
                    .ThenInclude(m => m.childrens)
                .Where(rm => roleIds.Contains(rm.RoleId))
                .Select(rm => rm.Menu)
                .Distinct()
                .OrderBy(m => m.SortOrder)
                .ToListAsync(cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user, roleNames, permissions!);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();

            await _uow.Repository<RefreshToken>().AddAsync(new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            }, cancellationToken);

            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Load branches if user is head office (ParentCompanyId == null)
            var company = await _uow.Repository<Company>()
                .TableNoTracking
                .FirstOrDefaultAsync(c => c.Id == user.CompanyId, cancellationToken);

            var isHeadOffice = company?.ParentCompanyId == null;

            var branches = isHeadOffice
                ? await _uow.Repository<Company>()
                    .TableNoTracking
                    .Where(c => c.ParentCompanyId == company!.Id && c.IsActive)
                    .Select(c => new BranchDto { Id = c.Id, CompanyName = c.CompanyName, CompanyCode = c.CompanyCode, Location = c.Location, IsActive = c.IsActive })
                    .ToListAsync(cancellationToken)
                : new List<BranchDto>();

            return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    CompanyId = user.CompanyId.ToString(),
                    Email = user.Email!,
                    Name = $"{user.Name} {user.Surname}".Trim(),
                    Role = roleNames.FirstOrDefault()
                },
                Permissions = permissions!,
                IsHeadOffice = isHeadOffice,
                Branches = branches,
                Menus = menus.Select(m => new MenuNodeDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Icon = m.Icon,
                    AccessName = m.AccessName,
                    SortOrder = m.SortOrder,
                    Children = m.childrens.OrderBy(s => s.Order).Select(s => new SubMenuNodeDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Icon = s.Icon,
                        Route = s.Route,
                        AccessName = s.AccessName,
                        Order = s.Order
                    }).ToList()
                }).ToList()
            });
        }
    }

    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<LoginResponseDto>>
    {
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public RefreshTokenHandler(IJwtService jwtService, IUnitOfWork uow,
            UserManager<ApplicationUser> userManager, RoleManager<Role> roleManager)
        {
            _jwtService = jwtService;
            _uow = uow;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ApiResponse<LoginResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var repo = _uow.Repository<RefreshToken>();
            var stored = repo.TableNoTracking
                .FirstOrDefault(r => r.Token == request.Dto.RefreshToken && !r.IsRevoked);

            if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
                return ApiResponse<LoginResponseDto>.Fail("Invalid or expired refresh token.");

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user == null)
                return ApiResponse<LoginResponseDto>.Fail("User not found.");

            stored.IsRevoked = true;
            await repo.UpdateAsync(stored, cancellationToken);

            var roleNames = await _userManager.GetRolesAsync(user);
            var roles = await _roleManager.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .ToListAsync(cancellationToken);
            var roleIds = roles.Select(r => r.Id).ToList();

            var permissions = await _uow.Repository<RolePermission>()
                .TableNoTracking
                .Include(rp => rp.Permission)
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync(cancellationToken);

            var newAccess = _jwtService.GenerateAccessToken(user, roleNames, permissions!);
            var newRefresh = _jwtService.GenerateRefreshToken();

            await repo.AddAsync(new RefreshToken
            {
                Token = newRefresh,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            }, cancellationToken);

            return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
            {
                AccessToken = newAccess,
                RefreshToken = newRefresh,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    Name = $"{user.Name} {user.Surname}".Trim(),
                    Role = roleNames.FirstOrDefault()
                },
                Permissions = permissions!
            });
        }
    }
}
