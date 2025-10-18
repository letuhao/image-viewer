using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for system settings operations
/// 中文：系统设置控制器
/// Tiếng Việt: Bộ điều khiển cài đặt hệ thống
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// [Authorize(Roles = "Admin")] // Uncomment when auth is fully tested
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _systemSettingService;
    private readonly IMacOSXCleanupService _macOSXCleanupService;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(
        ISystemSettingService systemSettingService,
        IMacOSXCleanupService macOSXCleanupService,
        ILogger<SystemSettingsController> logger)
    {
        _systemSettingService = systemSettingService ?? throw new ArgumentNullException(nameof(systemSettingService));
        _macOSXCleanupService = macOSXCleanupService ?? throw new ArgumentNullException(nameof(macOSXCleanupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all system settings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSettings()
    {
        try
        {
            var settings = await _systemSettingService.GetAllSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all system settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get settings by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetSettingsByCategory(string category)
    {
        try
        {
            var settings = await _systemSettingService.GetSettingsByCategoryAsync(category);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings for category {Category}", category);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get single setting by key
    /// </summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        try
        {
            var setting = await _systemSettingService.GetSettingAsync(key);
            if (setting == null)
            {
                return NotFound(new { message = $"Setting with key '{key}' not found" });
            }
            return Ok(setting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get setting with key {Key}", key);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update setting value by key
    /// </summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            ObjectId? modifiedBy = null;
            // TODO: Get from authenticated user context
            // var userId = User.FindFirst("sub")?.Value;
            // if (!string.IsNullOrEmpty(userId) && ObjectId.TryParse(userId, out var userObjectId))
            //     modifiedBy = userObjectId;

            var setting = await _systemSettingService.UpdateSettingAsync(key, request.Value, modifiedBy);
            return Ok(setting);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update setting with key {Key}", key);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create new setting
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSetting([FromBody] CreateSettingRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var setting = await _systemSettingService.CreateSettingAsync(
                request.Key,
                request.Value,
                request.Type ?? "String",
                request.Category ?? "General",
                request.Description);

            return CreatedAtAction(nameof(GetSetting), new { key = setting.SettingKey }, setting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create setting");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete setting
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        try
        {
            await _systemSettingService.DeleteSettingAsync(key);
            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete setting with key {Key}", key);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Batch update settings
    /// </summary>
    [HttpPut("batch")]
    public async Task<IActionResult> BatchUpdateSettings([FromBody] BatchUpdateSettingsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var results = new List<object>();

            foreach (var item in request.Settings)
            {
                try
                {
                    var setting = await _systemSettingService.UpdateSettingAsync(item.Key, item.Value);
                    results.Add(new { key = item.Key, success = true, setting });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update setting {Key} in batch", item.Key);
                    results.Add(new { key = item.Key, success = false, error = ex.Message });
                }
            }

            return Ok(new { results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch update settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Preview __MACOSX cleanup - shows what would be cleaned up without making changes
    /// </summary>
    [HttpGet("cleanup/macosx/preview")]
    public async Task<IActionResult> PreviewMacOSXCleanup()
    {
        try
        {
            _logger.LogInformation("🔍 Admin requested __MACOSX cleanup preview");
            
            var preview = await _macOSXCleanupService.PreviewMacOSXCleanupAsync();
            
            _logger.LogInformation("🔍 __MACOSX cleanup preview completed: {Collections} collections would be affected", 
                preview.AffectedCollections);
            
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to preview __MACOSX cleanup");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Clean up __MACOSX metadata files from all collections
    /// </summary>
    [HttpPost("cleanup/macosx")]
    public async Task<IActionResult> CleanupMacOSXFiles()
    {
        try
        {
            _logger.LogInformation("🧹 Admin requested __MACOSX cleanup");
            
            var result = await _macOSXCleanupService.CleanupMacOSXFilesAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("✅ __MACOSX cleanup completed successfully: {Collections} collections affected, {Images} images removed, {Space} bytes freed", 
                    result.AffectedCollections, result.TotalImagesRemoved, result.TotalSpaceFreed);
            }
            else
            {
                _logger.LogWarning("⚠️ __MACOSX cleanup completed with errors: {ErrorCount} errors", result.Errors.Count);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to cleanup __MACOSX files");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for updating a setting
/// </summary>
public class UpdateSettingRequest
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a setting
/// </summary>
public class CreateSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request model for batch updating settings
/// </summary>
public class BatchUpdateSettingsRequest
{
    public List<SettingUpdate> Settings { get; set; } = new();
}

/// <summary>
/// Single setting update in batch request
/// </summary>
public class SettingUpdate
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

