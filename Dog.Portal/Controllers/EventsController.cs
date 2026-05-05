using Dog.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class EventsController : Controller
    {
        private const string SessionKey = "DashboardEvents";
        private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        private List<DashboardEvent> GetEvents()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return new List<DashboardEvent>();
            return JsonSerializer.Deserialize<List<DashboardEvent>>(json, Opts) ?? new List<DashboardEvent>();
        }

        private void SaveEvents(List<DashboardEvent> events)
        {
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(events));
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            if (!IsAuthenticated()) return Json(new { success = false });
            var events = GetEvents().OrderBy(e => e.EventDate).ToList();
            return Json(new { success = true, data = events });
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateEventRequest req)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            if (string.IsNullOrWhiteSpace(req.Title)) return Json(new { success = false, message = "Title is required" });
            if (!DateTime.TryParse(req.EventDate, out var date)) return Json(new { success = false, message = "Invalid date" });

            var categoryColors = new Dictionary<string, string>
            {
                ["general"] = "#667eea",
                ["vaccination"] = "#10b981",
                ["mating"] = "#ec4899",
                ["litter"] = "#f59e0b",
                ["deployment"] = "#3b82f6",
                ["other"] = "#94a3b8"
            };

            var ev = new DashboardEvent
            {
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                EventDate = date,
                EventTime = req.EventTime,
                Category = req.Category,
                Color = categoryColors.GetValueOrDefault(req.Category, "#667eea"),
                IsCompleted = false
            };

            var events = GetEvents();
            events.Add(ev);
            SaveEvents(events);

            return Json(new { success = true, message = "Event created!", data = ev });
        }

        [HttpPost]
        public IActionResult Toggle(string id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            var events = GetEvents();
            var ev = events.FirstOrDefault(e => e.Id == id);
            if (ev == null) return Json(new { success = false, message = "Event not found" });
            ev.IsCompleted = !ev.IsCompleted;
            SaveEvents(events);
            return Json(new { success = true, isCompleted = ev.IsCompleted });
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            var events = GetEvents();
            var removed = events.RemoveAll(e => e.Id == id);
            if (removed == 0) return Json(new { success = false, message = "Event not found" });
            SaveEvents(events);
            return Json(new { success = true, message = "Event deleted" });
        }
    }
}
