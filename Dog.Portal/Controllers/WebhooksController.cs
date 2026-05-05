using Dog.Application.Features.Webhooks.DTOs;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class WebhooksController : Controller
    {
        private readonly IApiClient _apiClient;

        public WebhooksController(IApiClient apiClient)
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
            
            var vm = new WebhooksListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            // Pass filter values to ViewBag for form state
            ViewBag.StatusFilter = status;

            try
            {
                // Get webhooks
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Webhooks/company/{Convert.ToInt32(userSession.CompanyId)}");
                var all = new List<WebhookDto>();
                
                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                    all = System.Text.Json.JsonSerializer.Deserialize<List<WebhookDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<WebhookDto>();
                }

                // Get available events - the API returns { events: [...] } directly (not wrapped in ApiResponse)
                try
                {
                    var eventsResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Webhooks/events");
                    string[] parsedEvents = null;

                    if (eventsResp?.Success == true && eventsResp.Data != null)
                    {
                        var eventsJson = System.Text.Json.JsonSerializer.Serialize(eventsResp.Data);
                        var eventsDoc = System.Text.Json.JsonDocument.Parse(eventsJson);
                        if (eventsDoc.RootElement.TryGetProperty("events", out var eventsEl) && eventsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            parsedEvents = eventsEl.EnumerateArray()
                                .Select(e => e.GetString() ?? "")
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToArray();
                        }
                    }

                    // Fallback: use known events if API call failed or returned empty
                    ViewBag.AvailableEvents = (parsedEvents != null && parsedEvents.Length > 0)
                        ? parsedEvents
                        : new string[]
                        {
                            "shipment.created", "shipment.delivered", "shipment.cancelled",
                            "invoice.created", "invoice.paid",
                            "customer.created", "user.created", "company.created"
                        };
                }
                catch
                {
                    // Always provide the known events as fallback
                    ViewBag.AvailableEvents = new string[]
                    {
                        "shipment.created", "shipment.delivered", "shipment.cancelled",
                        "invoice.created", "invoice.paid",
                        "customer.created", "user.created", "company.created"
                    };
                }

                // Apply filters
                var filteredWebhooks = all.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredWebhooks = filteredWebhooks.Where(w =>
                        (w.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (w.Url?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (w.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "active")
                        filteredWebhooks = filteredWebhooks.Where(w => w.IsActive);
                    else if (status == "inactive")
                        filteredWebhooks = filteredWebhooks.Where(w => !w.IsActive);
                }

                var filteredList = filteredWebhooks.ToList();
                vm.TotalCount = filteredList.Count;
                vm.WebhookList = filteredList.OrderBy(w => w.Name)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Unable to load webhooks. Error: {ex.Message}";
                ViewBag.AvailableEvents = new string[]
                {
                    "shipment.created", "shipment.delivered", "shipment.cancelled",
                    "invoice.created", "invoice.paid",
                    "customer.created", "user.created", "company.created"
                };
                Console.WriteLine($"Webhooks Error: {ex}");
            }

            return View(vm);
        }

        public async Task<IActionResult> Logs(int? webhookId, string? search, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            
            var vm = new WebhookLogsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize, WebhookId = webhookId };

            try
            {
                var queryParams = new List<string>();
                if (webhookId.HasValue) queryParams.Add($"webhookId={webhookId.Value}");
                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                var queryString = string.Join("&", queryParams);
                var resp = await _apiClient.GetAsync<ApiResponse<Models.WebhookLogPageDto>>($"api/Webhooks/logs?{queryString}");
                
                if (resp?.Success == true && resp.Data != null)
                {
                    vm.TotalCount = resp.Data.Total;
                    vm.LogList = resp.Data.Data ?? [];

                    // Apply search filter on client side if needed
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        vm.LogList = vm.LogList.Where(log =>
                            (log.WebhookName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (log.Event?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (log.ErrorMessage?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToList();
                        vm.TotalCount = vm.LogList.Count;
                    }
                }
            }
            catch
            {
                ViewBag.FetchError = "Unable to load webhook logs. Please try again.";
            }

            return View(vm);
        }

        // POST /Webhooks/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWebhookRequest req)
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
                var resp = await _apiClient.PostAsync<ApiResponse<WebhookDto>>("api/Webhooks", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Webhooks/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateWebhookRequest req)
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
                var resp = await _apiClient.PutAsync<ApiResponse<WebhookDto>>($"api/Webhooks/{req.Id}", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Webhooks/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/Webhooks/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Webhooks/Toggle/{id}
        [HttpPost]
        public async Task<IActionResult> Toggle(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>($"api/Webhooks/{id}/toggle", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Status updated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Webhooks/RetryLog/{logId}
        [HttpPost]
        public async Task<IActionResult> RetryLog(int logId)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>($"api/Webhooks/logs/{logId}/retry", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Retry initiated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}