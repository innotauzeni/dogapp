using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Authorization;
using Dog.Application.Common;
using Dog.Infrastructure.Persistence;

namespace Dog.Api.Controllers
{
    // Both SystemErrorLog and AuditEntry are global tables with no CompanyId — a company's
    // Admin must never see them, even if the systemworker.* permission were ever mistakenly
    // granted to their role via Roles & Permissions. This is a hard role check on top of (not
    // instead of) the per-action [HasPermission] checks below — ASP.NET Core combines multiple
    // [Authorize]-derived attributes with AND, so both must pass.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SystemOwner")]
    public class SystemLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public SystemLogsController(ApplicationDbContext db) => _db = db;

        [HttpGet("errors")]
        [HasPermission("systemworker.view")]
        public async Task<IActionResult> GetErrors(
            [FromQuery] string? severity,
            [FromQuery] bool? resolved,
            [FromQuery] bool deleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            // Deletes on AuditableBaseEntity are soft (see ApplicationDbContext.SaveChangesAsync),
            // so this must respect the default IsDeleted query filter — IgnoreQueryFilters() here
            // used to make "Delete"/"Delete All" look like they did nothing, since the rows were
            // only ever marked IsDeleted=1, never actually removed, yet still showed up in this list.
            // `deleted=true` explicitly asks for the trash view instead (opts back into
            // IgnoreQueryFilters so soft-deleted rows become visible for restore/permanent-delete).
            var query = deleted
                ? _db.ka_sytemerrorlogs.IgnoreQueryFilters().Where(e => e.IsDeleted)
                : _db.ka_sytemerrorlogs.AsQueryable();
            if (!string.IsNullOrEmpty(severity)) query = query.Where(e => e.Severity == severity);
            if (resolved.HasValue)               query = query.Where(e => e.IsResolved == resolved.Value);

            var total = await query.CountAsync(ct);
            var totalDeleted = await _db.ka_sytemerrorlogs.IgnoreQueryFilters().CountAsync(e => e.IsDeleted, ct);
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
                    IsDeleted    = e.IsDeleted,
                    DeletedAt    = e.DeletedAt,
                })
                .ToListAsync(ct);

            return Ok(ApiResponse<ErrorLogPageDto>.Ok(new ErrorLogPageDto
            {
                Total        = total,
                TotalDeleted = totalDeleted,
                Page         = page,
                PageSize     = pageSize,
                Data         = data,
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

        [HttpPost("errors/resolve-all")]
        [HasPermission("systemworker.manage")]
        public async Task<IActionResult> ResolveAllErrors([FromQuery] string? severity, CancellationToken ct)
        {
            var query = _db.ka_sytemerrorlogs.Where(e => !e.IsResolved).AsQueryable();
            if (!string.IsNullOrEmpty(severity)) query = query.Where(e => e.Severity == severity);

            var logs = await query.ToListAsync(ct);
            foreach (var log in logs)
            {
                log.IsResolved = true;
                log.Resolution = "Bulk resolved";
                log.ResolvedAt = DateTime.UtcNow;
            }
            _db.ka_sytemerrorlogs.UpdateRange(logs);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, $"{logs.Count} error(s) marked as resolved."));
        }

        [HttpDelete("errors/all")]
        [HasPermission("systemworker.manage")]
        public async Task<IActionResult> DeleteAllErrors([FromQuery] string? severity, [FromQuery] bool? resolved, CancellationToken ct)
        {
            var query = _db.ka_sytemerrorlogs.AsQueryable();
            if (!string.IsNullOrEmpty(severity)) query = query.Where(e => e.Severity == severity);
            if (resolved.HasValue) query = query.Where(e => e.IsResolved == resolved.Value);

            var logs = await query.ToListAsync(ct);
            _db.ka_sytemerrorlogs.RemoveRange(logs);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, $"{logs.Count} log(s) deleted."));
        }

        // ── Trash (soft-deleted logs): restore or permanently remove ───────────

        [HttpPost("errors/{id:int}/restore")]
        [HasPermission("systemworker.manage")]
        public async Task<IActionResult> RestoreError(int id, CancellationToken ct)
        {
            var log = await _db.ka_sytemerrorlogs.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (log == null) return NotFound(ApiResponse<bool>.Fail("Not found."));
            log.IsDeleted = false;
            log.DeletedAt = null;
            log.DeletedBy = null;
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, "Restored."));
        }

        // Bypasses SaveChangesAsync's soft-delete conversion entirely via ExecuteDeleteAsync,
        // which issues a real DELETE statement without going through the change tracker —
        // this is the only way to truly remove an AuditableBaseEntity row.
        [HttpDelete("errors/{id:int}/permanent")]
        [HasPermission("systemworker.manage")]
        public async Task<IActionResult> PermanentlyDeleteError(int id, CancellationToken ct)
        {
            var affected = await _db.ka_sytemerrorlogs.IgnoreQueryFilters()
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync(ct);
            if (affected == 0) return NotFound(ApiResponse<bool>.Fail("Not found."));
            return Ok(ApiResponse<bool>.Ok(true, "Permanently deleted."));
        }

        [HttpDelete("errors/trash")]
        [HasPermission("systemworker.manage")]
        public async Task<IActionResult> EmptyTrash(CancellationToken ct)
        {
            var affected = await _db.ka_sytemerrorlogs.IgnoreQueryFilters()
                .Where(e => e.IsDeleted)
                .ExecuteDeleteAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, $"{affected} log(s) permanently deleted."));
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

            await AuditEnricher.EnrichAsync(_db, data, ct);

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

        // GET /api/SystemLogs/audit/{id} — full detail for one entry, including the
        // field-level Changes diff (deliberately left out of the list response above,
        // since it can be a large JSON blob per row and the list can return up to 100+ rows).
        [HttpGet("audit/{id:long}")]
        [HasPermission("systemworker.audit")]
        public async Task<IActionResult> GetAuditEntryById(long id, CancellationToken ct)
        {
            var a = await _db.ka_auditentries.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (a == null) return NotFound(ApiResponse<AuditItemDto>.Fail("Audit entry not found."));

            var dto = new AuditItemDto
            {
                Id         = a.Id,
                EntityName = a.EntityName,
                ActionType = a.ActionType,
                Username   = a.Username,
                TimeStamp  = a.TimeStamp,
                EntityId   = a.EntityId,
                Changes    = a.Changes,
            };
            await AuditEnricher.EnrichAsync(_db, new[] { dto }, ct);

            return Ok(ApiResponse<AuditItemDto>.Ok(dto));
        }

        // AuditEntry.Username is actually the actor's Id (see GetCurrentUserId in
        // ApplicationDbContext — it stores the NameIdentifier claim, not a display name),
        // so every audit row showed a raw GUID instead of a readable name. This resolves
        // the distinct set of ids in a page against AspNetUsers in one query and swaps in
        // "First Last" where a match exists, leaving ids that no longer resolve (e.g. a
        // deleted user) or the literal "System" untouched.
        private async Task ResolveUsernamesAsync(IReadOnlyCollection<AuditItemDto> items, CancellationToken ct)
        {
            var ids = items
                .Select(i => i.Username)
                .Where(u => !string.IsNullOrEmpty(u) && u != "System")
                .Distinct()
                .ToList();
            if (ids.Count == 0) return;

            var names = await _db.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, Display = ((u.Name ?? "") + " " + (u.Surname ?? "")).Trim() })
                .ToDictionaryAsync(x => x.Id, x => x.Display, ct);

            foreach (var item in items)
            {
                if (item.Username != null && names.TryGetValue(item.Username, out var name) && !string.IsNullOrWhiteSpace(name))
                    item.Username = name;
            }
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
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class ErrorLogPageDto
    {
        public int Total { get; set; }
        public int TotalDeleted { get; set; }
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
        /// <summary>The record as it is AFTER the operation (for a delete: the record that was deleted).</summary>
        public Dictionary<string, object>? Changes { get; set; }
        /// <summary>For an update: the record exactly as it was BEFORE the change. Null for older entries / inserts.</summary>
        public Dictionary<string, object>? Previous { get; set; }
        /// <summary>Who performed the operation, and from where.</summary>
        public AuditActorDto? Actor { get; set; }
    }

    public class AuditActorDto
    {
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? CompanyName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
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
