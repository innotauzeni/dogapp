using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Dog.Application.Features.Customers.DTOs;
using Dog.Application.Features.Deployments.DTOs;

namespace Dog.Portal.Controllers
{
    public class DeploymentsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public DeploymentsController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new DeploymentListViewModel
            {
                SearchTerm = search,
                Page = page,
                PageSize = pageSize
            };

            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Deployment?CompanyId={companyId}");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = JsonSerializer.Serialize(resp.Data);
                    var deployments = JsonSerializer.Deserialize<List<DeploymentDto>>(jsonString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (deployments != null)
                    {
                        // Search filter
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            deployments = deployments.Where(d =>
                                (d.Location?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (d.CompanyDeployedTo?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (d.SecurityGuardAssigned?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (d.Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                            ).ToList();
                        }

                        // Status filter
                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            var isActive = status.ToLower() == "active";
                            deployments = deployments.Where(d => d.IsActive == isActive).ToList();
                        }

                        vm.TotalCount = deployments.Count;
                        vm.DeploymentList = deployments.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    }
                }

                // Load dogs for dropdown
                var dogsResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal?CompanyId={companyId}&pageSize=1000");
                if (dogsResp?.Success == true && dogsResp.Data != null)
                {
                    var dogsJson = JsonSerializer.Serialize(dogsResp.Data);
                    var dogsPage = JsonSerializer.Deserialize<AnimalPageResult>(dogsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    vm.Dogs = dogsPage?.Data ?? new List<DogDropdownItem>();
                }

                // Load customers for dropdown
                var custResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Customer?CompanyId={companyId}");
                if (custResp?.Success == true && custResp.Data != null)
                {
                    var custJson = JsonSerializer.Serialize(custResp.Data);
                    vm.Customers = JsonSerializer.Deserialize<List<CustomerDto>>(custJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CustomerDto>();
                }

                ViewBag.StatusFilter = status;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load deployments: {ex.Message}";
                Console.WriteLine($"Deployments Error: {ex}");
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDeploymentRequest req)
        {
            if (!await IsAuthenticated())
                return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateDeploymentDto
                {
                    CompanyId = companyId,
                    DogId = req.DogId,
                    Location = req.Location ?? string.Empty,
                    CompanyDeployedTo = req.CompanyDeployedTo ?? string.Empty,
                    SecurityGuardAssigned = req.SecurityGuardAssigned ?? string.Empty,
                    StartDate = req.StartDate,
                    EndDate = req.EndDate,
                    Notes = req.Notes,
                    IsActive = req.IsActive,
                    CustomerId = req.CustomerId,
                    CustomerSiteId = req.CustomerSiteId
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Deployment/CreateDeployment", dto);
                if (resp?.Success == true)
                    return Json(new { success = true, message = "Deployment created successfully!" });
                return Json(new { success = false, message = resp?.Message ?? "Failed to create deployment" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateDeploymentRequest req)
        {
            if (!await IsAuthenticated())
                return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateDeploymentDto
                {
                    CompanyId = companyId,
                    DogId = req.DogId,
                    Location = req.Location ?? string.Empty,
                    CompanyDeployedTo = req.CompanyDeployedTo ?? string.Empty,
                    SecurityGuardAssigned = req.SecurityGuardAssigned ?? string.Empty,
                    StartDate = req.StartDate,
                    EndDate = req.EndDate,
                    Notes = req.Notes,
                    IsActive = req.IsActive,
                    CustomerId = req.CustomerId,
                    CustomerSiteId = req.CustomerSiteId
                };
                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/Deployment/Deployment/{req.Id}", dto);
                if (resp?.Success == true)
                    return Json(new { success = true, message = "Deployment updated successfully!" });
                return Json(new { success = false, message = resp?.Message ?? "Failed to update deployment" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthenticated())
                return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/Deployment/Deployment/{id}");
                if (resp?.Success == true)
                    return Json(new { success = true, message = "Deployment deleted successfully!" });
                return Json(new { success = false, message = resp?.Message ?? "Failed to delete deployment" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST /Deployments/Return/{id} — recalls the dog back to its branch/head office.
        [HttpPost]
        public async Task<IActionResult> Return(int id)
        {
            if (!await IsAuthenticated())
                return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var raw = await _apiClient.PostRawAsync($"api/Deployment/Deployment/{id}/return", new { });
                ApiResponse<bool>? parsed = null;
                try { parsed = JsonSerializer.Deserialize<ApiResponse<bool>>(raw.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                catch { /* body wasn't the expected envelope shape — fall through */ }

                if (raw.IsSuccessStatusCode && parsed?.Success == true)
                    return Json(new { success = true, message = parsed.Message ?? "Dog returned to branch." });
                if (raw.StatusCode == 401 || raw.StatusCode == 403)
                    return Json(new { success = false, message = "You don't have permission to do this." });
                return Json(new { success = false, message = parsed?.Message ?? $"Request failed ({raw.StatusCode})." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
