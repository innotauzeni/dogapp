using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class AuditsController : Controller
    {
        private readonly IApiClient _apiClient;

        public AuditsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        public async Task<IActionResult> Index(string? entity, string? actionType, string? username, string? search, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            
            var vm = new AuditsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            // Pass filter values to ViewBag for form state
            ViewBag.EntityFilter = entity;
            ViewBag.ActionFilter = actionType;
            ViewBag.UsernameFilter = username;

            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(entity)) queryParams.Add($"entity={entity}");
                if (!string.IsNullOrEmpty(actionType)) queryParams.Add($"action={actionType}");
                if (!string.IsNullOrEmpty(username)) queryParams.Add($"username={username}");
                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                var queryString = string.Join("&", queryParams);
                Console.WriteLine($"Calling API: api/Audit?{queryString}");
                
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Audit?{queryString}");
                Console.WriteLine($"API Response - Success: {resp?.Success}, Data is null: {resp?.Data == null}, Message: {resp?.Message}");
                
                if (resp?.Success == true && resp.Data != null)
                {
                    try
                    {
                        // The API returns an object, we need to deserialize it to AuditPageDto
                        var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                        Console.WriteLine($"Audit API Response JSON: {jsonString}");
                        
                        var auditPage = System.Text.Json.JsonSerializer.Deserialize<AuditPageDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        Console.WriteLine($"Deserialized - Total: {auditPage?.Total}, Data Count: {auditPage?.Data?.Count}");
                        
                        if (auditPage != null)
                        {
                            vm.TotalCount = auditPage.Total;
                            vm.AuditList = auditPage.Data ?? new List<AuditItemDto>();
                            ViewBag.Entities = auditPage.Entities ?? new List<string>();
                            
                            Console.WriteLine($"ViewModel - TotalCount: {vm.TotalCount}, AuditList Count: {vm.AuditList.Count}");

                            if (!string.IsNullOrWhiteSpace(search))
                            {
                                vm.AuditList = vm.AuditList.Where(audit =>
                                    (audit.EntityName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (audit.ActionType?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (audit.Username?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (audit.EntityId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                                    .ToList();
                                vm.TotalCount = vm.AuditList.Count;
                            }
                        }
                        else
                        {
                            ViewBag.FetchError = "Failed to deserialize audit data.";
                            Console.WriteLine("Deserialization returned null");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        ViewBag.FetchError = $"JSON deserialization error: {jsonEx.Message}";
                        Console.WriteLine($"Audit JSON Error: {jsonEx}");
                    }
                }
                else
                {
                    ViewBag.FetchError = $"API call failed. Success: {resp?.Success}, Data: {resp?.Data != null}, Message: {resp?.Message}";
                    Console.WriteLine($"API call failed - Success: {resp?.Success}, Data: {resp?.Data != null}, Message: {resp?.Message}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Unable to load audit logs. Error: {ex.Message}";
                ViewBag.Entities = new List<string>();
                // Log the full exception for debugging
                Console.WriteLine($"Audits Error: {ex}");
            }

            return View(vm);
        }
    }
}