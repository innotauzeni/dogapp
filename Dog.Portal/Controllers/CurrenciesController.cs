using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    public class CurrenciesController : Controller
    {

        private readonly IApiClient _apiClient;

        public CurrenciesController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            var vm = new CurrenciesListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<CurrencyDto>>>("api/currency/currencies");
                var all = resp?.Data ?? [];

                if (!string.IsNullOrWhiteSpace(search))
                {
                    all = all.Where(c =>
                        (c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Code?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                         (c.Symbol?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                vm.TotalCount = all.Count;
                vm.Currencies = all.OrderBy(c => c.Name)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load Currencies. Please try again.";
            }

            return View(vm);
        }

        // POST /Country/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<CurrencyDto>>("api/currency/CreateCurrency", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        // POST /Country/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCurrencyRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<CurrencyDto>>(
                    $"api/currency/UpdateCurrency/{req.Id}",
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
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/currency/DeleteCurrency/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


    }
}
