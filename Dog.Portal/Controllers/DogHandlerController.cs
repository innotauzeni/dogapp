using Dog.Application.Contracts;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class DogHandlerController : Controller
    {
        private readonly IApiClient _apiClient;

        public DogHandlerController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        public async Task<IActionResult> Index(string? search, string? status, string? gender, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Auth");

            var vm = await BuildDogsListViewModel(search, status, gender, page, pageSize);
            return View("~/Views/Dogs/Handlers.cshtml", vm);
        }

        // GET /DogHandler/Activity — the handler's own care-activity feed with archive navigation.
        public async Task<IActionResult> Activity()
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Auth");

            var vm = new HandlerActivityViewModel();
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<HandlerActivityDto>>>("api/animal/GetHandlerActivities");
                vm.Activities = resp?.Data ?? new List<HandlerActivityDto>();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load your activity. Please try again.";
            }

            return View("~/Views/Dogs/HandlerActivity.cshtml", vm);
        }

        // POST /DogHandler/UpdateCareRecord — edit a care record the handler recorded.
        [HttpPost]
        public async Task<IActionResult> UpdateCareRecord([FromBody] UpdateCareRecordRequest request)
        {
            if (!IsAuthenticated())
                return Json(new { success = false, message = "Session expired." });

            try
            {
                var response = await _apiClient.PutAsync<ApiResponse<bool>>(
                    $"api/animal/UpdateCareRecord/{request.Id}", request);
                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Care record updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<DogsListViewModel> BuildDogsListViewModel(string? search, string? status, string? gender, int page, int pageSize)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

            var vm = new DogsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };
            ViewBag.StatusFilter = status;
            ViewBag.GenderFilter = gender;

            try
            {
                var route = $"api/animal/GetAssignedDogs?handlerUserId={Uri.EscapeDataString(userSession!.Id)}";
                var resp = await _apiClient.GetAsync<ApiResponse<List<AnimalListDto>>>(route);
                var assignedDogs = resp?.Data ?? new List<AnimalListDto>();

                var filteredDogs = assignedDogs;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredDogs = filteredDogs.Where(c =>
                        (c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Breed?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Color?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.MicrochipNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "active")
                        filteredDogs = filteredDogs.Where(c => c.IsActive).ToList();
                    else if (status == "inactive")
                        filteredDogs = filteredDogs.Where(c => !c.IsActive).ToList();
                }

                if (!string.IsNullOrWhiteSpace(gender))
                {
                    filteredDogs = filteredDogs.Where(c =>
                        string.Equals(c.Gender, gender, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                vm.TotalCount = filteredDogs.Count;
                vm.Doglist = filteredDogs.OrderBy(c => c.Name)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load dogs. Please try again.";
            }

            return vm;
        }
    }
}
