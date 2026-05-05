using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class SystemActionsController : Controller
    {
        private readonly IApiClient _apiClient;

        public SystemActionsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        public async Task<IActionResult> Index(string? search, string? controller, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            
            var vm = new SystemActionsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            // Pass filter values to ViewBag for form state
            ViewBag.ControllerFilter = controller;

            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(controller)) queryParams.Add($"controller={controller}");
                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                var queryString = string.Join("&", queryParams);
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/SystemActions?{queryString}");
                
                if (resp?.Success == true && resp.Data != null)
                {
                    // The API returns an object, we need to deserialize it to a list
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                    var systemActions = System.Text.Json.JsonSerializer.Deserialize<List<SystemActionItemDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    vm.SystemActionList = systemActions ?? new List<SystemActionItemDto>();
                    vm.TotalCount = vm.SystemActionList.Count;

                    // Apply search filter on client side if needed
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        vm.SystemActionList = vm.SystemActionList.Where(action =>
                            (action.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (action.Controller?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (action.Action?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToList();
                        vm.TotalCount = vm.SystemActionList.Count;
                    }

                    // Apply controller filter
                    if (!string.IsNullOrWhiteSpace(controller))
                    {
                        vm.SystemActionList = vm.SystemActionList.Where(action =>
                            action.Controller?.Equals(controller, StringComparison.OrdinalIgnoreCase) ?? false)
                            .ToList();
                        vm.TotalCount = vm.SystemActionList.Count;
                    }
                }

                // Get roles for role-action assignments
                var rolesResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Roles");
                if (rolesResp?.Success == true && rolesResp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(rolesResp.Data);
                    var roles = System.Text.Json.JsonSerializer.Deserialize<List<RoleItemDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    vm.RolesList = roles ?? new List<RoleItemDto>();
                }

                // Get role-actions
                var roleActionsResp = await _apiClient.GetAsync<ApiResponse<object>>("api/SystemActions/role-actions");
                if (roleActionsResp?.Success == true && roleActionsResp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(roleActionsResp.Data);
                    var roleActions = System.Text.Json.JsonSerializer.Deserialize<List<SystemRoleActionItemDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    vm.RoleActionsList = roleActions ?? new List<SystemRoleActionItemDto>();
                }
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Unable to load system actions. Error: {ex.Message}";
                // Log the full exception for debugging
                Console.WriteLine($"SystemActions Error: {ex}");
            }

            return View(vm);
        }

        // POST /SystemActions/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSystemActionRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Controller) || string.IsNullOrWhiteSpace(req.Action)) 
                return Json(new { success = false, message = "Name, Controller, and Action are required." });
            
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<SystemActionItemDto>>("api/SystemActions", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemActions/Update/{id}
        [HttpPost]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSystemActionRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Controller) || string.IsNullOrWhiteSpace(req.Action)) 
                return Json(new { success = false, message = "Name, Controller, and Action are required." });
            
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<SystemActionItemDto>>($"api/SystemActions/{id}", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemActions/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/SystemActions/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemActions/AssignRole
        [HttpPost]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleActionRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null || string.IsNullOrWhiteSpace(req.RoleId) || req.SystemActionId <= 0) 
                return Json(new { success = false, message = "Role and System Action are required." });
            
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>("api/SystemActions/role-actions", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Role assigned successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemActions/RemoveRoleAction/{id}
        [HttpPost]
        public async Task<IActionResult> RemoveRoleAction(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/SystemActions/role-actions/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Role assignment removed successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}