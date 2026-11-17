using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    /// <summary>
    /// Public landing page controller. Loads dynamic content from DB (ka_appsettings key "Landing:Json")
    /// or defaults. No authentication required.
    /// </summary>
    [AllowAnonymous]
    public class LandingController : Controller
    {
        private readonly IApiClient _apiClient;
        private static readonly JsonSerializerOptions _camel =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = false };

        public LandingController(IApiClient apiClient) => _apiClient = apiClient;

        // GET / (public landing page)
        public async Task<IActionResult> Index()
        {
            var model = new LandingViewModel();
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<string>>("api/Landing");
                if (!string.IsNullOrWhiteSpace(resp?.Data))
                {
                    var custom = JsonSerializer.Deserialize<LandingViewModel>(resp.Data, _camel);
                    if (custom != null) model = custom;
                }
            }
            catch { /* fall back to defaults */ }

            return View(model);
        }
    }
}
