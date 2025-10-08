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
    private readonly IServiceProvider _serviceProvider;

    public BulkOperationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider,
        ILogger<BulkOperationConsumer> logger)
        : base(connection, options, logger, options.Value.BulkOperationQueue, "bulk-operation-consumer")
    {
        _serviceProvider = serviceProvider;
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

            using var scope = _serviceProvider.CreateScope();
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
                    using var scope = _serviceProvider.CreateScope();
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
            if (result.SuccessCount > 0)
            {
                _logger.LogInformation("🔄 Creating collection scan jobs for {SuccessCount} collections", result.SuccessCount);
                
                using var scope = _serviceProvider.CreateScope();
                var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
                var createdCollections = result.Results?.Where(r => r.Status == "Success" && r.CollectionId.HasValue).ToList() ?? new List<BulkCollectionResult>();
                
                foreach (var collectionResult in createdCollections)
                {
                    if (collectionResult.CollectionId.HasValue)
                    {
                        try
                        {
                            // Get the collection details
                            var collection = await collectionService.GetCollectionByIdAsync(collectionResult.CollectionId.Value);
                            if (collection != null)
                            {
                                // Create collection scan job
                                var scanMessage = new CollectionScanMessage
                                {
                                    CollectionId = collection.Id.ToString(), // Convert ObjectId to string
                                    CollectionPath = collection.Path,
                                    CollectionType = collection.Type,
                                    ForceRescan = false,
                                    CreatedBy = "BulkOperationConsumer",
                                    CreatedBySystem = "ImageViewer.Worker"
                                };

                                // Queue the scan job
                                await messageQueueService.PublishAsync(scanMessage, "collection.scan");
                                _logger.LogInformation("📋 Queued scan job for collection {CollectionId}: {CollectionName}", 
                                    collection.Id, collection.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Failed to create scan job for collection {CollectionId}", collectionResult.CollectionId);
                        }
                    }
                }
                
                _logger.LogInformation("✅ Created {ScanJobCount} collection scan jobs", createdCollections.Count);
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
        
        using var scope = _serviceProvider.CreateScope();
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
        
        using var scope = _serviceProvider.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
        
        // Get all images (we'll need to get them by collection since there's no GetAllImagesAsync)
        var images = new List<Domain.Entities.Image>();
        var collections = await collectionService.GetCollectionsAsync(page: 1, pageSize: 1000);
        foreach (var collection in collections)
        {
            var collectionImages = await imageService.GetByCollectionIdAsync(collection.Id);
            images.AddRange(collectionImages);
        }
        _logger.LogInformation("🖼️ Found {ImageCount} images for thumbnail generation", images.Count);
        
        // Create individual thumbnail generation jobs
        foreach (var image in images)
        {
            try
            {
                var thumbnailMessage = new ThumbnailGenerationMessage
                {
                    ImageId = image.Id.ToString(), // Convert ObjectId to string
                    CollectionId = image.CollectionId.ToString(), // Convert ObjectId to string
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
        
        _logger.LogInformation("✅ Created {ThumbnailJobCount} thumbnail generation jobs", images.Count());
    }

    private async Task ProcessGenerateAllCacheAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("💾 Processing generate all cache operation");
        
        using var scope = _serviceProvider.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
        
        // Get all images (we'll need to get them by collection since there's no GetAllImagesAsync)
        var images = new List<Domain.Entities.Image>();
        var collections = await collectionService.GetCollectionsAsync(page: 1, pageSize: 1000);
        foreach (var collection in collections)
        {
            var collectionImages = await imageService.GetByCollectionIdAsync(collection.Id);
            images.AddRange(collectionImages);
        }
        _logger.LogInformation("🖼️ Found {ImageCount} images for cache generation", images.Count);
        
        // Create individual cache generation jobs
        foreach (var image in images)
        {
            try
            {
                var cacheMessage = new CacheGenerationMessage
                {
                    ImageId = image.Id.ToString(), // Convert ObjectId to string
                    CollectionId = image.CollectionId.ToString(), // Convert ObjectId to string
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
        
        _logger.LogInformation("✅ Created {CacheJobCount} cache generation jobs", images.Count());
    }

    private async Task ProcessScanCollectionsAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("🔍 Processing scan collections operation for {CollectionIds}", 
            string.Join(", ", bulkMessage.CollectionIds));
        
        using var scope = _serviceProvider.CreateScope();
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
        
        using var scope = _serviceProvider.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        
        // Get images for specified collections
        var images = new List<Domain.Entities.Image>();
        foreach (var collectionId in bulkMessage.CollectionIds)
        {
            try
            {
                var collectionImages = await imageService.GetByCollectionIdAsync(ObjectId.Parse(collectionId.ToString()));
                images.AddRange(collectionImages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get images for collection {CollectionId}", collectionId);
            }
        }
        
        _logger.LogInformation("🖼️ Found {ImageCount} images for thumbnail generation", images.Count);
        
        // Create individual thumbnail generation jobs
        foreach (var image in images)
        {
            try
            {
                var thumbnailMessage = new ThumbnailGenerationMessage
                {
                    ImageId = image.Id.ToString(), // Convert ObjectId to string
                    CollectionId = image.CollectionId.ToString(), // Convert ObjectId to string
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
        
        _logger.LogInformation("✅ Created {ThumbnailJobCount} thumbnail generation jobs", images.Count);
    }

    private async Task ProcessGenerateCacheAsync(BulkOperationMessage bulkMessage, IMessageQueueService messageQueueService)
    {
        _logger.LogInformation("💾 Processing generate cache operation for {CollectionIds}", 
            string.Join(", ", bulkMessage.CollectionIds));
        
        using var scope = _serviceProvider.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
        
        // Get images for specified collections
        var images = new List<Domain.Entities.Image>();
        foreach (var collectionId in bulkMessage.CollectionIds)
        {
            try
            {
                var collectionImages = await imageService.GetByCollectionIdAsync(ObjectId.Parse(collectionId.ToString()));
                images.AddRange(collectionImages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get images for collection {CollectionId}", collectionId);
            }
        }
        
        _logger.LogInformation("🖼️ Found {ImageCount} images for cache generation", images.Count);
        
        // Create individual cache generation jobs
        foreach (var image in images)
        {
            try
            {
                var cacheMessage = new CacheGenerationMessage
                {
                    ImageId = image.Id.ToString(), // Convert ObjectId to string
                    CollectionId = image.CollectionId.ToString(), // Convert ObjectId to string
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
        
        _logger.LogInformation("✅ Created {CacheJobCount} cache generation jobs", images.Count);
    }
}
