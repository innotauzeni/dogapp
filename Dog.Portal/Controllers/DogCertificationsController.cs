using Dog.Application.Common.Pdf;
using Dog.Application.Features.DogCertifications.DTOs;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AppDogProfileDto = Dog.Application.Features.Animals.DTOs.DogProfileDto;
using AppCompanyDto = Dog.Application.Features.Companies.DTOs.CompanyDto;

namespace Dog.Portal.Controllers
{
    public class DogCertificationsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

        public DogCertificationsController(IApiClient apiClient, ICurrentUserInfoService currentUserService)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        private Task<byte[]?> TryLoadImageAsync(string? url)
            => PdfImageLoader.LoadAsync(url, $"{Request.Scheme}://{Request.Host}");

        // GET /DogCertifications/Certificate/{id} — branded PDF for one certification.
        public async Task<IActionResult> Certificate(int id)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var certResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/DogCertification/{id}");
                if (certResp?.Success != true || certResp.Data == null)
                    return NotFound(certResp?.Message ?? "Certification not found.");

                var cert = JsonSerializer.Deserialize<DogCertificationDto>(JsonSerializer.Serialize(certResp.Data), Opts);
                if (cert == null) return NotFound("Certification not found.");

                var dogResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/animal/GetDogProfile/{cert.DogId}");
                if (dogResp?.Success != true || dogResp.Data == null)
                    return NotFound("Dog not found.");

                var dogProfile = JsonSerializer.Deserialize<AppDogProfileDto>(JsonSerializer.Serialize(dogResp.Data), Opts);
                if (dogProfile?.Dog == null) return NotFound("Dog not found.");

                AppCompanyDto? company = null;
                try
                {
                    var compResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/companies/{dogProfile.Dog.CompanyId}");
                    if (compResp?.Data != null)
                        company = JsonSerializer.Deserialize<AppCompanyDto>(JsonSerializer.Serialize(compResp.Data), Opts);
                }
                catch { /* branding is optional */ }

                var logo = await TryLoadImageAsync(company?.Logo);
                var photo = await TryLoadImageAsync(dogProfile.Dog.PhotoUrl);

                var pdf = DogCertificationPdfBuilder.Build(cert, dogProfile.Dog, company, logo, photo);
                return File(pdf, "application/pdf", $"Certificate_{cert.CertificationName}_{dogProfile.Dog.Name}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Certificate generation failed: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetByDog(int dogId)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/DogCertification?dogId={dogId}");
                var certs = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<DogCertificationDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : new List<DogCertificationDto>();
                return Json(new { success = true, data = certs });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DogCertificationRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new CreateDogCertificationDto
                {
                    DogId = req.DogId,
                    CompanyId = companyId,
                    CertificationName = req.CertificationName,
                    IssuingBody = req.IssuingBody,
                    IssueDate = req.IssueDate,
                    ExpiryDate = req.ExpiryDate,
                    TrainerName = req.TrainerName,
                    Notes = req.Notes,
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/DogCertification", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to add certification" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] DogCertificationRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new UpdateDogCertificationDto
                {
                    Id = req.Id,
                    DogId = req.DogId,
                    CompanyId = companyId,
                    CertificationName = req.CertificationName,
                    IssuingBody = req.IssuingBody,
                    IssueDate = req.IssueDate,
                    ExpiryDate = req.ExpiryDate,
                    TrainerName = req.TrainerName,
                    Notes = req.Notes,
                };
                var resp = await _apiClient.PutAsync<ApiResponse<object>>($"api/DogCertification/{req.Id}", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to update certification" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/DogCertification/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to delete certification" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
