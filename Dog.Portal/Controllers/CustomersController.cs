using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Dog.Application.Features.Customers.DTOs;
using Dog.Application.Features.CustomerContracts.DTOs;
using Dog.Application.Features.CustomerSites.DTOs;
using Dog.Application.Features.Deployments.DTOs;
using AnimalListDto = Dog.Application.Features.Animals.DTOs.AnimalListDto;

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
                var companyId = await _currentUserService.GetActiveCompanyId();
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

        public async Task<IActionResult> Details(int id)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var vm = new CustomerDetailsViewModel();

            try
            {
                var custResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Customer/GetCustomerById?Id={id}");
                if (custResp?.Success != true || custResp.Data == null)
                {
                    ViewBag.Error = custResp?.Message ?? "Customer not found.";
                    return View(vm);
                }
                vm.Customer = JsonSerializer.Deserialize<CustomerDto>(JsonSerializer.Serialize(custResp.Data), jsonOpts)!;

                var sitesResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/CustomerSite?customerId={id}");
                if (sitesResp?.Success == true && sitesResp.Data != null)
                    vm.Sites = JsonSerializer.Deserialize<List<CustomerSiteDto>>(JsonSerializer.Serialize(sitesResp.Data), jsonOpts) ?? new();

                var contractsResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/CustomerContract?customerId={id}");
                if (contractsResp?.Success == true && contractsResp.Data != null)
                    vm.Contracts = JsonSerializer.Deserialize<List<CustomerContractDto>>(JsonSerializer.Serialize(contractsResp.Data), jsonOpts) ?? new();

                var companyId = await _currentUserService.GetActiveCompanyId();

                var dogsResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal/GetPets?CompanyId={companyId}");
                if (dogsResp?.Success == true && dogsResp.Data != null)
                {
                    var allDogs = JsonSerializer.Deserialize<List<AnimalListDto>>(JsonSerializer.Serialize(dogsResp.Data), jsonOpts) ?? new();
                    vm.OwnedDogs = allDogs.Where(d => d.CustomerId == id).ToList();
                }

                var depResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Deployment?CompanyId={companyId}");
                if (depResp?.Success == true && depResp.Data != null)
                {
                    var allDeployments = JsonSerializer.Deserialize<List<DeploymentDto>>(JsonSerializer.Serialize(depResp.Data), jsonOpts) ?? new();
                    vm.Deployments = allDeployments.Where(d => d.CustomerId == id).OrderByDescending(d => d.StartDate).ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load customer. Error: {ex.Message}";
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
                var companyId = await _currentUserService.GetActiveCompanyId();
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
                    JobTitle = req.JobTitle,
                    CustomerCompanyId = req.CustomerCompanyId,
                    IsActive = req.IsActive
                };

                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Customer/CreateCustomer", dto);

                if (resp?.Success == true)
                {
                    // Optionally record the initial contract alongside the new customer.
                    if (!string.IsNullOrWhiteSpace(req.ContractDate) && DateTime.TryParse(req.ContractDate, out var contractDate) && resp.Data != null)
                    {
                        var created = JsonSerializer.Deserialize<CustomerDto>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (created != null)
                        {
                            var contractDto = new CreateCustomerContractDto
                            {
                                CustomerId = created.Id,
                                CompanyId = companyId,
                                ContractDate = contractDate,
                                ContractDetails = req.ContractDetails,
                                IsActive = true,
                            };
                            await _apiClient.PostAsync<ApiResponse<object>>("api/CustomerContract", contractDto);
                        }
                    }

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
                var companyId = await _currentUserService.GetActiveCompanyId();
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
                    JobTitle = req.JobTitle,
                    CustomerCompanyId = req.CustomerCompanyId,
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
