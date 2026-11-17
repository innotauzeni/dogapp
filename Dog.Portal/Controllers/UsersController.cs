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

        /// Turns a raw (never-swallowed) API response into an honest success/message pair —
        /// distinguishes a real failure reason (e.g. "still has related records", a permission
        /// error, or a DB conflict) from a generic fallback that would otherwise mask it.
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

        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Json(new { available = false });

            try
            {
                var allUsers = await _apiClient.GetAsync<ApiResponse<List<UserDto>>>("api/users");
                var users = allUsers?.Data ?? new List<UserDto>();

                // Check if username already exists (case-insensitive)
                var exists = users.Any(u =>
                    u.Username != null &&
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                return Json(new { available = !exists });
            }
            catch
            {
                return Json(new { available = false });
            }
        }
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
                var myCompanyId = Convert.ToInt32(userSession.CompanyId);

                if (userSession.Role == "SystemOwner")
                {
                    // SystemOwner sees everyone
                    allowedCompanyIds = allCompanies.Select(c => c.Id).ToList();
                }
                else if (userSession.IsHeadOffice)
                {
                    if (userSession.CurrentBranchId.HasValue && userSession.CurrentBranchId.Value > 0)
                    {
                        // Head-office user switched to a specific branch → show only that branch
                        allowedCompanyIds.Add(userSession.CurrentBranchId.Value);
                    }
                    else
                    {
                        // Head-office with no branch selected → show head-office + all direct branches
                        allowedCompanyIds.Add(myCompanyId);
                        allowedCompanyIds.AddRange(
                            allCompanies
                                .Where(c => c.ParentCompanyId == myCompanyId)
                                .Select(c => c.Id)
                        );
                    }
                }
                else
                {
                    // Branch user → only own company
                    allowedCompanyIds.Add(myCompanyId);
                }

                // Determine the default company for the "Add User" / "Change Branch" modal
                ViewBag.DefaultCompanyId = userSession.IsHeadOffice && userSession.CurrentBranchId.HasValue
                    ? userSession.CurrentBranchId.Value
                    : myCompanyId;

                // Pass current user ID so the view can exclude self from dropdowns
                ViewBag.CurrentUserId = userSession.Id;

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
                    .Where(u => allowedCompanyIds.Contains(u.CompanyId) && u.Id != userSession.Id)
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
                    username = req.Username,
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
                    username = req.Username,
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
                var raw = await _apiClient.DeleteRawAsync($"api/users/{id}");
                return Json(BuildResult(raw, "Done."));
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
