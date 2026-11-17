using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Portal.Controllers
{
    public class BillingSettingsController : Controller
    {
        private readonly ICurrentUserInfoService _user;
        public BillingSettingsController(ICurrentUserInfoService user) => _user = user;

        public async Task<IActionResult> Index()
        {
            if (!await _user.IsAuthenticated()) return RedirectToAction("Login", "Auth");
            return View();
        }
    }
}
