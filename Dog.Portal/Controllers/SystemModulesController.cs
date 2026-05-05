using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    public class SystemModulesController : Controller
    {
        private readonly IApiClient _apiClient;

        public SystemModulesController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        // GET /SystemModules
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new ModulesListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            try
            {
                var response = await _apiClient.GetAsync<ApiResponse<List<SystemModuleDto>>>("api/systemmodules");
                var all = response?.Data ?? new List<SystemModuleDto>();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    all = all.Where(m =>
                        m.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (m.AccessName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (m.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                vm.TotalCount = all.Count;
                vm.Modules = all.OrderBy(m => m.SortOrder)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load system modules. Please try again.";
            }

            return View(vm);
        }

        // GET /SystemModules/{id}
        public async Task<IActionResult> Details(int id, string? search, int page = 1, int pageSize = 10)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var modResp = await _apiClient.GetAsync<ApiResponse<SystemModuleDto>>($"api/systemmodules/{id}");
                var module = modResp?.Data;
                if (module == null) return NotFound();

                var subResp = await _apiClient.GetAsync<ApiResponse<List<SubmoduleDetailDto>>>($"api/submodules/by-module/{id}");
                var all = subResp?.Data ?? new List<SubmoduleDetailDto>();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    all = all.Where(s =>
                        (s.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.Route?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.AccessName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                var vm = new ModuleDetailViewModel
                {
                    Module = module,
                    SearchTerm = search,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = all.Count,
                    Submodules = all.OrderBy(s => s.Order)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToList()
                };

                return View(vm);
            }
            catch
            {
                TempData["Error"] = "Unable to load module details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET /SystemModules/Permissions/{id}
        public async Task<IActionResult> Permissions(int id, string? search, int page = 1, int pageSize = 10)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var subResp = await _apiClient.GetAsync<ApiResponse<SubmoduleDetailDto>>($"api/submodules/{id}");
                var sub = subResp?.Data;
                if (sub == null) return NotFound();

                SystemModuleDto? module = null;
                if (sub.ModuleId > 0)
                {
                    var modResp = await _apiClient.GetAsync<ApiResponse<SystemModuleDto>>($"api/systemmodules/{sub.ModuleId}");
                    module = modResp?.Data;
                }

                // Permission.Module stores the parent SystemModule's Name (e.g. "Configuration").
                // Submodule.AccessName is the view-guard key (e.g. "systemmodules.view").
                // Permissions are named with a shared prefix:  "systemmodules.view / .create / .edit / .delete"
                // Strategy:
                //   1. Extract prefix from AccessName ("systemmodules.view" → "systemmodules")
                //   2. Filter all permissions where Name starts with that prefix
                //   3. Fallback: show all permissions whose Module == parent module Name

                var allPermResp = await _apiClient.GetAsync<ApiResponse<List<PermissionDto>>>("api/permissions");
                var allPerms = allPermResp?.Data ?? new List<PermissionDto>();

                // "systemmodules.view" → "systemmodules"
                var accessPrefix = sub.AccessName?.Split('.').FirstOrDefault() ?? "";

                List<PermissionDto> all;
                if (!string.IsNullOrEmpty(accessPrefix))
                {
                    all = allPerms
                        .Where(p => p.Name.StartsWith(accessPrefix + ".", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Fallback: show whole module's permissions if prefix matches nothing
                    if (all.Count == 0 && module != null)
                    {
                        all = allPerms
                            .Where(p => p.Module.Equals(module.Name, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }
                else
                {
                    all = [];
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    all = all.Where(p =>
                        p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.Module.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var vm = new SubmodulePermissionsViewModel
                {
                    Submodule = sub,
                    Module = module,
                    SearchTerm = search,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = all.Count,
                    Permissions = all.Skip((page - 1) * pageSize).Take(pageSize).ToList()
                };

                return View(vm);
            }
            catch
            {
                TempData["Error"] = "Unable to load permissions.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST /SystemModules/CreateModule
        [HttpPost]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<SystemModuleDto>>("api/systemmodules", new
                {
                    name = req.Name,
                    icon = req.Icon,
                    description = req.Description,
                    accessName = req.AccessName,
                    sortOrder = req.SortOrder,
                    isActive = req.IsActive
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/UpdateModule/{id}
        [HttpPost]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] UpdateModuleRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<SystemModuleDto>>($"api/systemmodules/{id}", new
                {
                    id,
                    name = req.Name,
                    icon = req.Icon,
                    description = req.Description,
                    accessName = req.AccessName,
                    sortOrder = req.SortOrder,
                    isActive = req.IsActive
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/DeleteModule/{id}
        [HttpPost]
        public async Task<IActionResult> DeleteModule(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/systemmodules/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/CreateSubmodule
        [HttpPost]
        public async Task<IActionResult> CreateSubmodule([FromBody] CreateSubmoduleRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<SubmoduleDetailDto>>("api/submodules", new
                {
                    name = req.Name,
                    moduleId = req.ModuleId,
                    icon = req.Icon,
                    route = req.Route,
                    accessName = req.AccessName,
                    order = req.Order
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/UpdateSubmodule/{id}
        [HttpPost]
        public async Task<IActionResult> UpdateSubmodule(int id, [FromBody] UpdateSubmoduleRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<SubmoduleDetailDto>>($"api/submodules/{id}", new
                {
                    id,
                    name = req.Name,
                    moduleId = req.ModuleId,
                    icon = req.Icon,
                    route = req.Route,
                    accessName = req.AccessName,
                    order = req.Order
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/DeleteSubmodule/{id}
        [HttpPost]
        public async Task<IActionResult> DeleteSubmodule(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/submodules/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/CreatePermission
        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<PermissionDto>>("api/permissions", new
                {
                    name = req.Name,
                    displayName = req.DisplayName,
                    module = req.Module
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/UpdatePermission/{id}
        [HttpPost]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] UpdatePermissionRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<PermissionDto>>($"api/permissions/{id}", new
                {
                    id,
                    name = req.Name,
                    displayName = req.DisplayName,
                    module = req.Module
                });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemModules/DeletePermission/{id}
        [HttpPost]
        public async Task<IActionResult> DeletePermission(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/permissions/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Done." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }

    // Request models
    public class CreateModuleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccessName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateModuleRequest : CreateModuleRequest { }

    public class CreateSubmoduleRequest
    {
        public string Name { get; set; } = string.Empty;
        public int ModuleId { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string AccessName { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class UpdateSubmoduleRequest : CreateSubmoduleRequest { }

    public class CreatePermissionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }

    public class UpdatePermissionRequest : CreatePermissionRequest { }
}
