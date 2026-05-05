using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    // Public signup flow — lets a prospect register their own company.
    // All endpoints are anonymous; they proxy to the API's [AllowAnonymous] /api/Onboard routes.
    public class OnboardController : Controller
    {
        private readonly IApiClient _apiClient;

        public OnboardController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // GET /Onboard  — signup landing page
        [HttpGet]
        public IActionResult Index() => View();

        // POST /Onboard/Register — submit the signup
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] OnboardSubmitRequest req)
        {
            try
            {
                var payload = new
                {
                    company = new
                    {
                        companyName       = req.Company.CompanyName,
                        companyCode       = req.Company.CompanyCode,
                        emailAddress      = req.Company.EmailAddress,
                        phoneNumber       = req.Company.PhoneNumber,
                        website           = req.Company.Website,
                        address           = req.Company.Address,
                        countryId         = req.Company.CountryId,
                        provinceId        = req.Company.ProvinceId,
                        cityId            = req.Company.CityId,
                        defaultCurrencyId = req.Company.DefaultCurrencyId
                    },
                    admin = new
                    {
                        firstName = req.Admin.FirstName,
                        lastName  = req.Admin.LastName,
                        email     = req.Admin.Email,
                        password  = req.Admin.Password
                    }
                };

                var resp = await _apiClient.PostAsync<ApiResponse<OnboardResultDto>>("api/Onboard", payload);
                return Json(new
                {
                    success = resp?.Success ?? false,
                    message = resp?.Message ?? "Registration complete.",
                    data    = resp?.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Public lookups (used by the signup form's dropdowns) ──────────
        [HttpGet]
        public async Task<IActionResult> Countries()
        {
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<CountryOption>>>("api/Onboard/countries");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data ?? new() });
            }
            catch { return Json(new { success = false, data = new List<CountryOption>() }); }
        }

        [HttpGet]
        public async Task<IActionResult> Provinces(int countryId)
        {
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<ProvinceOption>>>($"api/Onboard/provinces/{countryId}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data ?? new() });
            }
            catch { return Json(new { success = false, data = new List<ProvinceOption>() }); }
        }

        [HttpGet]
        public async Task<IActionResult> Cities(int provinceId)
        {
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<CityOption>>>($"api/Onboard/cities/{provinceId}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data ?? new() });
            }
            catch { return Json(new { success = false, data = new List<CityOption>() }); }
        }

        [HttpGet]
        public async Task<IActionResult> Currencies()
        {
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<CurrencyOption>>>("api/Onboard/currencies");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data ?? new() });
            }
            catch { return Json(new { success = false, data = new List<CurrencyOption>() }); }
        }
    }
}
