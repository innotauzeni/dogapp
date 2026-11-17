using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    public class CustomerCompaniesController : Controller
    {
        private readonly IApiClient _api;
        private readonly ICurrentUserInfoService _user;
        public CustomerCompaniesController(IApiClient api, ICurrentUserInfoService user) { _api = api; _user = user; }

        public async Task<IActionResult> Index(int? open)
        {
            if (!await _user.IsAuthenticated()) return RedirectToAction("Login", "Auth");
            ViewBag.OpenId = open;
            return View();
        }

        /// <summary>The companies for the dropdowns on the customer and invoice forms (works with just customer / invoice access).</summary>
        [HttpGet]
        public async Task<IActionResult> Lookup(string? search)
        {
            if (!await _user.IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            var companyId = await _user.GetActiveCompanyId();
            var r = await _api.GetRawAsync($"api/CustomerCompany/Lookup?CompanyId={companyId}&search={Uri.EscapeDataString(search ?? "")}");
            return Content(r.IsSuccessStatusCode ? r.Body : "{\"success\":false,\"data\":[]}", "application/json");
        }
    }
}
