using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class PaymentMethodsController : Controller
    {
        private readonly IApiClient _apiClient;

        public PaymentMethodsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 15)
        {
            // ✅ Get session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);


            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            var vm = new PaymentMethodsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            try
            {
                // 🔥 ROLE-BASED FILTERING
                List<PaymentMethodDto> filteredPaymentMethods;

                var resp = await _apiClient.GetAsync<ApiResponse<List<PaymentMethodDto>>>($"api/paymentmethod/GetPaymentMethods?CompanyId={Convert.ToInt32(userSession.CompanyId)}");
                var all = resp?.Data ?? [];

               

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredPaymentMethods = all.Where(c =>
                        (c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                vm.TotalCount = all.Count;
                vm.PaymentMethods = all.OrderBy(c => c.Name)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load Payment Method. Please try again.";
            }

            return View(vm);
        }

        // POST /Country/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentMethodRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                req.CompanyId = Convert.ToInt32(userSession.CompanyId);
                var resp = await _apiClient.PostAsync<ApiResponse<PaymentMethodDto>>("api/paymentmethod/CreatePaymentMethod", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        // POST /Country/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdatePaymentMethodRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PutAsync<ApiResponse<CurrencyDto>>(
                    $"api/paymentmethod/UpdatePaymentMethod/{req.Id}",
                    new { name = req.Name });
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
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/paymentmethod/DeletePaymentMethod/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


    }
}
