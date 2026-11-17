using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;
        private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

        public NotificationsController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        public async Task<IActionResult> Index(string? type, int page = 1, int pageSize = 20)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new NotificationLogListViewModel { TypeFilter = type, Page = page, PageSize = pageSize };

            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Notification?companyId={companyId}");
                if (resp?.Success == true && resp.Data != null)
                {
                    var list = JsonSerializer.Deserialize<List<NotificationLogItemViewModel>>(JsonSerializer.Serialize(resp.Data), Opts) ?? new();
                    if (!string.IsNullOrWhiteSpace(type))
                        list = list.Where(n => n.NotificationType == type).ToList();

                    vm.TotalCount = list.Count;
                    vm.LogList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load notifications: {ex.Message}";
            }

            return View(vm);
        }
    }
}
