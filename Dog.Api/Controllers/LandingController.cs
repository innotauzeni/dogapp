using Dog.Application.Common;
using Dog.Core.Entities;
using Dog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LandingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private const string SettingKey = "Landing:Json";

    public LandingController(ApplicationDbContext context) => _context = context;

    /// <summary>Get landing page content from DB or empty string if not set.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        try
        {
            var setting = await _context.ka_appsettings
                .IgnoreQueryFilters()
                .Where(x => !x.IsDeleted && x.SettingKey == SettingKey)
                .FirstOrDefaultAsync();

            var json = setting?.SettingValue ?? "";
            return Ok(new ApiResponse<string> { Success = true, Data = json });
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<string> { Success = false, Message = ex.Message });
        }
    }

    /// <summary>Save landing page content JSON. Admin only.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Save([FromBody] SavePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.Json))
            return Ok(new ApiResponse<bool> { Success = false, Message = "No content provided." });

        try
        {
            var setting = await _context.ka_appsettings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.SettingKey == SettingKey);

            if (setting == null)
            {
                setting = new KaAppSetting
                {
                    SettingKey = SettingKey,
                    SettingValue = payload.Json
                };
                _context.ka_appsettings.Add(setting);
            }
            else
            {
                setting.SettingValue = payload.Json;
                _context.ka_appsettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Message = "Landing page saved.", Data = true });
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    public class SavePayload { public string? Json { get; set; } }
}
