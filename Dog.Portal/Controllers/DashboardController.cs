using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public DashboardController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index()
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
                return RedirectToAction("Login", "Auth");

            var vm = new DashboardViewModel();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();

                // ── Dogs ──────────────────────────────────────────────
                try
                {
                    var dogsResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal/GetPets?CompanyId={companyId}");
                    if (dogsResp?.Success == true && dogsResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(dogsResp.Data);
                        var dogs = JsonSerializer.Deserialize<List<DashboardAnimalItem>>(json, opts) ?? new();
                        vm.TotalDogs = dogs.Count;
                        vm.ActiveDogs = dogs.Count(d => d.IsActive);
                        vm.MaleDogs = dogs.Count(d => string.Equals(d.Gender, "male", StringComparison.OrdinalIgnoreCase));
                        vm.FemaleDogs = dogs.Count(d => string.Equals(d.Gender, "female", StringComparison.OrdinalIgnoreCase));

                        vm.TopBreeds = dogs
                            .Where(d => !string.IsNullOrEmpty(d.Breed))
                            .GroupBy(d => d.Breed)
                            .OrderByDescending(g => g.Count())
                            .Take(5)
                            .Select(g => new BreedCount
                            {
                                Breed = g.Key,
                                Count = g.Count(),
                                Percentage = dogs.Count > 0 ? (int)Math.Round(g.Count() * 100.0 / dogs.Count) : 0
                            }).ToList();

                        vm.RecentDogs = dogs
                            .OrderByDescending(d => d.Id)
                            .Take(5)
                            .Select(d => new DashboardDogItem
                            {
                                Id = d.Id,
                                Name = d.Name,
                                Breed = d.Breed,
                                Gender = d.Gender,
                                IsActive = d.IsActive,
                                DateOfBirth = d.BirthDate
                            }).ToList();
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Dashboard dogs error: {ex.Message}"); }

                // ── Customers ─────────────────────────────────────────
                try
                {
                    var custResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Customer?CompanyId={companyId}");
                    if (custResp?.Success == true && custResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(custResp.Data);
                        var customers = JsonSerializer.Deserialize<List<DashboardCustomerItem>>(json, opts);
                        if (customers != null)
                        {
                            vm.TotalCustomers = customers.Count;
                            vm.RecentCustomers = customers.OrderByDescending(c => c.Id).Take(5).ToList();
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Dashboard customers error: {ex.Message}"); }

                // ── Litters ───────────────────────────────────────────
                try
                {
                    var littersResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal/GetLitters?CompanyId={companyId}");
                    if (littersResp?.Success == true && littersResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(littersResp.Data);
                        var litters = JsonSerializer.Deserialize<List<object>>(json, opts);
                        vm.TotalLitters = litters?.Count ?? 0;
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Dashboard litters error: {ex.Message}"); }

                // ── Mating Records ────────────────────────────────────
                try
                {
                    var matingResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal/GetMatingRecords?CompanyId={companyId}");
                    if (matingResp?.Success == true && matingResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(matingResp.Data);
                        var matings = JsonSerializer.Deserialize<List<object>>(json, opts);
                        vm.TotalMatingRecords = matings?.Count ?? 0;
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Dashboard mating error: {ex.Message}"); }

                // ── Deployments ───────────────────────────────────────
                try
                {
                    var depResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Deployment?CompanyId={companyId}");
                    if (depResp?.Success == true && depResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(depResp.Data);
                        var deps = JsonSerializer.Deserialize<List<DashboardDeploymentItem>>(json, opts);
                        if (deps != null)
                        {
                            vm.TotalDeployments = deps.Count;
                            vm.ActiveDeployments = deps.Count(d => d.IsActive);
                            vm.RecentDeployments = deps.OrderByDescending(d => d.Id).Take(5).ToList();
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Dashboard deployments error: {ex.Message}"); }

                // ── Banks ─────────────────────────────────────────────
                try
                {
                    var banksResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Bank?CompanyId={companyId}");
                    if (banksResp?.Success == true && banksResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(banksResp.Data);
                        var banks = JsonSerializer.Deserialize<List<object>>(json, opts);
                        vm.TotalBanks = banks?.Count ?? 0;
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Dashboard banks error: {ex.Message}"); }

                // ── Bank Accounts ─────────────────────────────────────
                try
                {
                    var accResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/BankAccount/GetAccountByCompany?CompanyId={companyId}");
                    if (accResp?.Success == true && accResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(accResp.Data);
                        var accounts = JsonSerializer.Deserialize<List<object>>(json, opts);
                        vm.TotalBankAccounts = accounts?.Count ?? 0;
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Dashboard accounts error: {ex.Message}"); }
            }
            catch (Exception ex)
            {
                vm.FetchError = $"Some data could not be loaded: {ex.Message}";
                Console.WriteLine($"Dashboard error: {ex}");
            }

            return View(vm);
        }
    }
}

