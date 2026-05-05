using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class UsersController : Controller
    {
        private readonly IApiClient _apiClient;
        public UsersController(IApiClient apiClient) => _apiClient = apiClient;

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));
        public async Task<IActionResult> Index(string? search, string? status, string? role, int page = 1, int pageSize = 10)
        {
            // ✅ Get session FIRST
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

            if (!IsAuthenticated() || userSession == null)
                return RedirectToAction("Login", "Auth");

            var vm = new UsersListViewModel
            {
                SearchTerm = search,
                StatusFilter = status,
                RoleFilter = role,
                Page = page,
                PageSize = pageSize
            };

            try
            {
                var t1 = _apiClient.GetAsync<ApiResponse<List<UserDto>>>("api/users");
                var t2 = _apiClient.GetAsync<ApiResponse<List<RoleDto>>>("api/roles/GetRoles");
                var t3 = _apiClient.GetAsync<ApiResponse<List<CompanyDropdownDto>>>("api/companies");
                //var t4 = (await t2)?.Data!.Where(q=>q.Name != "SystemOwner").ToList();
                await Task.WhenAll(t1, t2, t3);

                var allUsers = (await t1)?.Data ?? new List<UserDto>();
                var allCompanies = (await t3)?.Data ?? new List<CompanyDropdownDto>();
                var datarole = (await t2)?.Data ?? new List<RoleDto>();
                if (userSession.Role == "SystemOwner")
                {
                    vm.AllRoles = (await t2)?.Data ?? new List<RoleDto>();
                }
                else if (userSession.Role == "Admin" || userSession.Role == "Administrator")
                {
                    vm.AllRoles = datarole.Where(q => q.Name != "SystemOwner" && q.Name != "Admin").ToList();
                }

                // =========================================
                // 🔥 STEP 1: BUILD ALLOWED COMPANY IDS
                // =========================================
                List<int> allowedCompanyIds = new List<int>();

                if (userSession.Role == "SystemOwner")
                {
                    allowedCompanyIds = allCompanies.Select(c => c.Id).ToList();
                }
                else if (userSession.Role == "Admin" || userSession.Role == "Administrator")
                {
                    var myCompany = allCompanies.FirstOrDefault(c => c.Id == Convert.ToInt32(userSession.CompanyId));

                    if (myCompany != null)
                    {
                        allowedCompanyIds.Add(myCompany.Id);

                        // include direct branches
                        allowedCompanyIds.AddRange(
                            allCompanies
                                .Where(c => c.ParentCompanyId == myCompany.Id)
                                .Select(c => c.Id)
                        );
                    }
                }
                else
                {
                    // fallback: only own company
                    allowedCompanyIds.Add(Convert.ToInt32(userSession.CompanyId));
                }

                // =========================================
                // 🔥 STEP 2: FILTER COMPANIES (UI DROPDOWN)
                // =========================================
                vm.AllCompanies = allCompanies
                    .Where(c => allowedCompanyIds.Contains(c.Id))
                    .ToList();

                // =========================================
                // 🔥 STEP 3: FILTER USERS
                // =========================================
                allUsers = allUsers
                    .Where(u => allowedCompanyIds.Contains(u.CompanyId))
                    .ToList();

                // =========================================
                // 🔍 SEARCH
                // =========================================
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.Trim();
                    allUsers = allUsers.Where(u =>
                        (u.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (u.Surname?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (u.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (u.PhoneNumber?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                // =========================================
                // 🔍 STATUS FILTER
                // =========================================
                if (!string.IsNullOrWhiteSpace(status))
                {
                    allUsers = allUsers
                        .Where(u => string.Equals(u.Status, status, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // =========================================
                // 🔍 ROLE FILTER
                // =========================================
                if (!string.IsNullOrWhiteSpace(role))
                {
                    allUsers = allUsers
                        .Where(u => string.Equals(u.RoleName, role, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // =========================================
                // 📄 PAGINATION
                // =========================================
                vm.TotalCount = allUsers.Count;

                vm.Users = allUsers
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load users. Please try again.";
            }

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<UserDto>>("api/users", new
                {
                    firstName = req.FirstName,
                    lastName = req.LastName,
                    email = req.Email,
                    password = req.Password,
                    phoneNumber = req.PhoneNumber,
                    roleName = req.RoleName,
                    companyId = req.CompanyId
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<UserDto>>($"api/users/{id}", new
                {
                    id,
                    firstName = req.FirstName,
                    lastName = req.LastName,
                    phoneNumber = req.PhoneNumber,
                    status = req.Status,
                    roleName = req.RoleName,
                    companyId = req.CompanyId
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/users/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>($"api/users/{id}/reset-password", req.NewPassword);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                // Fetch current user data first so we don't wipe existing fields
                var current = await _apiClient.GetAsync<ApiResponse<UserDto>>($"api/users/{id}");
                var u = current?.Data;
                if (u == null) return Json(new { success = false, message = "User not found." });

                var resp = await _apiClient.PutAsync<ApiResponse<UserDto>>($"api/users/{id}", new
                {
                    id,
                    firstName = u.Name ?? "",
                    lastName = u.Surname ?? "",
                    phoneNumber = u.PhoneNumber,
                    status = u.Status,
                    roleName = req.RoleName,
                    companyId = u.CompanyId
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeCompany(string id, [FromBody] ChangeCompanyRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<bool>>($"api/users/{id}/assign-company/{req.CompanyId}", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}
