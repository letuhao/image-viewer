using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Bulk operations controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BulkController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ILogger<BulkController> _logger;

    public BulkController(ICollectionService collectionService, ILogger<BulkController> logger)
    {
        _collectionService = collectionService;
        _logger = logger;
    }

    /// <summary>
    /// Bulk add collections from parent directory
    /// </summary>
    [HttpPost("collections")]
    public async Task<ActionResult<BulkOperationResult>> BulkAddCollections([FromBody] BulkAddCollectionsRequest request)
    {
        try
        {
            _logger.LogInformation("Starting bulk add collections from parent path {ParentPath}", request.ParentPath);
            
            if (string.IsNullOrEmpty(request.ParentPath))
            {
                return BadRequest(new { error = "Parent path is required" });
            }
            
            // Safety check for dangerous paths
            var dangerousSystemPaths = new[]
            {
                "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)", 
                "C:\\ProgramData", "C:\\System Volume Information", "C:\\$Recycle.Bin"
            };
            
            var isDangerousParent = dangerousSystemPaths.Any(dangerous => 
                request.ParentPath.StartsWith(dangerous, StringComparison.OrdinalIgnoreCase));
            
            if (isDangerousParent)
            {
                return BadRequest(new { 
                    error = "Cannot scan system directories. Please choose a user directory or create a dedicated folder for your collections." 
                });
            }
            
            // Find potential collections
            var potentialCollections = await FindPotentialCollections(
                request.ParentPath, 
                request.IncludeSubfolders, 
                request.CollectionPrefix);
            
            _logger.LogInformation("Found {Count} potential collections", potentialCollections.Count);
            
            var results = new List<BulkCollectionResult>();
            var errors = new List<string>();
            
            // Process each potential collection
            foreach (var potential in potentialCollections)
            {
                try
                {
                    _logger.LogDebug("Processing potential collection {Name} at {Path}", potential.Name, potential.Path);
                    
                    // Check if collection already exists - normalize path for comparison
                    var normalizedPath = Path.GetFullPath(potential.Path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var existingCollection = await _collectionService.GetByPathAsync(normalizedPath);
                    if (existingCollection != null)
                    {
                        // Skip existing collections for now to avoid concurrency issues
                        results.Add(new BulkCollectionResult
                        {
                            Name = potential.Name,
                            Path = potential.Path,
                            Type = potential.Type,
                            Status = "Skipped",
                            Message = "Collection already exists - skipping to avoid concurrency issues",
                            CollectionId = existingCollection.Id
                        });
                        continue;
                    }
                    
                    // Create collection settings
                    var settings = new CollectionSettings();
                    settings.UpdateThumbnailSize(request.ThumbnailWidth ?? 300, request.ThumbnailHeight ?? 300);
                    settings.UpdateCacheSize(request.CacheWidth ?? 1920, request.CacheHeight ?? 1080);
                    settings.SetAutoGenerateThumbnails(request.EnableCache ?? true);
                    settings.SetAutoGenerateCache(request.AutoScan ?? true);
                    
                    // Create collection (this calls the same logic as single add)
                    var collection = await _collectionService.CreateAsync(
                        potential.Name,
                        normalizedPath, // Use normalized path for consistency
                        potential.Type,
                        settings);
                    
                    results.Add(new BulkCollectionResult
                    {
                        Name = potential.Name,
                        Path = potential.Path,
                        Type = potential.Type,
                        Status = "Success",
                        Message = "Collection created successfully",
                        CollectionId = collection.Id
                    });
                    
                    _logger.LogInformation("Successfully created collection {Name} with ID {CollectionId}", 
                        potential.Name, collection.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating collection {Name} at {Path}", potential.Name, potential.Path);
                    
                    results.Add(new BulkCollectionResult
                    {
                        Name = potential.Name,
                        Path = potential.Path,
                        Type = potential.Type,
                        Status = "Error",
                        Message = ex.Message,
                        CollectionId = null
                    });
                    
                    errors.Add($"Failed to create collection '{potential.Name}': {ex.Message}");
                }
            }
            
            var successCount = results.Count(r => r.Status == "Success");
            var skippedCount = results.Count(r => r.Status == "Skipped");
            var errorCount = results.Count(r => r.Status == "Error");
            
            _logger.LogInformation("Bulk operation completed. Success: {Success}, Skipped: {Skipped}, Errors: {Errors}", 
                successCount, skippedCount, errorCount);
            
            var response = new BulkOperationResult
            {
                TotalProcessed = results.Count,
                SuccessCount = successCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                Results = results,
                Errors = errors
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk add collections");
            return StatusCode(500, "Internal server error");
        }
    }
    
    private async Task<List<PotentialCollection>> FindPotentialCollections(
        string parentPath, 
        bool includeSubfolders, 
        string collectionPrefix)
    {
        var potentialCollections = new List<PotentialCollection>();
        
        try
        {
            if (!Directory.Exists(parentPath))
            {
                throw new DirectoryNotFoundException($"Parent path does not exist: {parentPath}");
            }
            
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            // Find folders
            var directories = Directory.GetDirectories(parentPath, "*", searchOption);
            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                
                // Skip prefix filtering for now - let the main logic handle existing vs new collections
                // This allows us to process both existing and new collections
                
                // Check if directory contains images
                var hasImages = await HasImageFiles(directory);
                if (hasImages)
                {
                    potentialCollections.Add(new PotentialCollection
                    {
                        Name = directoryName,
                        Path = directory,
                        Type = CollectionType.Folder
                    });
                }
            }
            
            // Find compressed files
            var compressedExtensions = new[] { ".zip", ".cbz", ".cbr", ".7z", ".rar", ".tar", ".tar.gz", ".tar.bz2" };
            var files = Directory.GetFiles(parentPath, "*", searchOption);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file).ToLowerInvariant();
                
                // Check if it's a supported compressed file
                if (compressedExtensions.Contains(extension))
                {
                    // Apply prefix filter if specified
                    if (!string.IsNullOrEmpty(collectionPrefix) && 
                        !fileName.StartsWith(collectionPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    // Check if compressed file contains images
                    var hasImages = await HasImagesInCompressedFile(file);
                    if (hasImages)
                    {
                        var collectionType = extension switch
                        {
                            ".zip" or ".cbz" => CollectionType.Zip,
                            ".7z" => CollectionType.SevenZip,
                            ".rar" or ".cbr" => CollectionType.Rar,
                            ".tar" or ".tar.gz" or ".tar.bz2" => CollectionType.Tar,
                            _ => CollectionType.Zip // Default to Zip
                        };
                        
                        potentialCollections.Add(new PotentialCollection
                        {
                            Name = Path.GetFileNameWithoutExtension(fileName),
                            Path = file,
                            Type = collectionType
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding potential collections in {ParentPath}", parentPath);
            throw;
        }
        
        return potentialCollections;
    }
    
    private Task<bool> HasImageFiles(string directory)
    {
        try
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg" };
            var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
            
            return Task.FromResult(files.Any(file => 
                imageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant())));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> HasImagesInCompressedFile(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // For now, only support ZIP files for bulk operations
            // Other formats require more complex libraries
            if (extension == ".zip")
            {
                using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg" };
                
                return Task.FromResult(archive.Entries.Any(entry => 
                    imageExtensions.Contains(Path.GetExtension(entry.Name).ToLowerInvariant())));
            }
            
            // For other compressed formats, assume they contain images if they exist
            // This is a simplified approach - in production, you'd want proper archive libraries
            return Task.FromResult(System.IO.File.Exists(filePath));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

public class BulkAddCollectionsRequest
{
    public string ParentPath { get; set; } = string.Empty;
    public string CollectionPrefix { get; set; } = string.Empty;
    public bool IncludeSubfolders { get; set; } = false;
    public bool AutoAdd { get; set; } = false;
    public int? ThumbnailWidth { get; set; }
    public int? ThumbnailHeight { get; set; }
    public int? CacheWidth { get; set; }
    public int? CacheHeight { get; set; }
    public bool? EnableCache { get; set; }
    public bool? AutoScan { get; set; }
}

public class BulkOperationResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<BulkCollectionResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class BulkCollectionResult
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
}

public class PotentialCollection
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType Type { get; set; }
}
