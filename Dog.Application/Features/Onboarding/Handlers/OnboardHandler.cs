using MediatR;
using Microsoft.AspNetCore.Identity;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Onboarding.Commands;
using Dog.Application.Features.Onboarding.DTOs;
using Dog.Core.Entities;

namespace Dog.Application.Features.Onboarding.Handlers
{
    public class OnboardHandler : IRequestHandler<OnboardCompanyCommand, ApiResponse<OnboardResultDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public OnboardHandler(IUnitOfWork uow, UserManager<ApplicationUser> userManager, RoleManager<Role> roleManager)
        {
            _uow = uow;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ApiResponse<OnboardResultDto>> Handle(OnboardCompanyCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Check email not already registered
            if (await _userManager.FindByEmailAsync(dto.Admin.Email) != null)
                return ApiResponse<OnboardResultDto>.Fail("An account with this email already exists.");

            // 2. Create head office company
            var company = new Company
            {
                CompanyName       = dto.Company.CompanyName,
                CompanyCode       = dto.Company.CompanyCode,
                EmailAddress      = dto.Company.EmailAddress,
                PhoneNumber       = dto.Company.PhoneNumber,
                Website           = dto.Company.Website,
                Address           = dto.Company.Address,
                CountryId         = dto.Company.CountryId,
                ProvinceId        = dto.Company.ProvinceId,
                CityId            = dto.Company.CityId,
                DefaultCurrencyId = dto.Company.DefaultCurrencyId,
                IsActive          = true,
                ParentCompanyId   = null,
            };

            await _uow.Repository<Company>().AddAsync(company, cancellationToken);

            // 3. Create main branch if provided
            if (dto.MainBranch != null)
            {
                var branch = new Company
                {
                    CompanyName = dto.MainBranch.CompanyName,
                    CompanyCode = dto.MainBranch.CompanyCode,
                    Location = dto.MainBranch.Location,
                    PhoneNumber = dto.MainBranch.PhoneNumber,
                    EmailAddress = dto.MainBranch.EmailAddress,
                    CountryId = dto.Company.CountryId,
                    ProvinceId  = dto.Company.ProvinceId,
                    CityId  = dto.Company.CityId,
                    DefaultCurrencyId = dto.Company.DefaultCurrencyId,
                    IsActive = true,
                    ParentCompanyId = company.Id,
                };

                await _uow.Repository<Company>().AddAsync(branch, cancellationToken);
            }

            // 4. Ensure Admin role exists
            const string adminRole = "Administrator";
            if (!await _roleManager.RoleExistsAsync(adminRole))
                await _roleManager.CreateAsync(new Role { Name = adminRole });

            // 5. Create admin user
            var user = new ApplicationUser
            {
                UserName = dto.Admin.Email,
                Email = dto.Admin.Email,
                Name = dto.Admin.FirstName,
                Surname = dto.Admin.LastName,
                CompanyId = company.Id,
                Status = "ACTIVE",
                EmailConfirmed = true,
            };

            var result = await _userManager.CreateAsync(user, dto.Admin.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<OnboardResultDto>.Fail("Failed to create admin user.", errors);
            }

            await _userManager.AddToRoleAsync(user, adminRole);

            return ApiResponse<OnboardResultDto>.Ok(new OnboardResultDto
            {
                CompanyId = company.Id,
                CompanyName = company.CompanyName!,
                AdminEmail = user.Email!,
            }, "Company registered successfully. You can now sign in.");
        }
    }
}
