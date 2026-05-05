using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Dog.Application.Features.Customers.DTOs;

namespace Dog.Portal.Controllers
{
    public class CustomersController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;

        public CustomersController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new CustomerListViewModel
            {
                SearchTerm = search,
                Page = page,
                PageSize = pageSize
            };

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Customer?CompanyId={companyId}");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = JsonSerializer.Serialize(resp.Data);
                    var customers = JsonSerializer.Deserialize<List<CustomerDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (customers != null)
                    {
                        // Apply search filter
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            customers = customers.Where(c => 
                                (c.FirstName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (c.LastName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (c.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (c.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (c.Cellphone?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                                (c.IDnumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                            ).ToList();
                        }

                        vm.TotalCount = customers.Count;
                        vm.CustomerList = customers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load customers. Error: {ex.Message}";
                Console.WriteLine($"Customers Error: {ex}");
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var dto = new CreateCustomerDto
                {
                    CompanyId = companyId,
                    Profile = req.Profile,
                    Gender = req.Gender,
                    FirstName = req.FirstName,
                    LastName = req.LastName,
                    FullName = req.FullName,
                    Dob = req.Dob,
                    IDnumber = req.IDnumber,
                    Cellphone = req.Cellphone,
                    Email = req.Email,
                    Address = req.Address,
                    IsActive = req.IsActive
                };

                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Customer/CreateCustomer", dto);

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Customer created successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to create customer" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCustomerRequest req)
        {
            if (!await IsAuthenticated()) 
                return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var companyId = await _currentUserService.GetUserCompanyId();
                var dto = new CreateCustomerDto
                {
                    CompanyId = companyId,
                    Profile = req.Profile,
                    Gender = req.Gender,
                    FirstName = req.FirstName,
                    LastName = req.LastName,
                    FullName = req.FullName,
                    Dob = req.Dob,
                    IDnumber = req.IDnumber,
                    Cellphone = req.Cellphone,
                    Email = req.Email,
                    Address = req.Address,
                    IsActive = req.IsActive
                };

                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/Customer/Customer/{req.Id}", dto);

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Customer updated successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to update customer" });
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
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/Customer/Customer/{id}");

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Customer deleted successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to delete customer" });
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
                var resp = await _apiClient.PostAsync<ApiResponse<object>>($"api/Customer/Customer/{id}/toggle", new { });

                if (resp?.Success == true)
                {
                    return Json(new { success = true, message = "Customer status updated successfully!" });
                }

                return Json(new { success = false, message = resp?.Message ?? "Failed to update customer status" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
