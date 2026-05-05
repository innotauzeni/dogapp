using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Authorization;
using Dog.Application.Common;
using Dog.Core.Entities;
using Dog.Infrastructure.Persistence;

namespace Dog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemActionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public SystemActionsController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        [HasPermission("systemactions.view")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var data = await _db.ka_systemactions
                .IgnoreQueryFilters()
                .OrderBy(a => a.Controller).ThenBy(a => a.Action)
                .Select(a => new { a.Id, a.Name, a.Controller, a.Action })
                .ToListAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpPost]
        [HasPermission("systemactions.create")]
        public async Task<IActionResult> Create([FromBody] SystemActionDto dto, CancellationToken ct)
        {
            var entity = new SystemAction { Name = dto.Name, Controller = dto.Controller, Action = dto.Action };
            _db.ka_systemactions.Add(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<object>.Ok(new { entity.Id, entity.Name, entity.Controller, entity.Action }, "Created."));
        }

        [HttpPut("{id:int}")]
        [HasPermission("systemactions.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] SystemActionDto dto, CancellationToken ct)
        {
            var entity = await _db.ka_systemactions.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (entity == null) return NotFound(ApiResponse<bool>.Fail("Not found."));
            entity.Name = dto.Name; entity.Controller = dto.Controller; entity.Action = dto.Action;
            _db.ka_systemactions.Update(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<object>.Ok(new { entity.Id, entity.Name, entity.Controller, entity.Action }, "Updated."));
        }

        [HttpDelete("{id:int}")]
        [HasPermission("systemactions.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var entity = await _db.ka_systemactions.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (entity == null) return NotFound(ApiResponse<bool>.Fail("Not found."));
            _db.ka_systemactions.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, "Deleted."));
        }

        [HttpGet("role-actions")]
        [HasPermission("systemactions.view")]
        public async Task<IActionResult> GetRoleActions(CancellationToken ct)
        {
            var data = await _db.ka_systemroleactions
                .IgnoreQueryFilters()
                .Include(r => r.Role).Include(r => r.SystemAction)
                .OrderBy(r => r.Role!.Name)
                .Select(r => new {
                    r.Id, RoleId = r.RoleId, RoleName = r.Role!.Name,
                    r.SystemActionId, ActionName = r.SystemAction!.Name,
                    Controller = r.SystemAction.Controller, Action = r.SystemAction.Action
                })
                .ToListAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpPost("role-actions")]
        [HasPermission("systemactions.assignrole")]
        public async Task<IActionResult> AssignRoleAction([FromBody] SystemRoleActionDto dto, CancellationToken ct)
        {
            var exists = await _db.ka_systemroleactions.IgnoreQueryFilters()
                .AnyAsync(r => r.RoleId == dto.RoleId && r.SystemActionId == dto.SystemActionId, ct);
            if (exists) return Ok(ApiResponse<bool>.Ok(true, "Already assigned."));
            _db.ka_systemroleactions.Add(new SystemRoleAction { RoleId = dto.RoleId, SystemActionId = dto.SystemActionId });
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, "Assigned."));
        }

        [HttpDelete("role-actions/{id:int}")]
        [HasPermission("systemactions.assignrole")]
        public async Task<IActionResult> RemoveRoleAction(int id, CancellationToken ct)
        {
            var entity = await _db.ka_systemroleactions.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity == null) return NotFound(ApiResponse<bool>.Fail("Not found."));
            _db.ka_systemroleactions.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<bool>.Ok(true, "Removed."));
        }
    }

    public class SystemActionDto
    {
        public string? Name { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
    }

    public class SystemRoleActionDto
    {
        public string RoleId { get; set; } = string.Empty;
        public int SystemActionId { get; set; }
    }
}
