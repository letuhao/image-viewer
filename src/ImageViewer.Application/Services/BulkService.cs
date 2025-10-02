using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for bulk operations
/// </summary>
public class BulkService : IBulkService
{
    private readonly ICollectionService _collectionService;
    private readonly ILogger<BulkService> _logger;

    public BulkService(ICollectionService collectionService, ILogger<BulkService> logger)
    {
        _collectionService = collectionService;
        _logger = logger;
    }

    public async Task<BulkOperationResult> BulkAddCollectionsAsync(BulkAddCollectionsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting bulk add collections from parent path {ParentPath}", request.ParentPath);
            _logger.LogInformation("Request parameters - OverwriteExisting: {OverwriteExisting}, AutoScan: {AutoScan}, EnableCache: {EnableCache}", 
                request.OverwriteExisting, request.AutoScan, request.EnableCache);
            
            if (string.IsNullOrEmpty(request.ParentPath))
            {
                throw new ArgumentException("Parent path is required", nameof(request.ParentPath));
            }
            
            // Safety check for dangerous paths
            ValidateParentPath(request.ParentPath);
            
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
                    
                    var result = await ProcessPotentialCollection(potential, request, cancellationToken);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing collection {Name} at {Path}", potential.Name, potential.Path);
                    
                    results.Add(new BulkCollectionResult
                    {
                        Name = potential.Name,
                        Path = potential.Path,
                        Type = potential.Type,
                        Status = "Error",
                        Message = ex.Message,
                        CollectionId = null
                    });
                    
                    errors.Add($"Failed to process collection '{potential.Name}': {ex.Message}");
                }
            }
            
            return CreateOperationResult(results, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk add collections");
            throw;
        }
    }

    private async Task<BulkCollectionResult> ProcessPotentialCollection(
        PotentialCollection potential, 
        BulkAddCollectionsRequest request, 
        CancellationToken cancellationToken)
    {
        // Normalize path for comparison
        var normalizedPath = Path.GetFullPath(potential.Path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var existingCollection = await _collectionService.GetByPathAsync(normalizedPath);
        
        Collection collection;
        bool wasOverwritten = false;
        
        if (existingCollection != null)
        {
            _logger.LogInformation("Found existing collection {Name} with OverwriteExisting={OverwriteExisting}", 
                potential.Name, request.OverwriteExisting);
            
            if (request.OverwriteExisting)
            {
                _logger.LogInformation("Overwriting existing collection {Name} at {Path}", potential.Name, potential.Path);
                
                // Update existing collection
                var settings = CreateCollectionSettings(request);
                collection = await _collectionService.UpdateAsync(
                    existingCollection.Id,
                    potential.Name,
                    normalizedPath,
                    settings);
                
                wasOverwritten = true;
                _logger.LogInformation("Successfully updated existing collection {Name} with ID {CollectionId}", 
                    potential.Name, collection.Id);
            }
            else
            {
                _logger.LogInformation("Skipping existing collection {Name} - OverwriteExisting is false", potential.Name);
                // Skip existing collections if overwrite is disabled
                return new BulkCollectionResult
                {
                    Name = potential.Name,
                    Path = potential.Path,
                    Type = potential.Type,
                    Status = "Skipped",
                    Message = "Collection already exists - use OverwriteExisting=true to update",
                    CollectionId = existingCollection.Id
                };
            }
        }
        else
        {
            // Create new collection
            var settings = CreateCollectionSettings(request);
            collection = await _collectionService.CreateAsync(
                potential.Name,
                normalizedPath,
                potential.Type,
                settings);
            
            _logger.LogInformation("Successfully created new collection {Name} with ID {CollectionId}", 
                potential.Name, collection.Id);
        }
        
        // Note: Scan logic is now handled inside CollectionService.CreateAsync/UpdateAsync methods
        // based on the AutoGenerateCache setting in the collection settings
        
        var actionText = wasOverwritten ? "updated" : "created";
        var scanText = (request.AutoScan ?? true) ? " and scanned" : "";
        
        return new BulkCollectionResult
        {
            Name = potential.Name,
            Path = potential.Path,
            Type = potential.Type,
            Status = "Success",
            Message = $"Collection {actionText}{scanText} successfully",
            CollectionId = collection.Id
        };
    }

    private static CollectionSettings CreateCollectionSettings(BulkAddCollectionsRequest request)
    {
        var settings = new CollectionSettings();
        settings.UpdateThumbnailSize(request.ThumbnailWidth ?? 300, request.ThumbnailHeight ?? 300);
        settings.UpdateCacheSize(request.CacheWidth ?? 1920, request.CacheHeight ?? 1080);
        settings.SetAutoGenerateThumbnails(request.EnableCache ?? true);
        // Map AutoScan to AutoGenerateCache - when AutoScan is true, enable auto cache generation
        settings.SetAutoGenerateCache(request.AutoScan ?? true);
        return settings;
    }

    private static BulkOperationResult CreateOperationResult(List<BulkCollectionResult> results, List<string> errors)
    {
        var successCount = results.Count(r => r.Status == "Success");
        var skippedCount = results.Count(r => r.Status == "Skipped");
        var errorCount = results.Count(r => r.Status == "Error");
        var scannedCount = results.Count(r => r.Status == "Success" && r.Message.Contains("scanned"));
        var updatedCount = results.Count(r => r.Status == "Success" && r.Message.Contains("updated"));
        var createdCount = results.Count(r => r.Status == "Success" && r.Message.Contains("created"));
        
        return new BulkOperationResult
        {
            TotalProcessed = results.Count,
            SuccessCount = successCount,
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            SkippedCount = skippedCount,
            ErrorCount = errorCount,
            ScannedCount = scannedCount,
            Results = results,
            Errors = errors
        };
    }

    private static void ValidateParentPath(string parentPath)
    {
        var dangerousSystemPaths = new[]
        {
            "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)", 
            "C:\\ProgramData", "C:\\System Volume Information", "C:\\$Recycle.Bin"
        };
        
        var isDangerousParent = dangerousSystemPaths.Any(dangerous => 
            parentPath.StartsWith(dangerous, StringComparison.OrdinalIgnoreCase));
        
        if (isDangerousParent)
        {
            throw new ArgumentException(
                "Cannot scan system directories. Please choose a user directory or create a dedicated folder for your collections.", 
                nameof(parentPath));
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
            _logger.LogInformation("Found {FileCount} files in {ParentPath}", files.Length, parentPath);
            
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file).ToLowerInvariant();
                
                // Check if it's a supported compressed file
                if (compressedExtensions.Contains(extension))
                {
                    _logger.LogDebug("Processing compressed file: {FileName}", fileName);
                    
                    // Apply prefix filter if specified - check if filename contains the prefix
                    if (!string.IsNullOrEmpty(collectionPrefix) && 
                        !fileName.Contains(collectionPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Skipping file {FileName} - does not contain prefix {Prefix}", fileName, collectionPrefix);
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
                        
                        _logger.LogInformation("Adding compressed file as potential collection: {FileName}", fileName);
                        potentialCollections.Add(new PotentialCollection
                        {
                            Name = Path.GetFileNameWithoutExtension(fileName),
                            Path = file,
                            Type = collectionType
                        });
                    }
                    else
                    {
                        _logger.LogDebug("Skipping file {FileName} - no images found", fileName);
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
                
                // Check if any entry has an image extension
                var hasImages = archive.Entries.Any(entry => 
                    imageExtensions.Contains(Path.GetExtension(entry.Name).ToLowerInvariant()));
                
                _logger.LogDebug("ZIP file {FilePath} has images: {HasImages}", filePath, hasImages);
                return Task.FromResult(hasImages);
            }
            
            // For other compressed formats, assume they contain images if they exist
            // This is a simplified approach - in production, you'd want proper archive libraries
            var exists = System.IO.File.Exists(filePath);
            _logger.LogDebug("Compressed file {FilePath} exists: {Exists}", filePath, exists);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking images in compressed file {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }
}
