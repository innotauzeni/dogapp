using Dog.Api.Services;
using Dog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dog.Api.Controllers
{
    /// <summary>Notices to show in the web portal while they are running (the portal banner disappears by itself).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppClock _clock;
        public AnnouncementsController(ApplicationDbContext db, IAppClock clock) { _db = db; _clock = clock; }

        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> Active(CancellationToken ct)
        {
            var today = _clock.Today;
            var list = (await _db.ka_appannouncements.AsNoTracking().Where(a => a.IsActive).ToListAsync(ct))
                .Where(a => today >= a.StartDate.Date && today < a.StartDate.Date.AddDays(Math.Max(1, a.PortalDays)))
                .Select(a => new
                {
                    id = a.Id, key = a.Key, title = a.Title, message = a.PortalMessage, linkUrl = a.LinkUrl, linkText = a.LinkText,
                    endsOn = a.StartDate.Date.AddDays(Math.Max(1, a.PortalDays) - 1)
                }).ToList();
            return Ok(new { success = true, data = list });
        }
    }
}
