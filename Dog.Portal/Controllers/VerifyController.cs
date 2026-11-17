using Dog.Application.Features.DogTraining.DTOs;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    /// <summary>Public certificate verification page (no login) — the target of the QR code on training certificates.</summary>
    public class VerifyController : Controller
    {
        private readonly IApiClient _api;
        public VerifyController(IApiClient api) => _api = api;

        [HttpGet("/Verify/Training/{token}")]
        public async Task<IActionResult> Training(string token)
        {
            CertificateVerificationDto result;
            try
            {
                // Never reveal why a lookup failed beyond "not found"; the API endpoint is anonymous by design.
                var raw = await _api.GetRawAsync($"api/Training/verify/{Uri.EscapeDataString(token ?? "")}");
                var parsed = raw.IsSuccessStatusCode
                    ? JsonSerializer.Deserialize<ApiResponse<CertificateVerificationDto>>(raw.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null;
                result = parsed?.Data ?? new CertificateVerificationDto { Valid = false, Message = "This certificate could not be verified right now. Please try again later." };
            }
            catch
            {
                result = new CertificateVerificationDto { Valid = false, Message = "This certificate could not be verified right now. Please try again later." };
            }
            return View(result);
        }

        /// <summary>Simple entry page: type a certificate's verification code.</summary>
        [HttpGet("/Verify")]
        public IActionResult Index() => View();
    }
}
