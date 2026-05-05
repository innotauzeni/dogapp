using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using PortalModels = Dog.Portal.Models;

namespace Dog.Portal.Controllers
{
    public class SystemLogsController : Controller
    {
        private readonly IApiClient _apiClient;

        public SystemLogsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        public async Task<IActionResult> Index(string? severity, bool? resolved, string? search, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            
            var vm = new SystemLogsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            // Pass filter values to ViewBag for form state
            ViewBag.SeverityFilter = severity;
            ViewBag.ResolvedFilter = resolved;

            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(severity)) queryParams.Add($"severity={severity}");
                if (resolved.HasValue) queryParams.Add($"resolved={resolved.Value}");
                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                var queryString = string.Join("&", queryParams);
                Console.WriteLine($"Calling SystemLogs API: api/SystemLogs/errors?{queryString}");
                
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/SystemLogs/errors?{queryString}");
                Console.WriteLine($"SystemLogs API Response - Success: {resp?.Success}, Data is null: {resp?.Data == null}, Message: {resp?.Message}");
                
                if (resp?.Success == true && resp.Data != null)
                {
                    try
                    {
                        // The API returns an object, we need to deserialize it to ErrorLogPageDto
                        var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                        Console.WriteLine($"SystemLogs API Response JSON: {jsonString}");
                        
                        var errorLogPage = System.Text.Json.JsonSerializer.Deserialize<PortalModels.ErrorLogPageDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        Console.WriteLine($"SystemLogs Deserialized - Total: {errorLogPage?.Total}, Data Count: {errorLogPage?.Data?.Count}");
                        
                        if (errorLogPage != null)
                        {
                            vm.TotalCount = errorLogPage.Total;
                            vm.ErrorLogList = errorLogPage.Data ?? new List<PortalModels.ErrorLogItemDto>();
                            
                            Console.WriteLine($"SystemLogs ViewModel - TotalCount: {vm.TotalCount}, ErrorLogList Count: {vm.ErrorLogList.Count}");

                            // Apply search filter on client side if needed
                            if (!string.IsNullOrWhiteSpace(search))
                            {
                                vm.ErrorLogList = vm.ErrorLogList.Where(log =>
                                    (log.Message?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (log.Source?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (log.ExceptionType?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                                    .ToList();
                                vm.TotalCount = vm.ErrorLogList.Count;
                            }
                        }
                        else
                        {
                            ViewBag.FetchError = "Failed to deserialize system logs data.";
                            Console.WriteLine("SystemLogs deserialization returned null");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        ViewBag.FetchError = $"JSON deserialization error: {jsonEx.Message}";
                        Console.WriteLine($"SystemLogs JSON Error: {jsonEx}");
                    }
                }
                else
                {
                    ViewBag.FetchError = $"API call failed. Success: {resp?.Success}, Data: {resp?.Data != null}, Message: {resp?.Message}";
                    Console.WriteLine($"SystemLogs API call failed - Success: {resp?.Success}, Data: {resp?.Data != null}, Message: {resp?.Message}");
                }
            }
            catch
            {
                ViewBag.FetchError = "Unable to load system logs. Please try again.";
            }

            return View(vm);
        }

        // POST /SystemLogs/Resolve/{id}
        [HttpPost]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveErrorRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null || string.IsNullOrWhiteSpace(req.Resolution)) 
                return Json(new { success = false, message = "Resolution is required." });
            
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>($"api/SystemLogs/errors/{id}/resolve", req.Resolution);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Resolved." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemLogs/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/SystemLogs/errors/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}