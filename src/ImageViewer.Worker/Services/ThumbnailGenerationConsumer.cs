using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Data;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for thumbnail generation messages
/// </summary>
public class ThumbnailGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private int _processedCount = 0;
    private readonly object _counterLock = new object();

    public ThumbnailGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ThumbnailGenerationConsumer> logger)
        : base(connection, options, logger, "thumbnail.generation", "thumbnail-generation-consumer")
    {
        _serviceScopeFactory = serviceScopeFactory;
        _rabbitMQOptions = options.Value;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("🖼️ Received thumbnail generation message: {Message}", message);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var thumbnailMessage = JsonSerializer.Deserialize<ThumbnailGenerationMessage>(message, options);
            if (thumbnailMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize ThumbnailGenerationMessage from: {Message}", message);
                return;
            }

            _logger.LogDebug("🖼️ Generating thumbnail for image {ImageId} ({Filename})", 
                thumbnailMessage.ImageId, thumbnailMessage.ImageFilename);

            // Try to create scope, handle disposal gracefully
            IServiceScope? scope = null;
            try
            {
                scope = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping thumbnail generation.");
                return;
            }

            using (scope)
            {
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            var collectionRepository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();
            var settingsService = scope.ServiceProvider.GetRequiredService<IImageProcessingSettingsService>();

            // Update progress heartbeat to show job is actively processing
            if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
            {
                try
                {
                    var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                    await jobStateRepository.UpdateStatusAsync(thumbnailMessage.JobId, "Running");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to update progress heartbeat for job {JobId}", thumbnailMessage.JobId);
                }
            }

            // Check if image file exists
            if (!File.Exists(thumbnailMessage.ImagePath) && !ArchiveFileHelper.IsArchiveEntryPath(thumbnailMessage.ImagePath))
            {
                _logger.LogWarning("❌ Image file {Path} does not exist, skipping thumbnail generation", thumbnailMessage.ImagePath);
                return;
            }

            // Validate source image file size (prevent OOM on huge images)
            long fileSize = 0;
            long maxSize = 0;
            
            if (ArchiveFileHelper.IsArchiveEntryPath(thumbnailMessage.ImagePath))
            {
                // ZIP entry - get uncompressed size without extraction
                fileSize = ArchiveFileHelper.GetArchiveEntrySize(thumbnailMessage.ImagePath, _logger);
                maxSize = _rabbitMQOptions.MaxZipEntrySizeBytes; // 20GB for ZIP entries
                
                if (fileSize > maxSize)
                {
                    _logger.LogWarning("⚠️ ZIP entry too large ({SizeGB}GB), skipping thumbnail generation for {ImageId}", 
                        fileSize / 1024.0 / 1024.0 / 1024.0, thumbnailMessage.ImageId);
                    
                    if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                    {
                        var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                        _logger.LogError("ZIP entry too large: {SizeGB}GB (max {MaxGB}GB) for {ImageId}", 
                            fileSize / 1024.0 / 1024.0 / 1024.0, maxSize / 1024.0 / 1024.0 / 1024.0, thumbnailMessage.ImageId);
                        await jobStateRepository.AtomicIncrementFailedAsync(thumbnailMessage.JobId, thumbnailMessage.ImageId);
                    }
                    
                    return;
                }
            }
            else
            {
                // Regular file - check file size on disk
                var imageFile = new FileInfo(thumbnailMessage.ImagePath);
                fileSize = imageFile.Exists ? imageFile.Length : 0;
                maxSize = _rabbitMQOptions.MaxImageSizeBytes; // 500MB for regular files
                
                if (fileSize > maxSize)
                {
                    _logger.LogWarning("⚠️ Image file too large ({SizeMB}MB), skipping thumbnail generation for {ImageId}", 
                        fileSize / 1024.0 / 1024.0, thumbnailMessage.ImageId);
                    
                    if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                    {
                        var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                        _logger.LogError("Image file too large: {SizeMB}MB (max {MaxMB}MB) for {ImageId}", 
                            fileSize / 1024.0 / 1024.0, maxSize / 1024.0 / 1024.0, thumbnailMessage.ImageId);
                        await jobStateRepository.AtomicIncrementFailedAsync(thumbnailMessage.JobId, thumbnailMessage.ImageId);
                    }
                    
                    return;
                }
            }

            // Get format from settings (needed for thumbnail path calculation)
            var format = await settingsService.GetThumbnailFormatAsync();
            
            // Check if thumbnail already exists in database
            var collectionId = ObjectId.Parse(thumbnailMessage.CollectionId);
            var collection = await collectionRepository.GetByIdAsync(collectionId);
            if (collection != null)
            {
                var existingThumbnail = collection.Thumbnails?.FirstOrDefault(t =>
                    t.ImageId == thumbnailMessage.ImageId &&
                    t.Width == thumbnailMessage.ThumbnailWidth &&
                    t.Height == thumbnailMessage.ThumbnailHeight
                );

                if (existingThumbnail != null && File.Exists(existingThumbnail.ThumbnailPath))
                {
                    _logger.LogDebug("📁 Thumbnail already exists in collection and on disk for image {ImageId}, skipping generation", thumbnailMessage.ImageId);
                    
                    // Track as skipped in FileProcessingJobState
                    if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                    {
                        try
                        {
                            var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                            await jobStateRepository.AtomicIncrementSkippedAsync(thumbnailMessage.JobId, thumbnailMessage.ImageId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to track skipped thumbnail for image {ImageId} in job {JobId}",
                                thumbnailMessage.ImageId, thumbnailMessage.JobId);
                        }
                    }
                    
                    // CRITICAL: Update job stage progress for skipped thumbnails
                    if (!string.IsNullOrEmpty(thumbnailMessage.ScanJobId))
                    {
                        try
                        {
                            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                            await backgroundJobService.IncrementJobStageProgressAsync(
                                ObjectId.Parse(thumbnailMessage.ScanJobId),
                                "thumbnail",
                                incrementBy: 1);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to update job stage for skipped thumbnail {ImageId}", thumbnailMessage.ImageId);
                        }
                    }
                    
                    return;
                }
                
                // CRITICAL: Check if thumbnail file exists on disk but NOT in collection array
                // This handles Resume Incomplete scenario where thumbnail array was cleared but disk files still exist
                if (existingThumbnail == null)
                {
                    var thumbnailPath = await GetThumbnailPath(
                        thumbnailMessage.ImagePath,
                        thumbnailMessage.ThumbnailWidth,
                        thumbnailMessage.ThumbnailHeight,
                        collectionId,
                        format);
                    
                    if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
                    {
                        _logger.LogInformation("📝 Re-adding existing thumbnail file to collection for image {ImageId}", thumbnailMessage.ImageId);
                        
                        // Thumbnail file exists on disk but not in collection - add the entry
                        var fileInfo = new FileInfo(thumbnailPath);
                        var thumbnailEmbedded = new ThumbnailEmbedded(
                            thumbnailMessage.ImageId,
                            thumbnailPath,
                            thumbnailMessage.ThumbnailWidth,
                            thumbnailMessage.ThumbnailHeight,
                            fileInfo.Length,
                            fileInfo.Extension.TrimStart('.').ToUpperInvariant(),
                            95 // quality
                        );
                        
                        await collectionRepository.AtomicAddThumbnailAsync(collectionId, thumbnailEmbedded);
                        
                        // Track as skipped in FileProcessingJobState
                        if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                        {
                            try
                            {
                                var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                                await jobStateRepository.AtomicIncrementSkippedAsync(thumbnailMessage.JobId, thumbnailMessage.ImageId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to track skipped thumbnail for image {ImageId} in job {JobId}",
                                    thumbnailMessage.ImageId, thumbnailMessage.JobId);
                            }
                        }
                        
                        // CRITICAL: Update job stage progress for re-added thumbnails
                        if (!string.IsNullOrEmpty(thumbnailMessage.ScanJobId))
                        {
                            try
                            {
                                var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                                await backgroundJobService.IncrementJobStageProgressAsync(
                                    ObjectId.Parse(thumbnailMessage.ScanJobId),
                                    "thumbnail",
                                    incrementBy: 1);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to update job stage for re-added thumbnail {ImageId}", thumbnailMessage.ImageId);
                            }
                        }
                        
                        return;
                    }
                }
            }

            // Generate thumbnail
            try
            {
                
                var thumbnailPath = await GenerateThumbnail(
                thumbnailMessage.ImagePath,
                thumbnailMessage.ThumbnailWidth,
                thumbnailMessage.ThumbnailHeight,
                    imageProcessingService,
                    collectionId,
                cancellationToken);

                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    // Update database with thumbnail information
                    await UpdateThumbnailInfoInDatabase(thumbnailMessage, thumbnailPath, collectionRepository);
                    
                    // REAL-TIME JOB TRACKING: Update job stage immediately after each thumbnail
                    if (!string.IsNullOrEmpty(thumbnailMessage.ScanJobId))
                    {
                        try
                        {
                            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                            await backgroundJobService.IncrementJobStageProgressAsync(
                                ObjectId.Parse(thumbnailMessage.ScanJobId),
                                "thumbnail",
                                incrementBy: 1);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to update job stage for {JobId}, fallback monitor will handle it", thumbnailMessage.ScanJobId);
                        }
                    }
                    
                    // Track progress in FileProcessingJobState if jobId is present
                    if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                    {
                        try
                        {
                            var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                            var fileInfo = new FileInfo(thumbnailPath);
                            await jobStateRepository.AtomicIncrementCompletedAsync(
                                thumbnailMessage.JobId, 
                                thumbnailMessage.ImageId,
                                fileInfo.Length);
                            
                            // Check if job is complete and mark it as finished
                            await CheckAndMarkJobComplete(thumbnailMessage.JobId, jobStateRepository);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to track thumbnail progress for image {ImageId} in job {JobId}", 
                                thumbnailMessage.ImageId, thumbnailMessage.JobId);
                        }
                    }
                    
                    // Batched logging - log every 50 files
                    int currentCount;
                    lock (_counterLock)
                    {
                        _processedCount++;
                        currentCount = _processedCount;
                    }
                    
                    if (currentCount % 50 == 0)
                    {
                        _logger.LogInformation("✅ Generated {Count} thumbnails (latest: {ImageId})", currentCount, thumbnailMessage.ImageId);
                    }
                    else
                    {
                        _logger.LogDebug("✅ Successfully generated thumbnail for image {ImageId} at {ThumbnailPath}", 
                            thumbnailMessage.ImageId, thumbnailPath);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Thumbnail generation returned empty path for image {ImageId}", thumbnailMessage.ImageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate thumbnail for image {ImageId}", thumbnailMessage.ImageId);
                
                // Track as failed in FileProcessingJobState
                if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                {
                    try
                    {
                        var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                        await jobStateRepository.AtomicIncrementFailedAsync(thumbnailMessage.JobId, thumbnailMessage.ImageId);
                        
                        // Check failure threshold and alert if needed (every 10 failures)
                        var jobState = await jobStateRepository.GetByJobIdAsync(thumbnailMessage.JobId);
                        if (jobState != null && jobState.FailedImages % 10 == 0 && jobState.FailedImages > 0)
                        {
                            var alertService = scope.ServiceProvider.GetService<IJobFailureAlertService>();
                            if (alertService != null)
                            {
                                await alertService.CheckAndAlertAsync(thumbnailMessage.JobId, failureThreshold: 0.1);
                            }
                        }
                    }
                    catch (Exception trackEx)
                    {
                        _logger.LogWarning(trackEx, "Failed to track thumbnail error for image {ImageId} in job {JobId}", 
                            thumbnailMessage.ImageId, thumbnailMessage.JobId);
                    }
                }
            }
            } // Close using (scope) block
        }
        catch (Exception ex)
        {
            // Deserialize message to get job/image info for tracking
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var thumbnailMsg = JsonSerializer.Deserialize<ThumbnailGenerationMessage>(message, options);
            
            // Check if this is a corrupted/unsupported file error (should skip, not retry)
            bool isSkippableError = (ex is InvalidOperationException && (ex.Message.Contains("Failed to decode image") || 
                                                                        ex.Message.Contains("Failed to decode") ||
                                                                        ex.Message.Contains("Unable to decode") ||
                                                                        ex.Message.Contains("Cannot decode") ||
                                                                        ex.Message.Contains("corrupted") ||
                                                                        ex.Message.Contains("invalid image"))) ||
                                   (ex is DirectoryNotFoundException) ||
                                   (ex is FileNotFoundException) ||
                                   (ex is UnauthorizedAccessException) ||
                                   (ex is PathTooLongException) ||
                                   (ex is ArgumentException && ex.Message.Contains("Path")) ||
                                   (ex is NotSupportedException && ex.Message.Contains("format")) ||
                                   (ex is BadImageFormatException) ||
                                   (ex is InvalidDataException && ex.Message.Contains("archive"));
            
            if (isSkippableError)
            {
                _logger.LogWarning(ex, "⚠️ Skipping corrupted/unsupported image file. This message will NOT be retried.");
                
                // Create dummy thumbnail entry for failed processing
                if (!string.IsNullOrEmpty(thumbnailMsg?.JobId) && !string.IsNullOrEmpty(thumbnailMsg?.ImageId))
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var collectionRepository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();
                        var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                        
                        // Create dummy thumbnail entry
                        var dummyThumbnail = ThumbnailEmbedded.CreateDummy(
                            thumbnailMsg.ImageId,
                            ex.Message,
                            ex.GetType().Name
                        );
                        
                        // Add dummy entry to collection
                        var collectionId = ObjectId.Parse(thumbnailMsg.CollectionId);
                        await collectionRepository.AtomicAddThumbnailAsync(collectionId, dummyThumbnail);
                        
                        // Track as completed (not failed) since we handled it
                        await jobStateRepository.AtomicIncrementCompletedAsync(thumbnailMsg.JobId, thumbnailMsg.ImageId, 0);
                        
                        // Track the error type for statistics
                        await jobStateRepository.TrackErrorAsync(thumbnailMsg.JobId, ex.GetType().Name);
                        
                        await CheckAndMarkJobComplete(thumbnailMsg.JobId, jobStateRepository);
                        
                        _logger.LogInformation("✅ Created dummy thumbnail entry for failed image {ImageId}: {Error}", 
                            thumbnailMsg.ImageId, ex.Message);
                    }
                    catch (Exception trackEx)
                    {
                        _logger.LogWarning(trackEx, "Failed to create dummy thumbnail for image {ImageId} in job {JobId}", 
                            thumbnailMsg?.ImageId, thumbnailMsg?.JobId);
                    }
                }
            }
            else
            {
                _logger.LogError(ex, "❌ Error processing thumbnail generation message");
            }
            
            // CRITICAL: Skip corrupted files (don't retry), but throw for other errors (network, disk, etc.)
            if (!isSkippableError)
            {
                throw;
            }
            
            // For skippable errors, we return without throwing = ACK the message (don't retry)
        }
    }

    private async Task<string?> GenerateThumbnail(string imagePath, int width, int height, IImageProcessingService imageProcessingService, ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get format from settings FIRST before determining thumbnail path
            using var settingsScope = _serviceScopeFactory.CreateScope();
            var settingsService = settingsScope.ServiceProvider.GetRequiredService<IImageProcessingSettingsService>();
            var format = await settingsService.GetThumbnailFormatAsync();
            
            // Determine thumbnail path (with correct format extension)
            var thumbnailPath = await GetThumbnailPath(imagePath, width, height, collectionId, format);
            
            // Ensure thumbnail directory exists
            var thumbnailDir = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(thumbnailDir) && !Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }

            // Generate thumbnail using image processing service
            byte[] thumbnailData;
            
            // Get quality setting (format already retrieved earlier)
            var quality = await settingsService.GetThumbnailQualityAsync();
            
            _logger.LogDebug("🎨 Using thumbnail format: {Format}, quality: {Quality}", format, quality);
            
            // Handle ZIP entries
            if (ArchiveFileHelper.IsZipEntryPath(imagePath))
            {
                // Extract image bytes from ZIP
                var imageBytes = await ArchiveFileHelper.ExtractZipEntryBytes(imagePath, null, cancellationToken);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogWarning("❌ Failed to extract ZIP entry: {Path}", imagePath);
                    return null;
                }
                
                thumbnailData = await imageProcessingService.GenerateThumbnailFromBytesAsync(
                    imageBytes, 
                    width, 
                    height,
                    format,
                    quality,
                    cancellationToken);
            }
            else
            {
                // Regular file
                thumbnailData = await imageProcessingService.GenerateThumbnailAsync(
                    imagePath, 
                    width, 
                    height,
                    format,
                    quality,
                    cancellationToken);
            }

            if (thumbnailData != null && thumbnailData.Length > 0)
            {
                // Save thumbnail data to file
            await File.WriteAllBytesAsync(thumbnailPath, thumbnailData, cancellationToken);
                _logger.LogDebug("✅ Generated thumbnail: {ThumbnailPath}", thumbnailPath);
                
                // ATOMIC UPDATE: Increment cache folder size to prevent race conditions
                await UpdateCacheFolderSizeAsync(thumbnailPath, thumbnailData.Length, collectionId);
                
                return thumbnailPath;
            }
            else
            {
                _logger.LogWarning("⚠️ Thumbnail generation failed: No data returned");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating thumbnail for {ImagePath}", imagePath);
            return null;
        }
    }

    private async Task<string> GetThumbnailPath(string imagePath, int width, int height, ObjectId collectionId, string format)
    {
        // Determine file extension based on format
        var extension = format.ToLowerInvariant() switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg" // Default fallback
        };
        
        // Extract filename only (handle archive entries like "archive.zip#entry.png")
        string fileName;
        
        if (ArchiveFileHelper.IsArchiveEntryPath(imagePath))
        {
            // For archive entries, extract ONLY the entry name (after #)
            var (_, entryName) = ArchiveFileHelper.SplitArchiveEntryPath(imagePath);
            fileName = Path.GetFileNameWithoutExtension(entryName);
        }
        else
        {
            // For regular files, use filename only (not full path)
            fileName = Path.GetFileNameWithoutExtension(imagePath);
        }
        
        // Use cache service to get the appropriate cache folder for thumbnails
        using var scope = _serviceScopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // Get all cache folders and select one based on collection ID hash for even distribution
        var cacheFolders = await cacheService.GetCacheFoldersAsync();
        var cacheFoldersList = cacheFolders.ToList();
        
        if (cacheFoldersList.Count == 0)
        {
            throw new InvalidOperationException("No cache folders available");
        }
        
        // Use hash-based distribution to select cache folder
        var hash = collectionId.GetHashCode();
        var selectedIndex = Math.Abs(hash) % cacheFoldersList.Count;
        var selectedCacheFolder = cacheFoldersList[selectedIndex];
        
        // Create proper folder structure: CacheFolder/thumbnails/CollectionId/ImageFileName_WidthxHeight.{ext}
        var collectionIdStr = collectionId.ToString();
        var thumbnailDir = Path.Combine(selectedCacheFolder.Path, "thumbnails", collectionIdStr);
        var thumbnailFileName = $"{fileName}_{width}x{height}{extension}";
        
        _logger.LogDebug("📁 Selected cache folder {CacheFolderName} for thumbnail (format: {Format})", 
            selectedCacheFolder.Name, format);
        
        return Path.Combine(thumbnailDir, thumbnailFileName);
    }

    private async Task UpdateThumbnailInfoInDatabase(ThumbnailGenerationMessage thumbnailMessage, string thumbnailPath, ICollectionRepository collectionRepository)
    {
        try
        {
            _logger.LogDebug("📝 Updating thumbnail info in database for image {ImageId}", thumbnailMessage.ImageId);
            
            // Convert string back to ObjectId for database operations
            var collectionId = ObjectId.Parse(thumbnailMessage.CollectionId);
            
            // Get the collection
            var collection = await collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection {collectionId} not found");
            }
            
            // Create thumbnail embedded object
            var fileInfo = new FileInfo(thumbnailPath);
            var thumbnailEmbedded = new ThumbnailEmbedded(
                thumbnailMessage.ImageId,
                thumbnailPath,
                thumbnailMessage.ThumbnailWidth,
                thumbnailMessage.ThumbnailHeight,
                fileInfo.Length,
                fileInfo.Extension.TrimStart('.').ToUpperInvariant(),
                95 // quality
            );
            
            // Atomically add thumbnail to collection (thread-safe, prevents race conditions!)
            var added = await collectionRepository.AtomicAddThumbnailAsync(collectionId, thumbnailEmbedded);
            if (!added)
            {
                _logger.LogWarning("Failed to add thumbnail to collection {CollectionId} - collection might not exist", collectionId);
                return;
            }
            
            _logger.LogDebug("✅ Thumbnail info created and persisted for image {ImageId}: {ThumbnailPath}", 
                thumbnailMessage.ImageId, thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating thumbnail info in database for image {ImageId}", thumbnailMessage.ImageId);
            throw;
        }
    }

    /// <summary>
    /// Atomically update cache folder size after saving thumbnail
    /// Uses MongoDB $inc operator to prevent race conditions during concurrent operations
    /// </summary>
    private async Task UpdateCacheFolderSizeAsync(string thumbnailPath, long fileSize, ObjectId collectionId)
    {
        try
        {
            // Determine which cache folder was used based on the path
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheFolderRepository = scope.ServiceProvider.GetRequiredService<ICacheFolderRepository>();
            
            // Extract cache folder path from thumbnail path
            // ThumbnailPath format: {CacheFolderPath}/thumbnails/{CollectionId}/{FileName}
            var thumbnailDir = Path.GetDirectoryName(thumbnailPath);
            if (string.IsNullOrEmpty(thumbnailDir))
            {
                _logger.LogWarning("⚠️ Cannot determine cache folder from path: {Path}", thumbnailPath);
                return;
            }
            
            // Get parent directory (remove CollectionId folder)
            var thumbnailsDir = Path.GetDirectoryName(thumbnailDir);
            if (string.IsNullOrEmpty(thumbnailsDir))
            {
                _logger.LogWarning("⚠️ Cannot determine thumbnails directory from path: {Path}", thumbnailPath);
                return;
            }
            
            // Get cache folder root (remove "thumbnails" folder)
            var cacheFolderPath = Path.GetDirectoryName(thumbnailsDir);
            if (string.IsNullOrEmpty(cacheFolderPath))
            {
                _logger.LogWarning("⚠️ Cannot determine cache folder root from path: {Path}", thumbnailPath);
                return;
            }
            
            // Find cache folder by path
            var cacheFolder = await cacheFolderRepository.GetByPathAsync(cacheFolderPath);
            if (cacheFolder == null)
            {
                _logger.LogWarning("⚠️ Cache folder not found for path: {Path}", cacheFolderPath);
                return;
            }
            
            // ATOMIC INCREMENT: Thread-safe update in SINGLE transaction
            await cacheFolderRepository.IncrementCacheStatisticsAsync(cacheFolder.Id, fileSize, 1);
            await cacheFolderRepository.AddCachedCollectionAsync(cacheFolder.Id, collectionId.ToString());
            
            _logger.LogDebug("📊 Atomically incremented cache folder {Name} size by {Size} bytes, file count by 1 (single transaction)", 
                cacheFolder.Name, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to update cache folder statistics for thumbnail: {Path}", thumbnailPath);
            // Don't throw - thumbnail is already saved, this is just statistics
        }
    }

    /// <summary>
    /// Check if job is complete and mark it as finished if so
    /// </summary>
    private async Task CheckAndMarkJobComplete(string jobId, IFileProcessingJobStateRepository jobStateRepository)
    {
        try
        {
            var jobState = await jobStateRepository.GetByJobIdAsync(jobId);
            if (jobState == null) return;

            // Check if all expected images have been processed (completed + failed)
            var totalProcessed = jobState.CompletedImages + jobState.FailedImages;
            var totalExpected = jobState.TotalImages;

            if (totalExpected > 0 && totalProcessed >= totalExpected)
            {
                // Since we now create dummy entries for failed images, 
                // all images are "completed" (either successful or dummy)
                await jobStateRepository.UpdateStatusAsync(jobId, "Completed");
                
                // Update the main BackgroundJob with error statistics
                await UpdateBackgroundJobErrorStats(jobId, jobState);
                
                _logger.LogInformation("✅ Job {JobId} marked as Completed - {Completed}/{Expected} processed (including dummy entries for failed images)", 
                    jobId, jobState.CompletedImages, totalExpected);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check/mark job completion for {JobId}", jobId);
        }
    }

    /// <summary>
    /// Update the main BackgroundJob with error statistics
    /// </summary>
    private async Task UpdateBackgroundJobErrorStats(string jobId, FileProcessingJobState jobState)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
            
            var successCount = jobState.CompletedImages - jobState.DummyEntryCount;
            var errorCount = jobState.DummyEntryCount;
            
            if (errorCount > 0)
            {
                // Job completed with errors
                await backgroundJobService.UpdateJobErrorStatisticsAsync(
                    ObjectId.Parse(jobId), 
                    successCount, 
                    errorCount, 
                    jobState.ErrorSummary
                );
            }
            else
            {
                // Job completed without errors
                await backgroundJobService.UpdateJobErrorStatisticsAsync(
                    ObjectId.Parse(jobId), 
                    successCount, 
                    0, 
                    null
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update BackgroundJob error statistics for {JobId}", jobId);
        }
    }
}