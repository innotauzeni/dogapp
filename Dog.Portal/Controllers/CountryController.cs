using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    public class CountryController : Controller
    {
        private readonly IApiClient _apiClient;

        public CountryController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        // GET /Country
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new CountriesListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<CountryDto>>>("api/country/GetCountries");
                var all = resp?.Data ?? [];

                vm.TotalProvinces = all.Sum(c => c.ProvinceCount);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    all = all.Where(c =>
                        (c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Code?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                vm.TotalCount = all.Count;
                vm.Countries  = all.OrderBy(c => c.Name)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load countries. Please try again.";
            }

            return View(vm);
        }

        // POST /Country/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCountryRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<CountryDto>>("api/country/CreateCountry", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Country/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCountryRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<CountryDto>>(
                    $"api/country/UpdateCountry/{req.Id}",
                    new { code = req.Code, name = req.Name });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Country/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/country/DeleteCountry/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Country/ImportFromWeb — seeds countries & provinces from REST Countries API
        [HttpPost]
        public async Task<IActionResult> ImportFromWeb()
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<ImportSummaryDto>>("api/country/ImportCountriesFromWeb", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Import complete.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Country/ImportAllCities — imports cities for all countries in DB
        [HttpPost]
        public async Task<IActionResult> ImportAllCities()
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<ImportSummaryDto>>("api/country/ImportAllCities", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Import complete.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Country/ImportCities/{iso2} — imports cities for a single country
        [HttpPost]
        public async Task<IActionResult> ImportCities(string iso2)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<ImportSummaryDto>>($"api/country/ImportCitiesByCountry/{iso2}", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Import complete.", data = resp?.Data });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET /Country/Provinces/{countryId} — AJAX list for drilldown
        [HttpGet]
        public async Task<IActionResult> Provinces(int countryId)
        {
            if (!IsAuthenticated()) return Json(new { success = false });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<ProvinceDto>>>($"api/country/GetProvincesByCountry/{countryId}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data ?? new() });
            }
            catch { return Json(new { success = false, data = new List<ProvinceDto>() }); }
        }

        // GET /Country/Cities/{provinceId} — AJAX list for drilldown
        [HttpGet]
        public async Task<IActionResult> Cities(int provinceId)
        {
            if (!IsAuthenticated()) return Json(new { success = false });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<CityDto>>>($"api/country/GetCitiesByProvince/{provinceId}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data ?? new() });
            }
            catch { return Json(new { success = false, data = new List<CityDto>() }); }
        }
    }
}
