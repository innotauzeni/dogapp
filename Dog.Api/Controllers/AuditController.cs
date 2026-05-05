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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAuditEntries(
     [FromQuery] string? entity,
     [FromQuery] string? action,
     [FromQuery] string? username,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 50,
     CancellationToken ct = default)
        {
            var query = _db.ka_auditentries.AsQueryable();
            if (!string.IsNullOrEmpty(entity)) query = query.Where(a => a.EntityName == entity);
            if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.ActionType == action);
            if (!string.IsNullOrEmpty(username)) query = query.Where(a => a.Username!.Contains(username));

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









