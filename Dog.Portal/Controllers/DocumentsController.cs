using Dog.Application.Contracts;
using Dog.Application.Features.Documents.DTOs;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;
        private readonly IFileStorageService _storage;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
        private const long MaxBytes = 10 * 1024 * 1024;

        public DocumentsController(IApiClient apiClient, ICurrentUserInfoService currentUserService, IFileStorageService storage)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
            _storage = storage;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        [HttpGet]
        public async Task<IActionResult> GetByEntity(string entityType, int entityId)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Document?entityType={entityType}&entityId={entityId}");
                var docs = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<DocumentAttachmentDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : new List<DocumentAttachmentDto>();
                return Json(new { success = true, data = docs });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string entityType, int entityId, string? notes)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file uploaded." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return Json(new { success = false, message = "Only images, PDF and Word documents are allowed." });
            if (file.Length > MaxBytes)
                return Json(new { success = false, message = "File must be 10 MB or smaller." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

                string fileUrl;
                if (_storage.IsEnabled)
                {
                    fileUrl = await _storage.UploadBytesAsync(ms.ToArray(), file.FileName, contentType, "documents");
                }
                else
                {
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                    var storedName = $"{Guid.NewGuid()}{ext}";
                    await System.IO.File.WriteAllBytesAsync(Path.Combine(uploadsDir, storedName), ms.ToArray());
                    fileUrl = $"/uploads/documents/{storedName}";
                }

                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateDocumentAttachmentDto
                {
                    CompanyId = companyId,
                    EntityType = entityType,
                    EntityId = entityId,
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    ContentType = contentType,
                    Notes = notes,
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Document/Create", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to attach document" });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Upload failed: {ex.Message}" }); }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/Document/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to delete document" });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }
    }
}
