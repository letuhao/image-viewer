using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using System.IO;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for cache folder management
/// 中文：缓存文件夹控制器
/// Tiếng Việt: Bộ điều khiển thư mục bộ nhớ cache
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// [Authorize(Roles = "Admin")] // Uncomment when auth is fully tested
public class CacheFoldersController : ControllerBase
{
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ILogger<CacheFoldersController> _logger;

    public CacheFoldersController(
        ICacheFolderRepository cacheFolderRepository,
        ILogger<CacheFoldersController> logger)
    {
        _cacheFolderRepository = cacheFolderRepository ?? throw new ArgumentNullException(nameof(cacheFolderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all cache folders with disk health information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllCacheFolders()
    {
        try
        {
            var folders = await _cacheFolderRepository.GetAllAsync();
            var foldersList = folders.ToList();

            var result = foldersList.Select(f => {
                var diskInfo = GetDiskInfo(f.Path);
                
                return new
                {
                    id = f.Id.ToString(),
                    name = f.Name,
                    path = f.Path,
                    priority = f.Priority,
                    maxSizeBytes = f.MaxSizeBytes,
                    currentSize = f.CurrentSize,
                    fileCount = 0, // Will be calculated later if needed
                    isActive = f.IsActive,
                    createdAt = f.CreatedAt,
                    updatedAt = f.UpdatedAt,
                    diskInfo = diskInfo
                };
            }).ToList();

            return Ok(new
            {
                folders = result,
                summary = new
                {
                    totalFolders = foldersList.Count,
                    totalSize = foldersList.Sum(f => f.CurrentSize),
                    totalFiles = 0, // Calculate from collections if needed
                    avgPriority = foldersList.Any() ? foldersList.Average(f => f.Priority) : 0
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache folders");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get disk information for a given path
    /// </summary>
    private object GetDiskInfo(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                return new { available = false };
            }

            // Get drive info
            var driveInfo = new System.IO.DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
            
            return new
            {
                available = driveInfo.IsReady,
                totalSpace = driveInfo.TotalSize,
                freeSpace = driveInfo.AvailableFreeSpace,
                usedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                usedPercentage = ((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize * 100),
                driveFormat = driveInfo.DriveFormat,
                driveType = driveInfo.DriveType.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get disk info for path: {Path}", path);
            return new { available = false, error = ex.Message };
        }
    }

    /// <summary>
    /// Validate if a path is writable
    /// </summary>
    [HttpPost("validate-path")]
    public IActionResult ValidatePath([FromBody] ValidatePathRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new { valid = false, error = "Path cannot be empty" });
            }

            // Check if directory exists or can be created
            var directoryExists = Directory.Exists(request.Path);
            var canWrite = false;

            if (!directoryExists)
            {
                try
                {
                    Directory.CreateDirectory(request.Path);
                    canWrite = true;
                    Directory.Delete(request.Path); // Clean up test directory
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot create directory: {Path}", request.Path);
                    return Ok(new { valid = false, error = $"Cannot create directory: {ex.Message}" });
                }
            }
            else
            {
                // Test write permission
                try
                {
                    var testFile = Path.Combine(request.Path, $"_test_{Guid.NewGuid()}.tmp");
                    System.IO.File.WriteAllText(testFile, "test");
                    System.IO.File.Delete(testFile);
                    canWrite = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot write to directory: {Path}", request.Path);
                    return Ok(new { valid = false, error = $"Cannot write to directory: {ex.Message}" });
                }
            }

            var diskInfo = GetDiskInfo(request.Path);
            
            return Ok(new
            {
                valid = true,
                exists = directoryExists,
                writable = canWrite,
                diskInfo = diskInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate path: {Path}", request.Path);
            return Ok(new { valid = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create new cache folder
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCacheFolder([FromBody] CreateCacheFolderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var cacheFolder = new CacheFolder(
                request.Name,
                request.Path,
                request.MaxSizeBytes ?? 0, // 0 means unlimited
                request.Priority
            );

            await _cacheFolderRepository.CreateAsync(cacheFolder);

            _logger.LogInformation("Created cache folder: {Name} at {Path}", request.Name, request.Path);

            return CreatedAtAction(nameof(GetAllCacheFolders), new { id = cacheFolder.Id }, new
            {
                id = cacheFolder.Id.ToString(),
                name = cacheFolder.Name,
                path = cacheFolder.Path,
                priority = cacheFolder.Priority,
                maxSizeBytes = cacheFolder.MaxSizeBytes,
                currentSize = cacheFolder.CurrentSize,
                isActive = cacheFolder.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cache folder");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update cache folder
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCacheFolder(string id, [FromBody] UpdateCacheFolderRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var folderId))
                return BadRequest(new { message = "Invalid folder ID" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var folder = await _cacheFolderRepository.GetByIdAsync(folderId);
            if (folder == null)
                return NotFound(new { message = "Cache folder not found" });

            if (request.Name != null)
                folder.UpdateName(request.Name);
            
            if (request.Path != null)
                folder.UpdatePath(request.Path);
            
            if (request.Priority.HasValue)
                folder.UpdatePriority(request.Priority.Value);
            
            if (request.MaxSizeBytes.HasValue)
                folder.UpdateMaxSize(request.MaxSizeBytes.Value);

            await _cacheFolderRepository.UpdateAsync(folder);

            _logger.LogInformation("Updated cache folder: {Name}", folder.Name);

            return Ok(new
            {
                id = folder.Id.ToString(),
                name = folder.Name,
                path = folder.Path,
                priority = folder.Priority,
                maxSizeBytes = folder.MaxSizeBytes,
                currentSize = folder.CurrentSize,
                isActive = folder.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cache folder");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete cache folder
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCacheFolder(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var folderId))
                return BadRequest(new { message = "Invalid folder ID" });

            var folder = await _cacheFolderRepository.GetByIdAsync(folderId);
            if (folder == null)
                return NotFound(new { message = "Cache folder not found" });

            await _cacheFolderRepository.DeleteAsync(folderId);

            _logger.LogInformation("Deleted cache folder: {Name}", folder.Name);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cache folder");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for validating a path
/// </summary>
public class ValidatePathRequest
{
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a cache folder
/// </summary>
public class CreateCacheFolderRequest
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public long? MaxSizeBytes { get; set; }
}

/// <summary>
/// Request model for updating a cache folder
/// </summary>
public class UpdateCacheFolderRequest
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public int? Priority { get; set; }
    public long? MaxSizeBytes { get; set; }
}

