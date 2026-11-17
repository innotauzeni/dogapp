using Dog.Application.Contracts;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly IFileStorageService _storage;

        public CompaniesController(IApiClient apiClient, IFileStorageService storage)
        {
            _apiClient = apiClient;
            _storage = storage;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        /// Turns a raw (never-swallowed) API response into an honest success/message pair —
        /// distinguishes a real failure reason (e.g. "still has branches", a permission error,
        /// or a DB conflict) from a generic fallback that would otherwise mask it.
        private static object BuildResult(ApiRawResult raw, string successFallback)
        {
            ApiResponse<bool>? parsed = null;
            try { parsed = JsonSerializer.Deserialize<ApiResponse<bool>>(raw.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { /* body wasn't the expected envelope shape — fall through */ }

            if (raw.IsSuccessStatusCode && parsed?.Success == true)
                return new { success = true, message = parsed.Message ?? successFallback };

            if (raw.StatusCode == 401 || raw.StatusCode == 403)
                return new { success = false, message = "You don't have permission to do this." };

            if (parsed?.Message != null)
                return new { success = false, message = parsed.Message };

            var bodySnippet = raw.Body.Length > 200 ? raw.Body[..200] + "…" : raw.Body;
            return new { success = false, message = $"Request failed ({raw.StatusCode}): {bodySnippet}" };
        }

        /// <summary>POST /Companies/UploadLogo — stores a company logo and returns its URL.</summary>
        [HttpPost("UploadLogo")]
        public async Task<IActionResult> UploadLogo(IFormFile? file)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file uploaded." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" }.Contains(ext))
                return Json(new { success = false, message = "Only JPG, PNG, GIF, WEBP or SVG files are allowed." });
            if (file.Length > 3 * 1024 * 1024)
                return Json(new { success = false, message = "Logo must be smaller than 3MB." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var contentType = string.IsNullOrEmpty(file.ContentType) ? "image/png" : file.ContentType;
                string url;

                if (_storage.IsEnabled)
                {
                    try
                    {
                        url = await _storage.UploadBytesAsync(ms.ToArray(), $"logo_{Guid.NewGuid()}{ext}", contentType, "logos");
                    }
                    catch (Exception storageEx)
                    {
                        // Fallback to local storage if cloud fails
                        System.Diagnostics.Debug.WriteLine($"Cloud upload failed: {storageEx.Message}, falling back to local storage");
                        url = await SaveLogoLocally(ms.ToArray(), ext);
                    }
                }
                else
                {
                    url = await SaveLogoLocally(ms.ToArray(), ext);
                }

                return Json(new { success = true, url, message = "Logo uploaded successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logo upload error: {ex}");
                return Json(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }

        private async Task<string> SaveLogoLocally(byte[] fileBytes, string ext)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(dir, name);
            await System.IO.File.WriteAllBytesAsync(fullPath, fileBytes);
            return $"/uploads/logos/{name}";
        }

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
                    req.Website, req.Address, req.Logo, req.Location,
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
                var raw = await _apiClient.DeleteRawAsync($"api/companies/{id}");
                return Json(BuildResult(raw, "Deleted."));
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
