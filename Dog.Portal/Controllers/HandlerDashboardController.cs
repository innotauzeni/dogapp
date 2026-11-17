using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class HandlerDashboardController : Controller
    {
        private readonly IApiClient _apiClient;
        public HandlerDashboardController(IApiClient apiClient) => _apiClient = apiClient;

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Auth");

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

            if (userSession == null || (!userSession.IsDogHandler && userSession.Role != "Handler"))
                return RedirectToAction("Index", "Dashboard");

            try
            {
                var userId = userSession.Id;
                var dogsResp = await _apiClient.GetAsync<ApiResponse<List<Animal>>>($"api/animals/handler/{userId}");
                var dogs = dogsResp?.Data ?? new List<Animal>();

                var vm = new HandlerDashboardViewModel
                {
                    Dogs = dogs,
                    TotalAssigned = dogs.Count,
                    VaccineDue = dogs.Count(d => d.Vaccinations?.Any(v => DateTime.Now > (v.NextDueDate ?? DateTime.MaxValue)) ?? false),
                    NeedsAttention = 0, // Will be calculated from care records
                    TodayActivities = 0
                };

                return View(vm);
            }
            catch
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
    }

    public class HandlerDashboardViewModel
    {
        public List<Animal> Dogs { get; set; } = new();
        public int TotalAssigned { get; set; }
        public int VaccineDue { get; set; }
        public int NeedsAttention { get; set; }
        public int TodayActivities { get; set; }
    }

    public class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public List<Vaccination>? Vaccinations { get; set; }
        public List<MatingRecordInfo> MatingRecords { get; set; } = new();
    }

    public class MatingRecordInfo
    {
        public int Id { get; set; }
        public string? SireName { get; set; }
        public string? DamName { get; set; }
        public DateTime? MatingDate { get; set; }
        public List<LitterInfo> Litters { get; set; } = new();
    }

    public class LitterInfo
    {
        public int Id { get; set; }
        public DateTime? BirthDate { get; set; }
        public int TotalBorn { get; set; }
        public int SurvivedCount { get; set; }
        public int DiedCount { get; set; }
        public List<PuppyInfo> Puppies { get; set; } = new();
    }

    public class PuppyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

    public class Vaccination
    {
        public string VaccineName { get; set; } = string.Empty;
        public DateTime? NextDueDate { get; set; }
    }
}
