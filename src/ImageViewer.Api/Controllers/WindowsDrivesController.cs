using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for Windows drive management and file operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class WindowsDrivesController : ControllerBase
{
    private readonly IWindowsDriveService _windowsDriveService;
    private readonly ILogger<WindowsDrivesController> _logger;

    public WindowsDrivesController(IWindowsDriveService windowsDriveService, ILogger<WindowsDrivesController> logger)
    {
        _windowsDriveService = windowsDriveService ?? throw new ArgumentNullException(nameof(windowsDriveService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all available Windows drives
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAvailableDrives()
    {
        try
        {
            var drives = await _windowsDriveService.GetAvailableDrivesAsync();
            return Ok(drives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available drives");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get information about a specific drive
    /// </summary>
    [HttpGet("{driveLetter}")]
    public async Task<IActionResult> GetDriveInfo(string driveLetter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            var driveInfo = await _windowsDriveService.GetDriveInfoAsync(driveLetter.ToUpper());
            
            if (driveInfo == null)
                return NotFound(new { message = $"Drive {driveLetter} is not accessible" });

            return Ok(driveInfo);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get drive info for {DriveLetter}", driveLetter);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if a drive is accessible
    /// </summary>
    [HttpGet("{driveLetter}/accessible")]
    public async Task<IActionResult> IsDriveAccessible(string driveLetter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            var isAccessible = await _windowsDriveService.IsDriveAccessibleAsync(driveLetter.ToUpper());
            return Ok(new { driveLetter = driveLetter.ToUpper(), accessible = isAccessible });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check drive accessibility for {DriveLetter}", driveLetter);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Scan drive for media files
    /// </summary>
    [HttpPost("{driveLetter}/scan")]
    public async Task<IActionResult> ScanDriveForMedia(string driveLetter, [FromBody] ScanDriveRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            var mediaFiles = await _windowsDriveService.ScanDriveForMediaAsync(driveLetter.ToUpper(), request?.Extensions);
            return Ok(new { driveLetter = driveLetter.ToUpper(), mediaFiles = mediaFiles, count = mediaFiles.Count() });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan drive {DriveLetter} for media files", driveLetter);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get directory structure of a drive
    /// </summary>
    [HttpGet("{driveLetter}/directories")]
    public async Task<IActionResult> GetDirectoryStructure(string driveLetter, [FromQuery] string path = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            var directories = await _windowsDriveService.GetDirectoryStructureAsync(driveLetter.ToUpper(), path);
            return Ok(new { driveLetter = driveLetter.ToUpper(), path = path, directories = directories });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get directory structure for {DriveLetter}:{Path}", driveLetter, path);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create library from drive
    /// </summary>
    [HttpPost("{driveLetter}/library")]
    public async Task<IActionResult> CreateLibraryFromDrive(string driveLetter, [FromBody] CreateLibraryFromDriveRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            if (request == null || string.IsNullOrWhiteSpace(request.LibraryName))
                return BadRequest(new { message = "Library name is required" });

            var libraryId = await _windowsDriveService.CreateLibraryFromDriveAsync(
                driveLetter.ToUpper(), 
                request.LibraryName, 
                request.Description);

            return CreatedAtAction(nameof(GetDriveInfo), new { driveLetter = driveLetter.ToUpper() }, 
                new { libraryId = libraryId, message = "Library created successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create library from drive {DriveLetter}", driveLetter);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Start monitoring drive for changes
    /// </summary>
    [HttpPost("{driveLetter}/monitor/start")]
    public async Task<IActionResult> StartDriveMonitoring(string driveLetter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            await _windowsDriveService.StartDriveMonitoringAsync(driveLetter.ToUpper());
            return Ok(new { message = $"Started monitoring drive {driveLetter.ToUpper()}" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring drive {DriveLetter}", driveLetter);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Stop monitoring drive for changes
    /// </summary>
    [HttpPost("{driveLetter}/monitor/stop")]
    public async Task<IActionResult> StopDriveMonitoring(string driveLetter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            await _windowsDriveService.StopDriveMonitoringAsync(driveLetter.ToUpper());
            return Ok(new { message = $"Stopped monitoring drive {driveLetter.ToUpper()}" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop monitoring drive {DriveLetter}", driveLetter);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get drive statistics
    /// </summary>
    [HttpGet("{driveLetter}/statistics")]
    public async Task<IActionResult> GetDriveStatistics(string driveLetter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
                return BadRequest(new { message = "Drive letter cannot be null or empty" });

            var statistics = await _windowsDriveService.GetDriveStatisticsAsync(driveLetter.ToUpper());
            return Ok(statistics);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for drive {DriveLetter}", driveLetter);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for scanning drive
/// </summary>
public class ScanDriveRequest
{
    public string[]? Extensions { get; set; }
}

/// <summary>
/// Request model for creating library from drive
/// </summary>
public class CreateLibraryFromDriveRequest
{
    public string LibraryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
