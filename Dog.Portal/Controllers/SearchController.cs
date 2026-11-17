using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    public class SearchController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public SearchController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        // GET /Search?query=... — used by the global search box in the admin layout.
        [HttpGet]
        public async Task<IActionResult> Index(string? query)
        {
            if (!await _currentUserService.IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            if (string.IsNullOrWhiteSpace(query)) return Json(new { success = true, data = new { dogs = Array.Empty<object>(), customers = Array.Empty<object>(), invoices = Array.Empty<object>() } });

            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Search?companyId={companyId}&query={Uri.EscapeDataString(query)}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data, message = resp?.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
