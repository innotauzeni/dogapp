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

        /// Turns a raw (never-swallowed) API response into an honest success/message pair —
        /// distinguishes "you don't have permission" (401/403) from "the server errored" (5xx)
        /// instead of collapsing every failure into the same generic text.
        private static object BuildResult(ApiRawResult raw, string successFallback)
        {
            ApiResponse<bool>? parsed = null;
            try { parsed = JsonSerializer.Deserialize<ApiResponse<bool>>(raw.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { /* body wasn't the expected envelope shape — fall through */ }

            if (raw.IsSuccessStatusCode && parsed?.Success == true)
                return new { success = true, message = parsed.Message ?? successFallback };

            if (raw.StatusCode == 401 || raw.StatusCode == 403)
                return new { success = false, message = "You don't have permission to do this. Your role is missing the required permission — check Roles & Permissions." };

            var bodySnippet = raw.Body.Length > 200 ? raw.Body[..200] + "…" : raw.Body;
            return new { success = false, message = parsed?.Message ?? $"Request failed ({raw.StatusCode}): {bodySnippet}" };
        }

        public async Task<IActionResult> Index(string? severity, bool? resolved, string? search, bool deleted = false, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new SystemLogsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize, ShowDeleted = deleted };

            // Pass filter values to ViewBag for form state
            ViewBag.SeverityFilter = severity;
            ViewBag.ResolvedFilter = resolved;
            ViewBag.ShowDeleted = deleted;

            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(severity)) queryParams.Add($"severity={severity}");
                if (resolved.HasValue) queryParams.Add($"resolved={resolved.Value}");
                if (deleted) queryParams.Add("deleted=true");
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
                            vm.TotalDeleted = errorLogPage.TotalDeleted;
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
                var raw = await _apiClient.PostRawAsync($"api/SystemLogs/errors/{id}/resolve", req.Resolution);
                return Json(BuildResult(raw, "Marked as resolved."));
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
                var raw = await _apiClient.DeleteRawAsync($"api/SystemLogs/errors/{id}");
                return Json(BuildResult(raw, "Deleted."));
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemLogs/ResolveAll — marks every currently-unresolved error (matching
        // the active severity filter, if any) as resolved.
        [HttpPost]
        public async Task<IActionResult> ResolveAll(string? severity)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var qs = string.IsNullOrWhiteSpace(severity) ? "" : $"?severity={severity}";
                var raw = await _apiClient.PostRawAsync($"api/SystemLogs/errors/resolve-all{qs}", new { });
                return Json(BuildResult(raw, "All resolved!"));
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemLogs/DeleteAll — deletes every log matching the active filters
        // (or literally everything if no filter is applied).
        [HttpPost]
        public async Task<IActionResult> DeleteAll(string? severity, bool? resolved)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var qs = new List<string>();
                if (!string.IsNullOrWhiteSpace(severity)) qs.Add($"severity={severity}");
                if (resolved.HasValue) qs.Add($"resolved={resolved.Value}");
                var url = "api/SystemLogs/errors/all" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
                var raw = await _apiClient.DeleteRawAsync(url);
                return Json(BuildResult(raw, "All deleted!"));
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemLogs/Restore/{id} — brings a soft-deleted log back out of the trash.
        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var raw = await _apiClient.PostRawAsync($"api/SystemLogs/errors/{id}/restore", new { });
                return Json(BuildResult(raw, "Restored."));
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemLogs/PermanentDelete/{id} — truly removes a trashed log, no undo.
        [HttpPost]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var raw = await _apiClient.DeleteRawAsync($"api/SystemLogs/errors/{id}/permanent");
                return Json(BuildResult(raw, "Permanently deleted."));
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /SystemLogs/EmptyTrash — permanently removes every soft-deleted log at once.
        [HttpPost]
        public async Task<IActionResult> EmptyTrash()
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var raw = await _apiClient.DeleteRawAsync("api/SystemLogs/errors/trash");
                return Json(BuildResult(raw, "Trash emptied."));
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}