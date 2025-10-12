using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Events;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for bulk operations
/// </summary>
public class BulkService : IBulkService
{
    private readonly ICollectionService _collectionService;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<BulkService> _logger;

    public BulkService(
        ICollectionService collectionService,
        IMessageQueueService messageQueueService,
        IBackgroundJobService backgroundJobService,
        ILogger<BulkService> logger)
    {
        _collectionService = collectionService;
        _messageQueueService = messageQueueService;
        _backgroundJobService = backgroundJobService;
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
        
        // Try to get existing collection, but don't throw if not found
        Collection? existingCollection = null;
        try
        {
            existingCollection = await _collectionService.GetCollectionByPathAsync(normalizedPath);
        }
        catch (EntityNotFoundException)
        {
            // Collection doesn't exist, which is fine - we'll create a new one
            existingCollection = null;
        }
        
        Collection collection;
        bool wasOverwritten = false;
        
        if (existingCollection != null)
        {
            _logger.LogInformation("Found existing collection {Name} with OverwriteExisting={OverwriteExisting}, ResumeIncomplete={ResumeIncomplete}", 
                potential.Name, request.OverwriteExisting, request.ResumeIncomplete);
            
            // Check if collection has images (already scanned)
            var hasImages = existingCollection.Images?.Count > 0;
            var imageCount = existingCollection.Images?.Count ?? 0;
            var thumbnailCount = existingCollection.Thumbnails?.Count ?? 0;
            var cacheCount = existingCollection.CacheImages?.Count ?? 0;
            
            // MODE 3: Force Rescan (OverwriteExisting=true)
            if (request.OverwriteExisting)
            {
                _logger.LogInformation("OverwriteExisting=true: Will clear image arrays and rescan collection {Name} from scratch", 
                    potential.Name);
                
                // Update existing collection metadata
                var updateRequest = new UpdateCollectionRequest
                {
                    Name = potential.Name,
                    Path = normalizedPath
                };
                collection = await _collectionService.UpdateCollectionAsync(existingCollection.Id, updateRequest);
                
                // Apply collection settings with force rescan
                var settings = CreateCollectionSettings(request);
                var settingsRequest = new UpdateCollectionSettingsRequest
                {
                    AutoScan = settings.AutoScan,
                    GenerateThumbnails = settings.GenerateThumbnails,
                    GenerateCache = settings.GenerateCache,
                    EnableWatching = settings.EnableWatching,
                    ScanInterval = settings.ScanInterval,
                    MaxFileSize = settings.MaxFileSize,
                    AllowedFormats = settings.AllowedFormats?.ToList(),
                    ExcludedPaths = settings.ExcludedPaths?.ToList()
                };
                collection = await _collectionService.UpdateSettingsAsync(
                    collection.Id, 
                    settingsRequest, 
                    triggerScan: true, 
                    forceRescan: true);
                
                wasOverwritten = true;
                _logger.LogInformation("Successfully updated existing collection {Name} with ID {CollectionId} and queued force rescan", 
                    potential.Name, collection.Id);
            }
            // MODE 2: Resume Incomplete (ResumeIncomplete=true AND hasImages)
            else if (request.ResumeIncomplete && hasImages)
            {
                // Collection has images, check if thumbnail/cache is incomplete
                var missingThumbnails = imageCount - thumbnailCount;
                var missingCache = imageCount - cacheCount;
                
                if (missingThumbnails > 0 || missingCache > 0)
                {
                    _logger.LogInformation("ResumeIncomplete=true: Collection {Name} has {ImageCount} images, {ThumbnailCount} thumbnails, {CacheCount} cache. Missing: {MissingThumbnails} thumbnails, {MissingCache} cache", 
                        potential.Name, imageCount, thumbnailCount, cacheCount, missingThumbnails, missingCache);
                    
                    // Queue ONLY missing thumbnail/cache jobs (no re-scan)
                    await QueueMissingThumbnailCacheJobsAsync(existingCollection, request, cancellationToken);
                    
                    return new BulkCollectionResult
                    {
                        Name = potential.Name,
                        Path = potential.Path,
                        Type = potential.Type,
                        Status = "Resumed",
                        Message = $"Resumed: {missingThumbnails} thumbnails, {missingCache} cache (no re-scan)",
                        CollectionId = existingCollection.Id
                    };
                }
                else
                {
                    // Collection is complete (100%)
                    _logger.LogInformation("ResumeIncomplete=true: Collection {Name} is complete ({ImageCount} images, {ThumbnailCount} thumbnails, {CacheCount} cache), skipping", 
                        potential.Name, imageCount, thumbnailCount, cacheCount);
                    
                    return new BulkCollectionResult
                    {
                        Name = potential.Name,
                        Path = potential.Path,
                        Type = potential.Type,
                        Status = "Skipped",
                        Message = $"Already complete: {imageCount} images, {thumbnailCount} thumbnails, {cacheCount} cache",
                        CollectionId = existingCollection.Id
                    };
                }
            }
            // MODE 1: Skip or Scan
            else if (!hasImages || request.ResumeIncomplete)
            {
                // No images yet OR ResumeIncomplete mode but no images = need to scan
                _logger.LogInformation("Collection {Name} has no images, queuing scan job", potential.Name);
                
                // Update existing collection metadata
                var updateRequest = new UpdateCollectionRequest
                {
                    Name = potential.Name,
                    Path = normalizedPath
                };
                collection = await _collectionService.UpdateCollectionAsync(existingCollection.Id, updateRequest);
                
                // Apply collection settings and queue scan
                var settings = CreateCollectionSettings(request);
                var settingsRequest = new UpdateCollectionSettingsRequest
                {
                    AutoScan = settings.AutoScan,
                    GenerateThumbnails = settings.GenerateThumbnails,
                    GenerateCache = settings.GenerateCache,
                    EnableWatching = settings.EnableWatching,
                    ScanInterval = settings.ScanInterval,
                    MaxFileSize = settings.MaxFileSize,
                    AllowedFormats = settings.AllowedFormats?.ToList(),
                    ExcludedPaths = settings.ExcludedPaths?.ToList()
                };
                collection = await _collectionService.UpdateSettingsAsync(
                    collection.Id, 
                    settingsRequest, 
                    triggerScan: true, 
                    forceRescan: false);
                
                _logger.LogInformation("Successfully updated existing collection {Name} with ID {CollectionId} and queued scan", 
                    potential.Name, collection.Id);
            }
            else
            {
                // MODE 1: Skip (hasImages and not ResumeIncomplete)
                _logger.LogInformation("Collection {Name} already has {ImageCount} images, skipping (ResumeIncomplete=false)", 
                    potential.Name, imageCount);
                
                return new BulkCollectionResult
                {
                    Name = potential.Name,
                    Path = potential.Path,
                    Type = potential.Type,
                    Status = "Skipped",
                    Message = $"Already scanned: {imageCount} images (use ResumeIncomplete=true to resume)",
                    CollectionId = existingCollection.Id
                };
            }
        }
        else
        {
            // Create new collection with creator tracking
            collection = await _collectionService.CreateCollectionAsync(
                request.LibraryId, // LibraryId from request - can be null for standalone collections
                potential.Name,
                normalizedPath,
                potential.Type,
                createdBy: "BulkService",
                createdBySystem: "ImageViewer.Worker");
            
            // Apply collection settings after creation
            var settings = CreateCollectionSettings(request);
            var settingsRequest = new UpdateCollectionSettingsRequest
            {
                AutoScan = settings.AutoScan,
                GenerateThumbnails = settings.GenerateThumbnails,
                GenerateCache = settings.GenerateCache,
                EnableWatching = settings.EnableWatching,
                ScanInterval = settings.ScanInterval,
                MaxFileSize = settings.MaxFileSize,
                AllowedFormats = settings.AllowedFormats?.ToList(),
                ExcludedPaths = settings.ExcludedPaths?.ToList()
            };
            collection = await _collectionService.UpdateSettingsAsync(collection.Id, settingsRequest, triggerScan: false); // Don't trigger scan - already triggered by CreateCollectionAsync
            
            _logger.LogInformation("Successfully created new collection {Name} with ID {CollectionId} and applied settings", 
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
        settings.UpdateThumbnailSize(request.ThumbnailWidth ?? 300);
        settings.UpdateCacheSize(request.CacheWidth ?? 1920);
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
            
            // CRITICAL FIX: Use TopDirectoryOnly to check ONLY direct children
            // We want LEAF folders only (folders that directly contain images)
            // NOT intermediate folders that have subfolders with images
            // 
            // Example structure:
            // L:\EMedia\AI_Generated\         ← NO images directly here → NOT a collection
            //   └── AiASAG\                   ← Images here → IS a collection
            //       └── image1.jpg
            //
            // Before fix: Both AI_Generated AND AiASAG were added as collections (wrong!)
            // After fix: Only AiASAG is added as a collection (correct!)
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

    /// <summary>
    /// Queue thumbnail and cache generation jobs for images that are missing them
    /// This is used for resuming incomplete collections without re-scanning
    /// </summary>
    private async Task QueueMissingThumbnailCacheJobsAsync(
        Collection collection, 
        BulkAddCollectionsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Queuing missing thumbnail/cache jobs for collection {CollectionId} ({Name})", 
                collection.Id, collection.Name);
            
            // Get images that don't have thumbnails
            var imagesNeedingThumbnails = collection.Images?
                .Where(img => !(collection.Thumbnails?.Any(t => t.ImageId == img.Id) ?? false))
                .ToList() ?? new List<ImageEmbedded>();
            
            // Get images that don't have cache
            var imagesNeedingCache = collection.Images?
                .Where(img => !(collection.CacheImages?.Any(c => c.ImageId == img.Id) ?? false))
                .ToList() ?? new List<ImageEmbedded>();
            
            _logger.LogInformation("Collection {Name}: {ThumbnailCount} images need thumbnails, {CacheCount} images need cache", 
                collection.Name, imagesNeedingThumbnails.Count, imagesNeedingCache.Count);
            
            // Create a background job for tracking
            var resumeJob = await _backgroundJobService.CreateJobAsync(new CreateBackgroundJobDto
            {
                Type = "resume-collection",
                Description = $"Resume thumbnail/cache generation for {collection.Name}"
            });
            
            // Queue thumbnail generation jobs
            foreach (var image in imagesNeedingThumbnails)
            {
                var thumbnailMessage = new ThumbnailGenerationMessage
                {
                    ImageId = image.Id,
                    CollectionId = collection.Id.ToString(),
                    ImagePath = image.FullPath,
                    ImageFilename = image.Filename,
                    ThumbnailWidth = request.ThumbnailWidth ?? 300,
                    ThumbnailHeight = request.ThumbnailHeight ?? 300,
                    JobId = resumeJob.JobId.ToString()
                };
                
                await _messageQueueService.PublishAsync(thumbnailMessage);
                _logger.LogDebug("Queued thumbnail generation for image {ImageId} in collection {CollectionId}", 
                    image.Id, collection.Id);
            }
            
            // Queue cache generation jobs
            foreach (var image in imagesNeedingCache)
            {
                var cacheMessage = new CacheGenerationMessage
                {
                    ImageId = image.Id,
                    CollectionId = collection.Id.ToString(),
                    ImagePath = image.FullPath,
                    ImageFilename = image.Filename,
                    TargetWidth = request.CacheWidth ?? 1920,
                    TargetHeight = request.CacheHeight ?? 1080,
                    Quality = 85,
                    Format = "jpeg",
                    ForceRegenerate = false,
                    JobId = resumeJob.JobId.ToString()
                };
                
                await _messageQueueService.PublishAsync(cacheMessage);
                _logger.LogDebug("Queued cache generation for image {ImageId} in collection {CollectionId}", 
                    image.Id, collection.Id);
            }
            
            _logger.LogInformation("Successfully queued {ThumbnailCount} thumbnail jobs and {CacheCount} cache jobs for collection {Name}", 
                imagesNeedingThumbnails.Count, imagesNeedingCache.Count, collection.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing missing thumbnail/cache jobs for collection {CollectionId}", collection.Id);
            throw;
        }
    }
}
