using Dog.Application.Features.CustomerSites.DTOs;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class CustomerSitesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public CustomerSitesController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        [HttpGet]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/CustomerSite?customerId={customerId}");
                var sites = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<CustomerSiteDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : new List<CustomerSiteDto>();
                return Json(new { success = true, data = sites });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerSiteRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateCustomerSiteDto
                {
                    CustomerId = req.CustomerId,
                    CompanyId = companyId,
                    SiteName = req.SiteName,
                    Address = req.Address,
                    Latitude = req.Latitude,
                    Longitude = req.Longitude,
                    ContactPerson = req.ContactPerson,
                    ContactPhone = req.ContactPhone,
                    IsActive = true,
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/CustomerSite", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to add site" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] CustomerSiteRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateCustomerSiteDto
                {
                    CustomerId = req.CustomerId,
                    CompanyId = companyId,
                    SiteName = req.SiteName,
                    Address = req.Address,
                    Latitude = req.Latitude,
                    Longitude = req.Longitude,
                    ContactPerson = req.ContactPerson,
                    ContactPhone = req.ContactPhone,
                    IsActive = true,
                };
                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/CustomerSite/{req.Id}", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to update site" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/CustomerSite/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to delete site" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
