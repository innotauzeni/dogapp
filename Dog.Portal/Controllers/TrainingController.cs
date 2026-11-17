using Dog.Application.Common.Pdf;
using Dog.Application.Contracts;
using AppCompanyDto = Dog.Application.Features.Companies.DTOs.CompanyDto;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Portal.Helpers;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    /// <summary>Dog Training screens. The pages themselves load their data from <c>TrainingApi/…</c>; this controller
    /// serves the pages, stores uploaded files and builds the PDFs (health form, progress report, certificate).</summary>
    public class TrainingController : Controller
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
        private readonly IApiClient _api;
        private readonly IFileStorageService _storage;

        public TrainingController(IApiClient api, IFileStorageService storage) { _api = api; _storage = storage; }

        private UserSessionModel? Session()
        {
            var json = HttpContext.Session.GetString("UserSession");
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<UserSessionModel>(json);
        }

        private bool Can(params string[] permissions)
        {
            var p = PermHelper.For(HttpContext);
            return permissions.Any(p.Can);
        }

        /// <summary>Redirects to login when signed out; shows "no access" when the user lacks every listed permission.</summary>
        private IActionResult? Gate(params string[] anyOf)
        {
            if (Session() == null) return RedirectToAction("Login", "Auth");
            if (anyOf.Length > 0 && !Can(anyOf)) { Response.StatusCode = 403; return View("NoAccess"); }
            return null;
        }

        private static int ActiveCompanyId(UserSessionModel s)
            => s.IsHeadOffice && s.CurrentBranchId is > 0 ? s.CurrentBranchId.Value : Convert.ToInt32(s.CompanyId);

        // ── Pages ────────────────────────────────────────────────────────────
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));
        public IActionResult Dashboard() => Gate("training.dashboard.view") ?? View();
        public IActionResult Customers() => Gate("training.customer.view") ?? View();
        public IActionResult Dogs() => Gate("training.dog.view") ?? View();
        public IActionResult Invoices() => Gate("training.invoice.view") ?? View();
        public IActionResult HealthAssessments() => Gate("training.health.view") ?? View();
        public IActionResult Enrolments() => Gate("training.enrolment.view") ?? View();

        public IActionResult Enrolment(int id)
        {
            var g = Gate("training.enrolment.view", "training.log.view", "training.log.record", "training.progress.submit", "training.health.view");
            if (g != null) return g;
            ViewBag.EnrollmentId = id;
            return View();
        }

        public IActionResult HandlerAllocation() => Gate("training.handler.view") ?? View();
        public IActionResult Programmes() => Gate("training.programme.view") ?? View();
        public IActionResult Logbook() => Gate("training.log.view", "training.log.record") ?? View();

        /// <summary>Mobile-friendly daily log form for one dog.</summary>
        public IActionResult Log(int id, DateTime? date)
        {
            var g = Gate("training.log.view", "training.log.record");
            if (g != null) return g;
            ViewBag.EnrollmentId = id;
            ViewBag.Date = (date ?? DateTime.Today).ToString("yyyy-MM-dd");
            return View();
        }

        public IActionResult ProgressReports() => Gate("training.progress.view", "training.progress.submit") ?? View();

        /// <summary>Weekly report form / review page for one dog and week.</summary>
        public IActionResult ProgressReport(int enrollmentId, int week)
        {
            var g = Gate("training.progress.view", "training.progress.submit", "training.progress.review");
            if (g != null) return g;
            ViewBag.EnrollmentId = enrollmentId;
            ViewBag.Week = week;
            return View();
        }

        public IActionResult FinalAssessments() => Gate("training.assessment.view") ?? View();

        public IActionResult FinalAssessment(int enrollmentId)
        {
            var g = Gate("training.assessment.conduct", "training.assessment.view");
            if (g != null) return g;
            ViewBag.EnrollmentId = enrollmentId;
            return View();
        }

        public IActionResult Certificates() => Gate("training.certificate.view") ?? View();
        public IActionResult Reports() => Gate("training.reports.view") ?? View();

        // ── Training dog photo (shown in lists and on the health form) ──
        [HttpPost]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<IActionResult> UploadDogPhoto(IFormFile file)
        {
            if (Session() == null) return Json(new { success = false, message = "Session expired." });
            if (!Can("training.dog.manage")) return Json(new { success = false, message = "You do not have permission to do this." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected." });
            var ext = (Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif")) return Json(new { success = false, message = "Only JPG, PNG, WebP or GIF photos are allowed." });
            if (file.Length > 12 * 1024 * 1024) return Json(new { success = false, message = "That photo is too large (limit 12 MB)." });
            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var contentType = string.IsNullOrEmpty(file.ContentType) ? "image/jpeg" : file.ContentType;
                string url;
                if (_storage.IsEnabled) url = await _storage.UploadBytesAsync(ms.ToArray(), file.FileName, contentType, "training");
                else
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "training");
                    Directory.CreateDirectory(dir);
                    var stored = $"{Guid.NewGuid()}{ext}";
                    await System.IO.File.WriteAllBytesAsync(Path.Combine(dir, stored), ms.ToArray());
                    url = $"/uploads/training/{stored}";
                }
                return Json(new { success = true, url });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Upload failed: {ex.Message}" }); }
        }

        // ── Uploads: photos, videos and vet documents ────────────────────────
        private static readonly Dictionary<string, long> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = 12 * 1024 * 1024, [".jpeg"] = 12 * 1024 * 1024, [".png"] = 12 * 1024 * 1024, [".gif"] = 12 * 1024 * 1024, [".webp"] = 12 * 1024 * 1024,
            [".mp4"] = 80 * 1024 * 1024, [".mov"] = 80 * 1024 * 1024, [".webm"] = 80 * 1024 * 1024, [".m4v"] = 80 * 1024 * 1024,
            [".pdf"] = 12 * 1024 * 1024,
        };

        /// <summary>Stores one file and attaches it to a health assessment, daily log or progress report.</summary>
        [HttpPost]
        [RequestSizeLimit(90 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 90 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file, string entityType, int entityId, string? notes)
        {
            if (Session() == null) return Json(new { success = false, message = "Session expired." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected." });

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedFiles.TryGetValue(ext ?? "", out var max))
                return Json(new { success = false, message = "Only photos (JPG, PNG, GIF, WebP), videos (MP4, MOV, WebM) and PDF documents are allowed." });
            if (file.Length > max)
                return Json(new { success = false, message = $"That file is too large (limit {max / 1024 / 1024} MB)." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

                string url;
                if (_storage.IsEnabled)
                {
                    url = await _storage.UploadBytesAsync(ms.ToArray(), file.FileName, contentType, "training");
                }
                else
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "training");
                    Directory.CreateDirectory(dir);
                    var stored = $"{Guid.NewGuid()}{ext!.ToLowerInvariant()}";
                    await System.IO.File.WriteAllBytesAsync(Path.Combine(dir, stored), ms.ToArray());
                    url = $"/uploads/training/{stored}";
                }

                var raw = await _api.PostRawAsync("api/Training/attachments", new
                {
                    entityType, entityId, fileName = file.FileName, fileUrl = url, contentType, notes
                });
                if (!raw.IsSuccessStatusCode)
                    return Json(new { success = false, message = raw.StatusCode == 403 ? "You do not have permission to do this." : "The file could not be attached." });

                var parsed = JsonSerializer.Deserialize<ApiResponse<object>>(raw.Body, Json);
                return Json(new { success = parsed?.Success ?? false, message = parsed?.Message ?? "File attached.", url });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Upload failed: {ex.Message}" }); }
        }

        // ── PDFs ─────────────────────────────────────────────────────────────
        private async Task<(AppCompanyDto? Company, byte[]? Logo)> BrandingAsync(int companyId)
        {
            AppCompanyDto? company = null;
            try
            {
                var r = await _api.GetAsync<ApiResponse<object>>($"api/companies/{companyId}");
                if (r?.Data != null)
                    company = JsonSerializer.Deserialize<AppCompanyDto>(JsonSerializer.Serialize(r.Data), Json);
            }
            catch { /* branding is optional */ }
            var logo = await PdfImageLoader.LoadAsync(company?.Logo, $"{Request.Scheme}://{Request.Host}");
            return (company, logo);
        }

        private async Task<T?> GetDataAsync<T>(string endpoint)
        {
            var raw = await _api.GetRawAsync(endpoint);
            if (!raw.IsSuccessStatusCode) return default;
            var parsed = JsonSerializer.Deserialize<ApiResponse<T>>(raw.Body, Json);
            return parsed is { Success: true } ? parsed.Data : default;
        }

        [HttpGet]
        public async Task<IActionResult> HealthFormPdf(int id)
        {
            var s = Session();
            if (s == null) return RedirectToAction("Login", "Auth");
            if (!Can("training.health.view")) return StatusCode(403, "You do not have permission to do this.");

            var a = await GetDataAsync<HealthAssessmentDto>($"api/Training/health-assessments/{id}");
            if (a == null) return NotFound("Health assessment not found.");
            var (company, logo) = await BrandingAsync(ActiveCompanyId(s));
            var pdf = HealthFormPdfBuilder.Build(a, company, logo);
            return File(pdf, "application/pdf", $"Health_Check_{a.DogName}_{a.AssessmentDate:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ProgressReportPdf(int id)
        {
            var s = Session();
            if (s == null) return RedirectToAction("Login", "Auth");
            if (!Can("training.progress.view", "training.progress.submit", "training.progress.review")) return StatusCode(403, "You do not have permission to do this.");

            var r = await GetDataAsync<ProgressReportDto>($"api/Training/progress-reports/{id}");
            if (r == null) return NotFound("Report not found.");
            var (company, logo) = await BrandingAsync(ActiveCompanyId(s));
            var pdf = ProgressReportPdfBuilder.Build(r, company, logo);
            return File(pdf, "application/pdf", $"Progress_Report_{r.DogName}_Week{r.WeekNumber}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> InvoicePdf(int id)
        {
            var s = Session();
            if (s == null) return RedirectToAction("Login", "Auth");
            if (!Can("training.invoice.view")) return StatusCode(403, "You do not have permission to do this.");

            var inv = await GetDataAsync<TrainingInvoiceDto>($"api/Training/invoices/{id}");
            if (inv == null) return NotFound("Invoice not found.");
            var (company, logo) = await BrandingAsync(inv.CompanyId);
            Dog.Application.Features.BillingSetup.PdfInfoDto? info = null;
            try { info = await GetDataAsync<Dog.Application.Features.BillingSetup.PdfInfoDto>($"api/BillingSetup/pdf-info?CompanyId={inv.CompanyId}"); } catch { /* optional */ }
            var pdf = TrainingInvoicePdfBuilder.Build(inv, company, logo, info?.Profile, info?.Banks);
            return File(pdf, "application/pdf", $"Training_Invoice_{inv.InvoiceNumber}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> CertificatePdf(int id)
        {
            var s = Session();
            if (s == null) return RedirectToAction("Login", "Auth");
            if (!Can("training.certificate.view")) return StatusCode(403, "You do not have permission to do this.");

            var cert = await GetDataAsync<TrainingCertificateDto>($"api/Training/certificates/{id}");
            if (cert == null) return NotFound("Certificate not found.");
            if (cert.IsRevoked) return BadRequest("This certificate has been revoked.");

            var (company, logo) = await BrandingAsync(cert.CompanyId);
            var verifyUrl = $"{Request.Scheme}://{Request.Host}/Verify/Training/{cert.VerificationToken}";
            var pdf = TrainingCertificatePdfBuilder.Build(cert, company, logo, verifyUrl);
            return File(pdf, "application/pdf", $"Training_Certificate_{cert.DogName}_{cert.CertificateNumber}.pdf");
        }
    }
}
