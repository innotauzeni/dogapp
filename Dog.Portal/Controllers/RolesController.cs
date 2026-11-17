using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    public class RolesController : Controller
    {
        private readonly IApiClient _apiClient;

        public RolesController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        // GET /Roles
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new RolesListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<RoleDto>>>("api/roles");
                var all = resp?.Data ?? [];

                if (!string.IsNullOrWhiteSpace(search))
                {
                    all = all.Where(r =>
                        r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (r.Type?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Level?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                vm.TotalCount = all.Count;
                vm.Roles = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load roles. Please try again.";
            }

            return View(vm);
        }

        // GET /Roles/Assign/{id}
        public async Task<IActionResult> Assign(string id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                // Load role, all permissions, all modules, and current assignments in parallel
                var roleTask    = _apiClient.GetAsync<ApiResponse<RoleDto>>($"api/roles/{id}");
                var permsTask   = _apiClient.GetAsync<ApiResponse<List<PermissionDto>>>("api/permissions");
                var modulesTask = _apiClient.GetAsync<ApiResponse<List<SystemModuleDto>>>("api/systemmodules");
                var assignedPermsTask   = _apiClient.GetAsync<ApiResponse<RolePermissionsDto>>($"api/roleaccess/{id}/permissions");
                var assignedMenusTask   = _apiClient.GetAsync<ApiResponse<RoleMenusDto>>($"api/roleaccess/{id}/menus");

                await Task.WhenAll(roleTask, permsTask, modulesTask, assignedPermsTask, assignedMenusTask);

                var role = (await roleTask)?.Data;
                if (role == null) return NotFound();

                var allPerms   = (await permsTask)?.Data   ?? [];
                var allModules = (await modulesTask)?.Data  ?? [];
                var assignedPerms  = (await assignedPermsTask)?.Data;
                var assignedMenus  = (await assignedMenusTask)?.Data;

                // Group permissions by their Module field
                var groups = allPerms
                    .GroupBy(p => p.Module)
                    .OrderBy(g => g.Key)
                    .Select(g => new PermissionGroupModel
                    {
                        Module = g.Key,
                        Permissions = g.OrderBy(p => p.Name).ToList()
                    })
                    .ToList();

                var vm = new RoleAssignViewModel
                {
                    Role = role,
                    PermissionGroups = groups,
                    AllModules = allModules.OrderBy(m => m.SortOrder).ToList(),
                    AssignedPermissionIds = assignedPerms?.AssignedPermissionIds ?? [],
                    AssignedModuleIds     = assignedMenus?.AssignedModuleIds     ?? []
                };

                return View(vm);
            }
            catch
            {
                TempData["Error"] = "Unable to load role assignment data.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST /Roles/SavePermissions  (called by JS fetch)
        [HttpPost]
        public async Task<IActionResult> SavePermissions([FromBody] SavePermissionsRequest req)
        {
            if (!IsAuthenticated())
                return Json(new { success = false, message = "Session expired." });

            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>(
                    "api/roleaccess/assign-permissions",
                    new { roleId = req.RoleId, permissionIds = req.PermissionIds });

                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Saved." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Roles/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/roles", new { name = req.Name, type = req.Type, level = req.Level });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Roles/Edit
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] EditRoleRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/roles/{req.Id}", new { id = req.Id, name = req.Name, type = req.Type, level = req.Level });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Roles/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/roles/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Roles/SaveMenus  (called by JS fetch)
        [HttpPost]
        public async Task<IActionResult> SaveMenus([FromBody] SaveMenusRequest req)
        {
            if (!IsAuthenticated())
                return Json(new { success = false, message = "Session expired." });

            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>(
                    "api/roleaccess/assign-menus",
                    new { roleId = req.RoleId, moduleIds = req.ModuleIds });

                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Saved." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
