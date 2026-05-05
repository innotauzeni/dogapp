using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Dog.Application.Features.ExchangeRates.DTOs;


namespace Dog.Portal.Controllers
{
    public class ExchangeRatesController : Controller
    {
        private readonly IApiClient _apiClient;

        public ExchangeRatesController(IApiClient apiClient)
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
            
            var vm = new ExchangeRatesListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            // Pass filter values to ViewBag for form state
            ViewBag.StatusFilter = status;

            try
            {
                // Get exchange rates
                var resp = await _apiClient.GetAsync<ApiResponse<List<ExchangeRateDto>>>($"api/ExchangeRate/exchange-rates/{Convert.ToInt32(userSession.CompanyId)}");
                var all = resp?.Data ?? [];

                // Get currencies for dropdowns
                var currenciesResp = await _apiClient.GetAsync<ApiResponse<List<Dog.Application.Features.Currencies.DTOs.CurrencyDto>>>("api/currency/GetPubicCurrencies");
                ViewBag.Currencies = currenciesResp?.Data ?? [];

                // Apply filters
                var filteredExchangeRates = all.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredExchangeRates = filteredExchangeRates.Where(c =>
                        (c.PrimaryCurrencyCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.SecondaryCurrencyCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "active")
                        filteredExchangeRates = filteredExchangeRates.Where(c => c.IsActive);
                    else if (status == "inactive")
                        filteredExchangeRates = filteredExchangeRates.Where(c => !c.IsActive);
                }

                var filteredList = filteredExchangeRates.ToList();
                vm.TotalCount = filteredList.Count;
                vm.ExchangeRateList = filteredList.OrderBy(c => c.PrimaryCurrencyCode)
                                    .ThenBy(c => c.SecondaryCurrencyCode)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load exchange rates. Please try again.";
            }

            return View(vm);
        }

        // POST /ExchangeRates/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExchangeRateRequest req)
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
                var resp = await _apiClient.PostAsync<ApiResponse<ExchangeRateDto>>("api/ExchangeRate/exchange-rates", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /ExchangeRates/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateExchangeRateRequest req)
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
                var resp = await _apiClient.PutAsync<ApiResponse<ExchangeRateDto>>(
                    $"api/ExchangeRate/exchange-rates/{req.Id}", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /ExchangeRates/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/ExchangeRate/exchange-rates/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}