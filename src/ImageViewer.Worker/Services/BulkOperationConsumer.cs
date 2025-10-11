using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Application.Services;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for bulk operation messages
/// </summary>
public class BulkOperationConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BulkOperationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BulkOperationConsumer> logger)
        : base(connection, options, logger, options.Value.BulkOperationQueue, "bulk-operation-consumer")
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🔔 Received RabbitMQ message: {Message}", message);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var bulkMessage = JsonSerializer.Deserialize<BulkOperationMessage>(message, options);
            if (bulkMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize BulkOperationMessage from: {Message}", message);
                return;
            }

            _logger.LogInformation("🚀 Processing bulk operation {OperationType} for {CollectionCount} collections", 
                bulkMessage.OperationType, bulkMessage.CollectionIds.Count);

            using var scope = _serviceScopeFactory.CreateScope();
            var bulkService = scope.ServiceProvider.GetRequiredService<IBulkService>();
            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
            var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();

            // Update job status to Running
            if (!string.IsNullOrEmpty(bulkMessage.JobId))
            {
                var jobId = ObjectId.Parse(bulkMessage.JobId);
                await backgroundJobService.UpdateJobStatusAsync(jobId, "Running");
                _logger.LogInformation("📊 Updated job {JobId} status to Running", bulkMessage.JobId);
            }
            
            switch (bulkMessage.OperationType.ToLowerInvariant())
            {
                case "bulkaddcollections":
                    await ProcessBulkAddCollectionsAsync(bulkMessage, bulkService, backgroundJobService, messageQueueService);
                    break;
                case "scanall":
                    await ProcessScanAllCollectionsAsync(bulkMessage, messageQueueService);
                    break;
                case "generateallthumbnails":
                    await ProcessGenerateAllThumbnailsAsync(bulkMessage, messageQueueService);
                    break;
                case "generateallcache":
                    await ProcessGenerateAllCacheAsync(bulkMessage, messageQueueService);
                    break;
                case "scancollections":
                    await ProcessScanCollectionsAsync(bulkMessage, messageQueueService);
                    break;
                case "generatethumbnails":
                    await ProcessGenerateThumbnailsAsync(bulkMessage, messageQueueService);
                    break;
                case "generatecache":
                    await ProcessGenerateCacheAsync(bulkMessage, messageQueueService);
                    break;
                default:
                    _logger.LogWarning("Unknown bulk operation type: {OperationType}", bulkMessage.OperationType);
                    break;
            }

            // Update job status to Completed
            if (!string.IsNullOrEmpty(bulkMessage.JobId))
            {
                var jobId = ObjectId.Parse(bulkMessage.JobId);
                await backgroundJobService.UpdateJobStatusAsync(jobId, "Completed");
                _logger.LogInformation("📊 Updated job {JobId} status to Completed", bulkMessage.JobId);
            }

            _logger.LogInformation("✅ Successfully completed bulk operation {OperationType}", bulkMessage.OperationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing bulk operation message");
            
            // Update job status to Failed
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                var bulkMessage = JsonSerializer.Deserialize<BulkOperationMessage>(message, options);
                if (!string.IsNullOrEmpty(bulkMessage?.JobId))
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                    var jobId = ObjectId.Parse(bulkMessage!.JobId);
                    await backgroundJobService.UpdateJobStatusAsync(jobId, "Failed");
                    _logger.LogInformation("📊 Updated job {JobId} status to Failed", bulkMessage.JobId);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update job status to Failed");
            }
            
            throw;
        }
    }

    private async Task ProcessBulkAddCollectionsAsync(BulkOperationMessage bulkMessage, IBulkService bulkService, IBackgroundJobService backgroundJobService, IMessageQueueService messageQueueService)
    {
        try
        {
            _logger.LogInformation("📦 Processing bulk add collections operation");

            // Extract parameters from the bulk message
            var parameters = bulkMessage.Parameters;
            var parentPath = parameters.GetValueOrDefault("ParentPath")?.ToString() ?? "";
            var collectionPrefix = parameters.GetValueOrDefault("CollectionPrefix")?.ToString() ?? "";
            var includeSubfolders = parameters.GetValueOrDefault("IncludeSubfolders")?.ToString() == "True";
            var autoAdd = parameters.GetValueOrDefault("AutoAdd")?.ToString() == "True";
            var overwriteExisting = parameters.GetValueOrDefault("OverwriteExisting")?.ToString() == "True";
            var processCompressedFiles = parameters.GetValueOrDefault("ProcessCompressedFiles")?.ToString() == "True";
            var maxConcurrentOperations = int.TryParse(parameters.GetValueOrDefault("MaxConcurrentOperations")?.ToString(), out var maxConcurrent) ? maxConcurrent : 5;

            _logger.LogInformation("📋 Extracted parameters:");
            _logger.LogInformation("   📁 ParentPath: {ParentPath}", parentPath);
            _logger.LogInformation("   🏷️ CollectionPrefix: {CollectionPrefix}", collectionPrefix);
            _logger.LogInformation("   📂 IncludeSubfolders: {IncludeSubfolders}", includeSubfolders);
            _logger.LogInformation("   ➕ AutoAdd: {AutoAdd}", autoAdd);
            _logger.LogInformation("   🔄 OverwriteExisting: {OverwriteExisting}", overwriteExisting);
            _logger.LogInformation("   📦 ProcessCompressedFiles: {ProcessCompressedFiles}", processCompressedFiles);
            _logger.LogInformation("   ⚡ MaxConcurrentOperations: {MaxConcurrentOperations}", maxConcurrentOperations);

            // Create the bulk request from message parameters
            var bulkRequest = new BulkAddCollectionsRequest
            {
                ParentPath = parentPath,
                CollectionPrefix = collectionPrefix,
                IncludeSubfolders = includeSubfolders,
                AutoAdd = autoAdd,
                OverwriteExisting = overwriteExisting,
                EnableCache = processCompressedFiles, // Map to existing property
                AutoScan = true // Enable auto scan
            };

            _logger.LogInformation("🚀 Starting bulk add collections for path: {ParentPath}", bulkRequest.ParentPath);

            // Process the bulk operation
            var result = await bulkService.BulkAddCollectionsAsync(bulkRequest);

            _logger.LogInformation("✅ Bulk add collections completed successfully!");
            _logger.LogInformation("📊 Results Summary:");
            _logger.LogInformation("   ✅ Success: {SuccessCount}", result.SuccessCount);
            _logger.LogInformation("   ➕ Created: {CreatedCount}", result.CreatedCount);
            _logger.LogInformation("   🔄 Updated: {UpdatedCount}", result.UpdatedCount);
            _logger.LogInformation("   ⏭️ Skipped: {SkippedCount}", result.SkippedCount);
            _logger.LogInformation("   ❌ Errors: {ErrorCount}", result.ErrorCount);
            
            if (result.Errors?.Any() == true)
            {
                _logger.LogWarning("⚠️ Errors encountered during bulk operation:");
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("   ❌ {Error}", error);
                }
            }

            // NEW: Create individual collection scan jobs for each created collection
            // Note: Collection scan jobs are automatically created by CollectionService.CreateCollectionAsync()
            // when AutoScan is enabled (default). No need to create duplicate scan jobs here.
            // This was causing double-scanning of each collection!
            
            if (result.SuccessCount > 0)
            {
                _logger.LogInformation("✅ {SuccessCount} collections created. Scan jobs automatically created by CollectionService.", result.SuccessCount);
            }
            
            // Update job status to completed
            var jobId = ObjectId.Parse(bulkMessage.JobId);
            await backgroundJobService.UpdateJobStatusAsync(jobId, "Completed", "Bulk operation completed successfully");
            _logger.LogInformation("✅ Bulk operation job {JobId} marked as completed", bulkMessage.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing bulk add collections operation");
            // Update job status to failed
            var jobId = ObjectId.Parse(bulkMessage.JobId);
            await backgroundJobService.UpdateJobStatusAsync(jobId, "Failed", $"Bulk operation failed: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessScanAllCollectionsAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("🔍 Processing scan all collections operation");
        
                using var scope = _serviceScopeFactory.CreateScope();
        var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
        
        // Get all collections
        var collections = await collectionService.GetCollectionsAsync(page: 1, pageSize: 1000);
        _logger.LogInformation("📁 Found {CollectionCount} collections to scan", collections.Count());
        
        // Create individual collection scan jobs
        foreach (var collection in collections)
        {
            try
            {
                var scanMessage = new CollectionScanMessage
                {
                    CollectionId = collection.Id.ToString(), // Convert ObjectId to string
                    CollectionPath = collection.Path,
                    CollectionType = collection.Type,
                    ForceRescan = true, // Force rescan for bulk operations
                };

                // Queue the scan job
                await messageQueueService.PublishAsync(scanMessage, "collection.scan");
                _logger.LogInformation("📋 Queued scan job for collection {CollectionId}: {CollectionName}", 
                    collection.Id, collection.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create scan job for collection {CollectionId}", collection.Id);
            }
        }
        
        _logger.LogInformation("✅ Created {ScanJobCount} collection scan jobs", collections.Count());
    }

    private async Task ProcessGenerateAllThumbnailsAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("🖼️ Processing generate all thumbnails operation");
        
                using var scope = _serviceScopeFactory.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
        
        // Get all images using embedded design - iterate through collections
        var collections = await collectionService.GetCollectionsAsync(page: 1, pageSize: 1000);
        int totalImages = 0;
        
        foreach (var collection in collections)
        {
            var collectionImages = await imageService.GetEmbeddedImagesByCollectionAsync(collection.Id);
            totalImages += collectionImages.Count();
            
            // Create individual thumbnail generation jobs for each image in this collection
            foreach (var image in collectionImages)
            {
                try
                {
                    var thumbnailMessage = new ThumbnailGenerationMessage
                    {
                        ImageId = image.Id, // Already a string
                        CollectionId = collection.Id.ToString(), // Use collection.Id from outer loop
                        ImagePath = image.RelativePath, // This should be the full path
                        ImageFilename = image.Filename,
                        ThumbnailWidth = 300, // Default thumbnail size
                        ThumbnailHeight = 300,
                    };

                    // Queue the thumbnail generation job
                    await messageQueueService.PublishAsync(thumbnailMessage, "thumbnail.generation");
                    _logger.LogInformation("📋 Queued thumbnail generation job for image {ImageId}: {Filename}", 
                        image.Id, image.Filename);
                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create thumbnail generation job for image {ImageId}", image.Id);
            }
        }
        }
        
        _logger.LogInformation("✅ Created {ThumbnailJobCount} thumbnail generation jobs for {CollectionCount} collections", totalImages, collections.Count());
    }

    private async Task ProcessGenerateAllCacheAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("💾 Processing generate all cache operation");
        
                using var scope = _serviceScopeFactory.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
        
        // Get all images using embedded design - iterate through collections
        var collections = await collectionService.GetCollectionsAsync(page: 1, pageSize: 1000);
        int totalImages = 0;
        
        foreach (var collection in collections)
        {
            var collectionImages = await imageService.GetEmbeddedImagesByCollectionAsync(collection.Id);
            totalImages += collectionImages.Count();
            
            // Create individual cache generation jobs for each image in this collection
            foreach (var image in collectionImages)
            {
                try
                {
                    var cacheMessage = new CacheGenerationMessage
                    {
                        ImageId = image.Id, // Already a string
                        CollectionId = collection.Id.ToString(), // Use collection.Id from outer loop
                        ImagePath = image.RelativePath, // This should be the full path
                        CachePath = "", // Will be determined by cache service
                        CacheWidth = 1920, // Default cache size
                        CacheHeight = 1080,
                        Quality = 85,
                        ForceRegenerate = true, // Force regeneration for bulk operations
                    };

                    // Queue the cache generation job
                    await messageQueueService.PublishAsync(cacheMessage, "cache.generation");
                    _logger.LogInformation("📋 Queued cache generation job for image {ImageId}: {Filename}", 
                        image.Id, image.Filename);
                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create cache generation job for image {ImageId}", image.Id);
            }
        }
        }
        
        _logger.LogInformation("✅ Created {CacheJobCount} cache generation jobs for {CollectionCount} collections", totalImages, collections.Count());
    }

    private async Task ProcessScanCollectionsAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("🔍 Processing scan collections operation for {CollectionIds}", 
            string.Join(", ", bulkMessage.CollectionIds));
        
                using var scope = _serviceScopeFactory.CreateScope();
        var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
        
        // Create individual collection scan jobs for specified collections
        foreach (var collectionId in bulkMessage.CollectionIds)
        {
            try
            {
                var collection = await collectionService.GetCollectionByIdAsync(ObjectId.Parse(collectionId.ToString()));
                if (collection != null)
                {
                    var scanMessage = new CollectionScanMessage
                    {
                        CollectionId = collection.Id.ToString(), // Convert ObjectId to string
                        CollectionPath = collection.Path,
                        CollectionType = collection.Type,
                        ForceRescan = true, // Force rescan for bulk operations
                        CreatedBy = "BulkOperationConsumer",
                        CreatedBySystem = "ImageViewer.Worker"
                    };

                    // Queue the scan job
                    await messageQueueService.PublishAsync(scanMessage, "collection.scan");
                    _logger.LogInformation("📋 Queued scan job for collection {CollectionId}: {CollectionName}", 
                        collection.Id, collection.Name);
                }
                else
                {
                    _logger.LogWarning("⚠️ Collection {CollectionId} not found, skipping scan", collectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create scan job for collection {CollectionId}", collectionId);
            }
        }
        
        _logger.LogInformation("✅ Created {ScanJobCount} collection scan jobs", bulkMessage.CollectionIds.Count);
    }

    private async Task ProcessGenerateThumbnailsAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("🖼️ Processing generate thumbnails operation for {CollectionIds}", 
            string.Join(", ", bulkMessage.CollectionIds));
        
        using var scope = _serviceScopeFactory.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        var collectionRepository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();
        var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
        var imageProcessingSettingsService = scope.ServiceProvider.GetService<IImageProcessingSettingsService>();
        
        // Get thumbnail settings
        var thumbnailFormat = imageProcessingSettingsService != null 
            ? await imageProcessingSettingsService.GetThumbnailFormatAsync() 
            : "jpeg";
        var thumbnailQuality = imageProcessingSettingsService != null 
            ? await imageProcessingSettingsService.GetThumbnailQualityAsync() 
            : 90;
        var thumbnailWidth = 300;
        var thumbnailHeight = 300;
        
        // Get images for specified collections using embedded design
        int totalImages = 0;
        foreach (var collectionId in bulkMessage.CollectionIds)
        {
            try
            {
                var collectionObjectId = ObjectId.Parse(collectionId.ToString());
                var collection = await collectionRepository.GetByIdAsync(collectionObjectId);
                if (collection == null)
                {
                    _logger.LogWarning("⚠️ Collection {CollectionId} not found", collectionId);
                    continue;
                }
                
                var collectionImages = await imageService.GetEmbeddedImagesByCollectionAsync(collectionObjectId);
                var imagesList = collectionImages.ToList();
                totalImages += imagesList.Count;
                
                // Pre-filter: Only queue images without thumbnails
                var imagesNeedingThumbnails = imagesList.Where(img => 
                    !collection.Thumbnails.Any(t => 
                        t.ImageId == img.Id && 
                        t.Width == thumbnailWidth && 
                        t.Height == thumbnailHeight
                    )
                ).ToList();
                
                if (!imagesNeedingThumbnails.Any())
                {
                    _logger.LogInformation("✅ Collection {CollectionId} already has all thumbnails, skipping", collectionId);
                    continue;
                }
                
                _logger.LogInformation("📊 Collection {CollectionId}: {Total} images, {HasThumbnails} with thumbnails, {Remaining} to process",
                    collectionId, imagesList.Count, imagesList.Count - imagesNeedingThumbnails.Count, imagesNeedingThumbnails.Count);
                
                // Create FileProcessingJobState for this collection
                var jobId = $"thumbnail_{bulkMessage.JobId}_{collectionId}";
                
                var jobSettings = System.Text.Json.JsonSerializer.Serialize(new
                {
                    width = thumbnailWidth,
                    height = thumbnailHeight,
                    quality = thumbnailQuality,
                    format = thumbnailFormat
                });
                
                var jobState = new Domain.Entities.FileProcessingJobState(
                    jobId: jobId,
                    jobType: "thumbnail",
                    collectionId: collectionId.ToString(),
                    collectionName: collection.Name,
                    totalImages: imagesNeedingThumbnails.Count,
                    outputFolderId: null,
                    outputFolderPath: null,
                    jobSettings: jobSettings
                );
                
                jobState.Start(); // Set status to Running
                await jobStateRepository.CreateAsync(jobState);
                
                _logger.LogInformation("✅ Created FileProcessingJobState {JobId} for collection {CollectionId} with {Count} images",
                    jobId, collectionId, imagesNeedingThumbnails.Count);
                
                // Create thumbnail generation messages in batches for better performance
                var thumbnailMessages = new List<ThumbnailGenerationMessage>();
                
                foreach (var image in imagesNeedingThumbnails)
                {
                    try
                    {
                        var thumbnailMessage = new ThumbnailGenerationMessage
                        {
                            JobId = jobId, // Link to FileProcessingJobState
                            ImageId = image.Id,
                            CollectionId = collectionId.ToString(),
                            ImagePath = collection.GetFullImagePath(image), // Use full path
                            ImageFilename = image.Filename,
                            ThumbnailWidth = thumbnailWidth,
                            ThumbnailHeight = thumbnailHeight,
                            ScanJobId = bulkMessage.JobId // Link to parent scan job
                        };

                        thumbnailMessages.Add(thumbnailMessage);
                        
                        // Publish in batches of 100 for optimal performance
                        if (thumbnailMessages.Count >= 100)
                        {
                            await messageQueueService.PublishBatchAsync(thumbnailMessages, "thumbnail.generation");
                            _logger.LogInformation("📋 Published batch of {Count} thumbnail generation messages", thumbnailMessages.Count);
                            thumbnailMessages.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to create thumbnail generation message for image {ImageId}", image.Id);
                    }
                }
                
                // Publish remaining messages
                if (thumbnailMessages.Any())
                {
                    await messageQueueService.PublishBatchAsync(thumbnailMessages, "thumbnail.generation");
                    _logger.LogInformation("📋 Published final batch of {Count} thumbnail generation messages", thumbnailMessages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get images for collection {CollectionId}", collectionId);
            }
        }
        
        _logger.LogInformation("✅ Created {ThumbnailJobCount} thumbnail generation jobs for {CollectionCount} collections", totalImages, bulkMessage.CollectionIds.Count);
    }

    private async Task ProcessGenerateCacheAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("💾 Processing generate cache operation for {CollectionIds}", 
            string.Join(", ", bulkMessage.CollectionIds));
        
        using var scope = _serviceScopeFactory.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        var collectionRepository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();
        var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
        var cacheFolderSelectionService = scope.ServiceProvider.GetService<ICacheFolderSelectionService>();
        var imageProcessingSettingsService = scope.ServiceProvider.GetService<IImageProcessingSettingsService>();
        
        // Get cache settings
        var cacheFormat = imageProcessingSettingsService != null 
            ? await imageProcessingSettingsService.GetCacheFormatAsync() 
            : "jpeg";
        var cacheQuality = imageProcessingSettingsService != null 
            ? await imageProcessingSettingsService.GetCacheQualityAsync() 
            : 85;
        var cacheWidth = 1920;
        var cacheHeight = 1080;
        
        // Get images for specified collections using embedded design
        int totalImages = 0;
        foreach (var collectionId in bulkMessage.CollectionIds)
        {
            try
            {
                var collectionObjectId = ObjectId.Parse(collectionId.ToString());
                var collection = await collectionRepository.GetByIdAsync(collectionObjectId);
                if (collection == null)
                {
                    _logger.LogWarning("⚠️ Collection {CollectionId} not found", collectionId);
                    continue;
                }
                
                var collectionImages = await imageService.GetEmbeddedImagesByCollectionAsync(collectionObjectId);
                var imagesList = collectionImages.ToList();
                totalImages += imagesList.Count;
                
                // Pre-filter: Only queue uncached images
                var uncachedImages = imagesList.Where(img => 
                    !collection.CacheImages.Any(c => 
                        c.ImageId == img.Id && 
                        c.Width == cacheWidth && 
                        c.Height == cacheHeight
                    )
                ).ToList();
                
                if (!uncachedImages.Any())
                {
                    _logger.LogInformation("✅ Collection {CollectionId} already fully cached, skipping", collectionId);
                    continue;
                }
                
                _logger.LogInformation("📊 Collection {CollectionId}: {Total} images, {Cached} cached, {Remaining} to process",
                    collectionId, imagesList.Count, imagesList.Count - uncachedImages.Count, uncachedImages.Count);
                
                // Create FileProcessingJobState for this collection
                var jobId = $"cache_{bulkMessage.JobId}_{collectionId}";
                var cacheFolderPath = await cacheFolderSelectionService?.SelectCacheFolderForCacheAsync(
                    collectionObjectId, 
                    uncachedImages[0].Id,
                    cacheWidth, 
                    cacheHeight, 
                    cacheFormat) ?? string.Empty;
                
                var jobSettings = System.Text.Json.JsonSerializer.Serialize(new
                {
                    width = cacheWidth,
                    height = cacheHeight,
                    quality = cacheQuality,
                    format = cacheFormat
                });
                
                var jobState = new Domain.Entities.FileProcessingJobState(
                    jobId: jobId,
                    jobType: "cache",
                    collectionId: collectionId.ToString(),
                    collectionName: collection.Name,
                    totalImages: uncachedImages.Count,
                    outputFolderId: null, // Will be determined from path
                    outputFolderPath: !string.IsNullOrEmpty(cacheFolderPath) ? Path.GetDirectoryName(Path.GetDirectoryName(cacheFolderPath)) : null,
                    jobSettings: jobSettings
                );
                
                jobState.Start(); // Set status to Running
                await jobStateRepository.CreateAsync(jobState);
                
                _logger.LogInformation("✅ Created FileProcessingJobState {JobId} for collection {CollectionId} with {Count} images",
                    jobId, collectionId, uncachedImages.Count);
                
                // Create cache generation messages in batches for better performance
                var cacheMessages = new List<CacheGenerationMessage>();
                
                foreach (var image in uncachedImages)
                {
                    try
                    {
                        var cacheMessage = new CacheGenerationMessage
                        {
                            JobId = jobId, // Link to FileProcessingJobState
                            ImageId = image.Id,
                            CollectionId = collectionId.ToString(),
                            ImagePath = collection.GetFullImagePath(image), // Use full path
                            CachePath = "", // Will be determined by cache service
                            CacheWidth = cacheWidth,
                            CacheHeight = cacheHeight,
                            Quality = cacheQuality,
                            Format = cacheFormat,
                            ForceRegenerate = false, // Don't force - we already pre-filtered
                            ScanJobId = bulkMessage.JobId // Link to parent scan job
                        };

                        cacheMessages.Add(cacheMessage);
                        
                        // Publish in batches of 100 for optimal performance
                        if (cacheMessages.Count >= 100)
                        {
                            await messageQueueService.PublishBatchAsync(cacheMessages, "cache.generation");
                            _logger.LogInformation("📋 Published batch of {Count} cache generation messages", cacheMessages.Count);
                            cacheMessages.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to create cache generation message for image {ImageId}", image.Id);
                    }
                }
                
                // Publish remaining messages
                if (cacheMessages.Any())
                {
                    await messageQueueService.PublishBatchAsync(cacheMessages, "cache.generation");
                    _logger.LogInformation("📋 Published final batch of {Count} cache generation messages", cacheMessages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get images for collection {CollectionId}", collectionId);
            }
        }
        
        _logger.LogInformation("✅ Created {CacheJobCount} cache generation jobs for {CollectionCount} collections", totalImages, bulkMessage.CollectionIds.Count);
    }
}
