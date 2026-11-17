using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApiClient _apiClient;

        public AuthController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _apiClient.PostAsync<LoginResponse>("api/Auth/login", new
                {
                    identifier = model.Identifier,
                    password = model.Password
                });

                if (response?.Success == true && response.Data != null)
                {
                    var data = response.Data;
                    
                    // Store user session
                    var userSession = new UserSessionModel
                    {
                        Id = data.User.Id,
                        Name = data.User.Name,
                        Email = data.User.Email,
                        Role = data.User.Role,
                        CompanyId = data.User.CompanyId,
                        AccessToken = data.AccessToken,
                        RefreshToken = data.RefreshToken,
                        IsHeadOffice = data.IsHeadOffice,
                        Permissions = data.Permissions,
                        Menus = data.Menus,
                        Branches = data.Branches
                    };

                    HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userSession));
                    HttpContext.Session.SetString("AccessToken", userSession.AccessToken);

                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError("", response?.Message ?? "Invalid login attempt.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Login failed: {ex.Message}");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult SwitchBranch([FromBody] SwitchBranchRequest request)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
                return Json(new { success = false, message = "Session expired" });

            var userSession = JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (userSession == null)
                return Json(new { success = false, message = "Invalid session" });

            // BranchId == 0 means "clear branch" → back to consolidated (all branches) view
            if (request.BranchId == 0)
            {
                userSession.CurrentBranchId = null;
                HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userSession));
                return Json(new { success = true, message = "Switched to All Branches" });
            }

            var branch = userSession.Branches.FirstOrDefault(b => b.Id == request.BranchId);
            if (branch == null)
                return Json(new { success = false, message = "Branch not found" });

            userSession.CurrentBranchId = request.BranchId;
            HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userSession));

            return Json(new { success = true, message = $"Switched to {branch.CompanyName}" });
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
