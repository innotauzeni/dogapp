using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;
        private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

        public ExpensesController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        public async Task<IActionResult> Index(string? search, string? category, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new ExpenseListViewModel { SearchTerm = search, CategoryFilter = category, Page = page, PageSize = pageSize };

            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Expense?CompanyId={companyId}");
                if (resp?.Success == true && resp.Data != null)
                {
                    var list = JsonSerializer.Deserialize<List<ExpenseItemViewModel>>(JsonSerializer.Serialize(resp.Data), Opts) ?? new();

                    vm.TotalAmount = list.Sum(e => e.Amount);
                    var now = DateTime.UtcNow;
                    vm.ThisMonthAmount = list.Where(e => e.ExpenseDate.Year == now.Year && e.ExpenseDate.Month == now.Month).Sum(e => e.Amount);

                    if (!string.IsNullOrWhiteSpace(search))
                        list = list.Where(e =>
                            (e.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                            (e.Vendor?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                            (e.DogName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)).ToList();

                    if (!string.IsNullOrWhiteSpace(category))
                        list = list.Where(e => e.Category == category).ToList();

                    vm.TotalCount = list.Count;
                    vm.ExpenseList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }

                var dogsResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal/GetPets?CompanyId={companyId}");
                if (dogsResp?.Success == true && dogsResp.Data != null)
                    vm.Dogs = JsonSerializer.Deserialize<List<AnimalListDto>>(JsonSerializer.Serialize(dogsResp.Data), Opts) ?? new();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load expenses: {ex.Message}";
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExpenseRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new
                {
                    companyId,
                    category = req.Category,
                    description = req.Description,
                    amount = req.Amount,
                    expenseDate = req.ExpenseDate,
                    dogId = req.DogId,
                    vendor = req.Vendor,
                    paymentMethod = req.PaymentMethod,
                    notes = req.Notes,
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Expense", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to record expense" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateExpenseRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new
                {
                    id = req.Id,
                    companyId,
                    category = req.Category,
                    description = req.Description,
                    amount = req.Amount,
                    expenseDate = req.ExpenseDate,
                    dogId = req.DogId,
                    vendor = req.Vendor,
                    paymentMethod = req.PaymentMethod,
                    notes = req.Notes,
                };
                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/Expense/{req.Id}", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to update expense" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/Expense/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to delete expense" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}
