using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Dog.Application.Features.BreedGroups.DTOs;
using Dog.Application.Features.Breeds.DTOs;

namespace Dog.Portal.Controllers
{
    public class BreedGroupsController : Controller
    {
        private readonly IApiClient _apiClient;

        public BreedGroupsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 15)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            
            var vm = new BreedGroupsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            // Pass filter values to ViewBag for form state
            ViewBag.StatusFilter = status;

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<BreedGroupDto>>>($"api/BreedGroup/GetBreedGroups?CompanyId={Convert.ToInt32(userSession.CompanyId)}");
                var all = resp?.Data ?? [];

                // Apply filters
                var filteredBreedGroups = all.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredBreedGroups = filteredBreedGroups.Where(c =>
                        (c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "active")
                        filteredBreedGroups = filteredBreedGroups.Where(c => c.IsActive);
                    else if (status == "inactive")
                        filteredBreedGroups = filteredBreedGroups.Where(c => !c.IsActive);
                }

                var filteredList = filteredBreedGroups.ToList();
                vm.TotalCount = filteredList.Count;
                vm.BreedGroupList = filteredList.OrderBy(c => c.Name)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load breed groups. Please try again.";
            }

            return View(vm);
        }

        // POST /BreedGroups/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBreedGroupRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            
            try
            {
                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
                var resp = await _apiClient.PostAsync<ApiResponse<BreedGroupDto>>("api/BreedGroup/CreateBreedGroup", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /BreedGroups/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateBreedGroupRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<BreedGroupDto>>(
                    $"api/BreedGroup/breedgroup/{req.Id}", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /BreedGroups/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/BreedGroup/breedgroup/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET /BreedGroups/GetBreedsByGroup/{breedGroupId}
        [HttpGet]
        public async Task<IActionResult> GetBreedsByGroup(int breedGroupId)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<BreedDto>>>($"api/Breed/GetBreedsByGroup?BreedGroupId={breedGroupId}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data, message = resp?.Message });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /BreedGroups/CreateBreed
        [HttpPost]
        public async Task<IActionResult> CreateBreed([FromBody] CreateBreedRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            
            try
            {
                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
                var resp = await _apiClient.PostAsync<ApiResponse<BreedDto>>("api/Breed/CreateBreed", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // PUT /BreedGroups/UpdateBreed/{id}
        [HttpPut]
        public async Task<IActionResult> UpdateBreed(int id, [FromBody] CreateBreedRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            
            try
            {
                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
                var resp = await _apiClient.PutAsync<ApiResponse<BreedDto>>($"api/Breed/breed/{id}", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // DELETE /BreedGroups/DeleteBreed/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteBreed(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/Breed/breed/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}