using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Dog.Application.Features.Banks.DTOs;
using Dog.Application.Features.BankAccounts.DTOs;

namespace Dog.Portal.Controllers
{
    public class BanksController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public BanksController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        // Banks Management
        public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new BankListViewModel
            {
                SearchTerm = search,
                Page = page,
                PageSize = pageSize
            };

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Bank?CompanyId={companyId}");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = JsonSerializer.Serialize(resp.Data);
                    var banks = JsonSerializer.Deserialize<List<BankDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (banks != null)
                    {
                        // Apply search filter
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            banks = banks.Where(b => 
                                (b.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (b.AccountName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                            ).ToList();
                        }

                        // Apply status filter
                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            var isActive = status.ToLower() == "active";
                            banks = banks.Where(b => b.IsActive == isActive).ToList();
                        }

                        vm.TotalCount = banks.Count;
                        vm.BankList = banks.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    }
                }

                ViewBag.StatusFilter = status;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load banks. Error: {ex.Message}";
                Console.WriteLine($"Banks Error: {ex}");
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBankRequest req)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var dto = new CreateBankDto
                {
                    CompanyId = companyId,
                    Name = req.Name,
                    AccountName = req.AccountName,
                    IsActive = req.IsActive
                };

                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Bank/CreateBank", dto);

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank created successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to create bank" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateBankRequest req)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var dto = new CreateBankDto
                {
                    CompanyId = companyId,
                    Name = req.Name,
                    AccountName = req.AccountName,
                    IsActive = req.IsActive
                };

                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/Bank/Bank/{req.Id}", dto);

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank updated successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to update bank" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/Bank/Bank/{id}");

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank deleted successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to delete bank" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int id)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<object>>($"api/Bank/Bank/{id}/toggle", new { });

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank status updated successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to update bank status" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Bank Accounts Management
        public async Task<IActionResult> Accounts(int? bankId, string? search, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new BankAccountListViewModel
            {
                BankId = bankId,
                SearchTerm = search,
                Page = page,
                PageSize = pageSize
            };

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                
                // Get bank accounts
                var accountsResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/BankAccount/GetAccountByCompany?CompanyId={companyId}");
                
                if (accountsResp?.Success == true && accountsResp.Data != null)
                {
                    var jsonString = JsonSerializer.Serialize(accountsResp.Data);
                    var accounts = JsonSerializer.Deserialize<List<BankAccountListDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (accounts != null)
                    {
                        // Apply bank filter
                        if (bankId.HasValue)
                        {
                            accounts = accounts.Where(a => a.BankId == bankId.Value).ToList();
                            vm.BankName = accounts.FirstOrDefault()?.BankName;
                        }

                        // Apply search filter
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            accounts = accounts.Where(a => 
                                (a.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (a.AccountNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (a.BankName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (a.BranchName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                            ).ToList();
                        }

                        vm.TotalCount = accounts.Count;
                        vm.BankAccountList = accounts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    }
                }

                // Get banks for dropdown
                var banksResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Bank?CompanyId={companyId}");
                if (banksResp?.Success == true && banksResp.Data != null)
                {
                    var banksJson = JsonSerializer.Serialize(banksResp.Data);
                    var banks = JsonSerializer.Deserialize<List<BankDto>>(banksJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    vm.Banks = banks?.Where(b => b.IsActive).ToList() ?? new List<BankDto>();
                }

                // Get currencies for dropdown
                var currenciesResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Currency/GetPubicCurrencies");
                if (currenciesResp?.Success == true && currenciesResp.Data != null)
                {
                    var currenciesJson = JsonSerializer.Serialize(currenciesResp.Data);
                    var currencies = JsonSerializer.Deserialize<List<CurrencyDto>>(currenciesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    vm.Currencies = currencies?.Where(c => c.IsActive).ToList() ?? new List<CurrencyDto>();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load bank accounts. Error: {ex.Message}";
                Console.WriteLine($"Bank Accounts Error: {ex}");
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateBankAccountRequest req)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var dto = new CreateBankAccountDto
                {
                    CompanyId = companyId,
                    Name = req.Name,
                    BankId = req.BankId,
                    CurrencyId = req.CurrencyId,
                    BranchName = req.BranchName,
                    BranchCode = req.BranchCode,
                    SwiftCode = req.SwiftCode,
                    AccountNumber = req.AccountNumber,
                    IsActive = req.IsActive
                };

                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/BankAccount/CreateBankAccount", dto);

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank account created successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to create bank account" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateBankAccountRequest req)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var dto = new CreateBankAccountDto
                {
                    CompanyId = companyId,
                    Name = req.Name,
                    BankId = req.BankId,
                    CurrencyId = req.CurrencyId,
                    BranchName = req.BranchName,
                    BranchCode = req.BranchCode,
                    SwiftCode = req.SwiftCode,
                    AccountNumber = req.AccountNumber,
                    IsActive = req.IsActive
                };

                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/BankAccount/BankAccount/{req.Id}", dto);

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank account updated successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to update bank account" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/BankAccount/BankAccount/{id}");

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank account deleted successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to delete bank account" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAccount(int id)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<object>>($"api/BankAccount/BankAccount/{id}/toggle", new { });

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Bank account status updated successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to update bank account status" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
