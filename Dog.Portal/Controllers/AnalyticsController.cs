using Dog.Application.Features.Analytics.DTOs;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public AnalyticsController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index(int months = 6)
        {
            if (!await _currentUserService.IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new AnalyticsSummaryDto();
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Analytics/summary?companyId={companyId}&months={months}");
                if (resp?.Success == true && resp.Data != null)
                {
                    vm = JsonSerializer.Deserialize<AnalyticsSummaryDto>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? vm;
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load analytics: {ex.Message}";
            }

            ViewBag.Months = months;
            return View(vm);
        }
    }
}
