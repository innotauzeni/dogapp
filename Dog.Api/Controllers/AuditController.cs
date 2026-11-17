using Dog.Application.Common;
using Dog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AuditController(ApplicationDbContext db) => _db = db;

        // Audit rows now carry full records, previous values and the actor's contact details, so this
        // must not be open to anonymous callers (it used to be [AllowAnonymous]).
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAuditEntries(
     [FromQuery] string? entity,
     [FromQuery] string? action,
     [FromQuery] string? username,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 50,
     CancellationToken ct = default)
        {
            // The log stores INSERT / UPDATE / DELETE; the UI speaks Create / Update / Delete.
            if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)) action = "INSERT";

            var query = _db.ka_auditentries.AsQueryable();
            if (!string.IsNullOrEmpty(entity)) query = query.Where(a => a.EntityName == entity);
            if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.ActionType == action);

            // The user column holds the user's id, so a typed name has to be matched against the users table.
            if (!string.IsNullOrEmpty(username))
            {
                var term = username.Trim();
                var matchingIds = await _db.Users
                    .Where(u => (u.Name + " " + u.Surname).Contains(term) || (u.Email != null && u.Email.Contains(term)) || u.Id == term)
                    .Select(u => u.Id)
                    .ToListAsync(ct);
                query = query.Where(a => matchingIds.Contains(a.Username!) || a.Username!.Contains(term));
            }

            var total = await query.CountAsync(ct);
            var entries = await query
                .OrderByDescending(a => a.TimeStamp)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

            var data = entries.Select(a => new AuditItemDto
                {
                    Id = a.Id,
                    EntityName = a.EntityName,
                    ActionType = a.ActionType,
                    Username = a.Username,
                    TimeStamp = a.TimeStamp,
                    EntityId = a.EntityId,
                    Changes = a.Changes,
                }).ToList();

            await AuditEnricher.EnrichAsync(_db, data, ct);

            var entities = await _db.ka_auditentries
                .Select(a => a.EntityName)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync(ct);

            return Ok(ApiResponse<AuditPageDto>.Ok(new AuditPageDto
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Data = data,
                Entities = entities!,
            }));
        }
    }
}
