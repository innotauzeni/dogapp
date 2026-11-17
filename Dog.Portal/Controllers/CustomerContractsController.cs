using Dog.Application.Features.CustomerContracts.DTOs;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class CustomerContractsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public CustomerContractsController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
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
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/CustomerContract?customerId={customerId}");
                var contracts = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<CustomerContractDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : new List<CustomerContractDto>();
                return Json(new { success = true, data = contracts });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerContractRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateCustomerContractDto
                {
                    CustomerId = req.CustomerId,
                    CompanyId = companyId,
                    ContractDate = req.ContractDate,
                    ContractDetails = req.ContractDetails,
                    IsActive = true,
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/CustomerContract", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to add contract" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] CustomerContractRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateCustomerContractDto
                {
                    CustomerId = req.CustomerId,
                    CompanyId = companyId,
                    ContractDate = req.ContractDate,
                    ContractDetails = req.ContractDetails,
                    IsActive = true,
                };
                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/CustomerContract/{req.Id}", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to update contract" });
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
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/CustomerContract/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to delete contract" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
