using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Authorization;
using Dog.Application.Common;
using Dog.Infrastructure.Persistence;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public SystemLogsController(ApplicationDbContext db) => _db = db;

        [HttpGet("errors")]
        [HasPermission("systemworker.view")]
        public async Task<IActionResult> GetErrors(
            [FromQuery] string? severity,
            [FromQuery] bool? resolved,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var query = _db.ka_sytemerrorlogs.IgnoreQueryFilters().AsQueryable();
            if (!string.IsNullOrEmpty(severity)) query = query.Where(e => e.Severity == severity);
            if (resolved.HasValue)               query = query.Where(e => e.IsResolved == resolved.Value);

            var total = await query.CountAsync(ct);
            var data  = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(e => new ErrorLogItemDto
                {
                    Id           = e.Id,
                    Uuid         = e.Uuid,
                    Message      = e.Message,
                    Severity     = e.Severity,
                    Source       = e.Source,
                    ExceptionType = e.ExceptionType,
                    Path         = e.Path,
                    Method       = e.Method,
                    IpAddress    = e.IpAddress,
                    MachineName  = e.MachineName,
                    Environment  = e.Environment,
                    IsResolved   = e.IsResolved,
                    Resolution   = e.Resolution,
                    ResolvedAt   = e.ResolvedAt,
                    CreatedAt    = e.CreatedAt,
                    StackTrace   = e.StackTrace,
                })
                .ToListAsync(ct);

            return Ok(ApiResponse<ErrorLogPageDto>.Ok(new ErrorLogPageDto
            {
                Total    = total,
                Page     = page,
                PageSize = pageSize,
                Data     = data,
            }));
        }

        [HttpPost("errors/{id:int}/resolve")]
        [HasPermission("systemworker.manage")]
        public async Task<IActionResult> ResolveError(int id, [FromBody] string resolution, CancellationToken ct)
        {
            var log = await _db.ka_sytemerrorlogs.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (log == null) return NotFound(ApiResponse<bool>.Fail("Error log not found."));
            log.IsResolved = true;
            log.Resolution = resolution;
            log.ResolvedAt = DateTime.UtcNow;
            _db.ka_sytemerrorlogs.Update(log);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, "Marked as resolved."));
        }

        [HttpDelete("errors/{id:int}")]
        [HasPermission("systemworker.manage")]
        public async Task<IActionResult> DeleteError(int id, CancellationToken ct)
        {
            var log = await _db.ka_sytemerrorlogs.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (log == null) return NotFound(ApiResponse<bool>.Fail("Not found."));
            _db.ka_sytemerrorlogs.Remove(log);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, "Deleted."));
        }

        // ── Audit Trail ───────────────────────────────────────────────────────
        [HttpGet("audit")]
        [HasPermission("systemworker.audit")]
        public async Task<IActionResult> GetAuditEntries(
            [FromQuery] string? entity,
            [FromQuery] string? action,
            [FromQuery] string? username,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var query = _db.ka_auditentries.AsQueryable();
            if (!string.IsNullOrEmpty(entity))   query = query.Where(a => a.EntityName == entity);
            if (!string.IsNullOrEmpty(action))   query = query.Where(a => a.ActionType == action);
            if (!string.IsNullOrEmpty(username)) query = query.Where(a => a.Username!.Contains(username));

            var total = await query.CountAsync(ct);
            var data  = await query
                .OrderByDescending(a => a.TimeStamp)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new AuditItemDto
                {
                    Id         = a.Id,
                    EntityName = a.EntityName,
                    ActionType = a.ActionType,
                    Username   = a.Username,
                    TimeStamp  = a.TimeStamp,
                    EntityId   = a.EntityId,
                })
                .ToListAsync(ct);

            var entities = await _db.ka_auditentries
                .Select(a => a.EntityName)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync(ct);

            return Ok(ApiResponse<AuditPageDto>.Ok(new AuditPageDto
            {
                Total    = total,
                Page     = page,
                PageSize = pageSize,
                Data     = data,
                Entities = entities!,
            }));
        }
    }

    // ── Strongly-typed DTOs (local to this controller) ────────────────────────
    public class ErrorLogItemDto
    {
        public int Id { get; set; }
        public string? Uuid { get; set; }
        public string? Message { get; set; }
        public string? Severity { get; set; }
        public string? Source { get; set; }
        public string? ExceptionType { get; set; }
        public string? Path { get; set; }
        public string? Method { get; set; }
        public string? IpAddress { get; set; }
        public string? MachineName { get; set; }
        public string? Environment { get; set; }
        public bool IsResolved { get; set; }
        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StackTrace { get; set; }
    }

    public class ErrorLogPageDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ErrorLogItemDto> Data { get; set; } = new();
    }

    public class AuditItemDto
    {
        public long Id { get; set; }
        public string? EntityName { get; set; }
        public string? ActionType { get; set; }
        public string? Username { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? EntityId { get; set; }
        public Dictionary<string, object>? Changes { get; set; }
    }

    public class AuditPageDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<AuditItemDto> Data { get; set; } = new();
        public List<string> Entities { get; set; } = new();
    }
}
