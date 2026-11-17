using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    /// <summary>
    /// Admin editor for the public landing page. Loads the effective content (DB override or baked-in defaults),
    /// lets an admin edit every section, and saves it back as a JSON blob to the API (ka_appsettings key "Landing:Json").
    /// No redeploy needed to change the site.
    /// </summary>
    public class LandingAdminController : Controller
    {
        private readonly IApiClient _apiClient;
        private static readonly JsonSerializerOptions _camel =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = false };

        public LandingAdminController(IApiClient apiClient) => _apiClient = apiClient;

        private bool IsAuthenticated() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        // GET /LandingAdmin
        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var model = new LandingViewModel();
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<string>>("api/Landing");
                if (!string.IsNullOrWhiteSpace(resp?.Data))
                {
                    var custom = JsonSerializer.Deserialize<LandingViewModel>(resp.Data, _camel);
                    if (custom != null) model = custom;
                }
            }
            catch { /* fall back to defaults */ }

            ViewBag.Json = JsonSerializer.Serialize(model, _camel);
            return View();
        }

        // POST /LandingAdmin/Save  { json: "<landing model json>" }
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SavePayload body)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (string.IsNullOrWhiteSpace(body?.Json)) return Json(new { success = false, message = "Nothing to save." });

            // Validate it parses as a LandingViewModel before storing.
            try { _ = JsonSerializer.Deserialize<LandingViewModel>(body.Json, _camel); }
            catch (Exception ex) { return Json(new { success = false, message = "Invalid content: " + ex.Message }); }

            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>("api/Landing", new { json = body.Json });
                if (resp?.Success == true) return Json(new { success = true, message = "Landing page updated." });
                return Json(new { success = false, message = resp?.Message ?? "Save failed (are you an admin?)." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /LandingAdmin/Reset — clear the override so defaults show again.
        [HttpPost]
        public async Task<IActionResult> Reset()
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>("api/Landing", new { json = "" });
                return Json(new { success = resp?.Success == true, message = resp?.Success == true ? "Reset to defaults." : resp?.Message });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /LandingAdmin/UploadLogo — upload logo image
        [HttpPost]
        public async Task<IActionResult> UploadLogo(IFormFile? file)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file provided." });

            var allowed = new[] { "image/png", "image/jpeg", "image/svg+xml", "image/webp" };
            if (!allowed.Contains(file.ContentType)) return Json(new { success = false, message = "Only PNG, JPG, SVG, or WebP allowed." });

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "landing");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var url = $"/uploads/landing/{fileName}";
                return Json(new { success = true, data = url, message = "Logo uploaded." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /LandingAdmin/UploadFavicon — upload favicon image
        [HttpPost]
        public async Task<IActionResult> UploadFavicon(IFormFile? file)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file provided." });

            var allowed = new[] { "image/png", "image/jpeg", "image/svg+xml", "image/webp", "image/x-icon", "image/vnd.microsoft.icon" };
            if (!allowed.Contains(file.ContentType)) return Json(new { success = false, message = "Only ICO, PNG, JPG, SVG, or WebP allowed." });

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "landing");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"favicon-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var url = $"/uploads/landing/{fileName}";
                return Json(new { success = true, data = url, message = "Favicon uploaded." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /LandingAdmin/UploadApk — upload APK or app file
        [HttpPost]
        public async Task<IActionResult> UploadApk(IFormFile? file)
        {
            System.Diagnostics.Debug.WriteLine($"UploadApk called. File: {file?.FileName}, Length: {file?.Length}");
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = $"No file provided. File is null: {file == null}, Length: {file?.Length}" });

            var maxSize = 500 * 1024 * 1024; // 500MB
            if (file.Length > maxSize) return Json(new { success = false, message = "File too large. Max 500MB." });

            var allowed = new[] { ".apk", ".ipa", ".exe", ".dmg", ".aab" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext)) return Json(new { success = false, message = $"Only APK, IPA, AAB, EXE, or DMG files allowed. You uploaded: {ext}" });

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "apps");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    await file.CopyToAsync(stream);

                // Verify file was created
                if (!System.IO.File.Exists(filePath))
                    return Json(new { success = false, message = "File upload failed - file not created on disk" });

                var url = $"/uploads/apps/{fileName}";
                return Json(new { success = true, data = url, message = "APK uploaded successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }

        // POST /LandingAdmin/UploadHeroImage — upload hero carousel image
        [HttpPost]
        public async Task<IActionResult> UploadHeroImage(IFormFile? file)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file provided." });

            var allowed = new[] { "image/png", "image/jpeg", "image/svg+xml", "image/webp" };
            if (!allowed.Contains(file.ContentType)) return Json(new { success = false, message = "Only PNG, JPG, SVG, or WebP allowed." });

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "hero");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var url = $"/uploads/hero/{fileName}";
                return Json(new { success = true, data = url, message = "Hero image uploaded." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        public class SavePayload { public string? Json { get; set; } }
    }
}
