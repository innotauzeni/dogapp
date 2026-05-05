using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly IApiClient _apiClient;

        public CompaniesController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        // GET /Companies
        public async Task<IActionResult> Index(string? search, string filterType = "all", int page = 1, int pageSize = 10)
        {
            // ✅ Get session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new CompaniesListViewModel
            {
                SearchTerm = search,
                FilterType = filterType,
                Page = page,
                PageSize = pageSize
            };

            try
            {
                // Load companies + dropdown data in parallel
                var companiesTask  = _apiClient.GetAsync<ApiResponse<List<CompanyDto>>>("api/companies");
                var countriesTask  = _apiClient.GetAsync<ApiResponse<List<CountryOption>>>("api/country/GetPublicCountries");
                var currenciesTask = _apiClient.GetAsync<ApiResponse<List<CurrencyOption>>>("api/Currency/GetPubicCurrencies");

                await Task.WhenAll(companiesTask, countriesTask, currenciesTask);

                var all = (await companiesTask)?.Data ?? [];
                vm.Countries = (await countriesTask)?.Data ?? [];
                vm.Currencies = (await currenciesTask)?.Data ?? [];
                

                // 🔥 ROLE-BASED FILTERING
                List<CompanyDto> filteredCompanies;


                if (userSession.Role == "SystemOwner")
                {
                    filteredCompanies = all;


                    vm.HeadOffices = filteredCompanies.Where(c => c.ParentCompanyId == null).ToList();
                    vm.HeadOfficeCount = filteredCompanies.Count(c => c.ParentCompanyId == null);
                    vm.BranchCount = filteredCompanies.Count(c => c.ParentCompanyId != null);

                    // Main list shows head offices only — branches are visible on the Details page.
                    var headOffices = filteredCompanies.Where(c => c.ParentCompanyId == null).ToList();

                    // Search
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        headOffices = headOffices.Where(c =>
                            (c.CompanyName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.CompanyCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.EmailAddress?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.Location?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.CountryName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToList();
                    }

                    vm.TotalCount = headOffices.Count;
                    vm.Companies = headOffices.Skip((page - 1) * pageSize).Take(pageSize).ToList();


                }
                else if (userSession.Role == "Admin" || userSession.Role == "Administrator")
                {
                    filteredCompanies = all
                        .Where(c => c.Id == Convert.ToInt32(userSession.CompanyId))
                        .ToList();



                    vm.HeadOffices = filteredCompanies.Where(c => c.ParentCompanyId == null).ToList();
                    vm.HeadOfficeCount = filteredCompanies.Count(c => c.ParentCompanyId == null);
                    vm.BranchCount = filteredCompanies.Count(c => c.ParentCompanyId != null);

                    // Main list shows head offices only — branches are visible on the Details page.
                    var headOffices = filteredCompanies.Where(c => c.ParentCompanyId == null).ToList();

                    // Search
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        headOffices = headOffices.Where(c =>
                            (c.CompanyName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.CompanyCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.EmailAddress?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.Location?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (c.CountryName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToList();
                    }

                    vm.TotalCount = headOffices.Count;
                    vm.Companies = headOffices.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }
                else
                {
                    filteredCompanies = new List<CompanyDto>();
                }


                
            }
            catch
            {
                ViewBag.FetchError = "Unable to load companies. Please try again.";
            }

            return View(vm);
        }

        // GET /Companies/Details/{id}
        public async Task<IActionResult> Details(int id, string? search, int page = 1, int pageSize = 10)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var compResp     = await _apiClient.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{id}");
                var branchesResp = await _apiClient.GetAsync<ApiResponse<List<CompanyDto>>>($"api/companies/{id}/branches");

                var company = compResp?.Data;
                if (company == null) return NotFound();

                var all = branchesResp?.Data ?? [];

                if (!string.IsNullOrWhiteSpace(search))
                {
                    all = all.Where(c =>
                        (c.CompanyName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.CompanyCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Location?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                var vm = new CompanyDetailViewModel
                {
                    Company = company,
                    SearchTerm = search,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = all.Count,
                    Branches = all.Skip((page - 1) * pageSize).Take(pageSize).ToList()
                };

                return View(vm);
            }
            catch
            {
                TempData["Error"] = "Unable to load company details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET /Companies/GetCompany/{id}  — AJAX used by edit modal
        [HttpGet]
        public async Task<IActionResult> GetCompany(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{id}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data });
            }
            catch { return Json(new { success = false }); }
        }

        // GET /Companies/Provinces/{countryId}  — AJAX for cascading dropdown
        [HttpGet]
        public async Task<IActionResult> Provinces(int countryId)
        {
            if (!IsAuthenticated()) return Json(new List<ProvinceOption>());
            if (countryId <= 0) return Json(new List<ProvinceOption>());
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<ProvinceOption>>>($"api/Country/GetProvincesByCountry/{countryId}");
                return Json(resp?.Data ?? new List<ProvinceOption>());
            }
            catch (Exception ex)
            {
                return Json(new List<ProvinceOption>());
            }
        }

        // GET /Companies/Cities/{provinceId}  — AJAX for cascading dropdown
        [HttpGet]
        public async Task<IActionResult> Cities(int provinceId)
        {
            if (!IsAuthenticated()) return Json(new List<CityOption>());
            if (provinceId <= 0) return Json(new List<CityOption>());
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<CityOption>>>($"api/Country/GetCitiesByProvince/{provinceId}");
                return Json(resp?.Data ?? new List<CityOption>());
            }
            catch (Exception ex)
            {
                return Json(new List<CityOption>());
            }
        }

        // POST /Companies/Create
        // Creates the company, and optionally its first Admin user in one shot.
        // If AdminEmail is provided, the user becomes the Admin of the new company.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });

            try
            {
                // 1. Create the company (trim out admin fields — they're not on the API DTO)
                var companyPayload = new
                {
                    req.CompanyName, req.CompanyCode, req.EmailAddress, req.PhoneNumber,
                    req.Website, req.Address, req.Location,
                    req.CountryId, req.ProvinceId, req.CityId, req.DefaultCurrencyId,
                    req.ParentCompanyId, req.IsActive
                };

                var compResp = await _apiClient.PostAsync<ApiResponse<CompanyDto>>("api/companies", companyPayload);
                if (compResp?.Success != true || compResp.Data == null)
                    return Json(new { success = false, message = compResp?.Message ?? "Failed to create company." });

                var newCompany = compResp.Data;

                // 2. If admin details supplied, create the first Admin user for this company.
                //    Only done for head offices (no parent) — branches inherit users from the HQ.
                var createAdmin = req.ParentCompanyId == null
                                  && !string.IsNullOrWhiteSpace(req.AdminEmail)
                                  && !string.IsNullOrWhiteSpace(req.AdminPassword);

                if (createAdmin)
                {
                    var userPayload = new
                    {
                        firstName   = req.AdminFirstName ?? string.Empty,
                        lastName    = req.AdminLastName  ?? string.Empty,
                        email       = req.AdminEmail,
                        password    = req.AdminPassword,
                        phoneNumber = req.AdminPhone,
                        roleName    = "Administrator",
                        companyId   = newCompany.Id
                    };

                    var userResp = await _apiClient.PostAsync<ApiResponse<object>>("api/users", userPayload);
                    if (userResp?.Success != true)
                    {
                        // Company was created but admin user failed — surface a clear partial-success message.
                        return Json(new
                        {
                            success = true,
                            partial = true,
                            message = $"Company \"{newCompany.CompanyName}\" created, but the Admin user failed: {userResp?.Message ?? "unknown error"}.",
                            data    = newCompany
                        });
                    }

                    return Json(new
                    {
                        success = true,
                        message = $"Company and Admin user \"{req.AdminEmail}\" created successfully.",
                        data    = newCompany
                    });
                }

                return Json(new { success = true, message = compResp.Message ?? "Company created.", data = newCompany });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Companies/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCompanyRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<CompanyDto>>($"api/companies/{req.Id}", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Companies/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/companies/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Companies/ToggleStatus/{id}
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>($"api/companies/{id}/toggle-status", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Status updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}
