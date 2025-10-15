using System.Text.Json;
using System.Collections.Concurrent;
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
/// Batch consumer for thumbnail generation messages
/// Collects messages, processes them in memory, then writes to disk and updates DB atomically
/// </summary>
public class BatchThumbnailGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly BatchProcessingOptions _batchOptions;
    
    // Batch collection - organized by collection ID to maintain consistency
    private readonly ConcurrentDictionary<string, ThumbnailCollectionBatch> _batchCollection;
    private readonly Timer _batchTimer;
    private readonly object _batchLock = new object();
    
    private int _totalProcessedCount = 0;
    private readonly object _counterLock = new object();

    public BatchThumbnailGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        IOptions<BatchProcessingOptions> batchOptions,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BatchThumbnailGenerationConsumer> logger)
        : base(connection, rabbitMQOptions, logger, "thumbnail.generation", "batch-thumbnail-generation-consumer")
    {
        try
        {
            logger.LogInformation("🔧 BatchThumbnailGenerationConsumer constructor starting...");
            
            _serviceScopeFactory = serviceScopeFactory;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _batchOptions = batchOptions.Value;
            _batchCollection = new ConcurrentDictionary<string, ThumbnailCollectionBatch>();
            
            // Timer to flush batches periodically (every 5 seconds)
            _batchTimer = new Timer(FlushAllBatches, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            logger.LogInformation("🚀 BatchThumbnailGenerationConsumer initialized successfully - Batch size: {BatchSize}, Timeout: {Timeout}s", 
                _batchOptions.MaxBatchSize, _batchOptions.BatchTimeoutSeconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to initialize BatchThumbnailGenerationConsumer");
            throw;
        }
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📦 Received thumbnail generation message: {Message}", message);
            
            // Check if service provider is disposed (shutdown protection)
            try
            {
                _ = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping batch thumbnail processing.");
                return;
            }
            
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

            // Add message to batch for processing
            await AddToBatchAsync(thumbnailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing thumbnail generation message: {Message}", message);
        }
    }

    private Task AddToBatchAsync(ThumbnailGenerationMessage message)
    {
        var collectionId = message.CollectionId;
        
        // Get or create batch for this collection
        var batch = _batchCollection.GetOrAdd(collectionId, _ => new ThumbnailCollectionBatch(collectionId));
        
        lock (batch.Lock)
        {
            batch.Messages.Add(message);
            batch.LastAddedTime = DateTime.UtcNow;
            
            _logger.LogInformation("📦 Added message to batch for collection {CollectionId}, batch size: {Size}/{MaxSize}", 
                collectionId, batch.Messages.Count, _batchOptions.MaxBatchSize);
            
            // Check if batch is ready to process
            if (batch.Messages.Count >= _batchOptions.MaxBatchSize)
            {
                _logger.LogInformation("📦 Batch for collection {CollectionId} reached max size ({Size}), processing immediately", 
                    collectionId, _batchOptions.MaxBatchSize);
                
                // Process this batch immediately
                _ = Task.Run(() => ProcessBatchAsync(batch), CancellationToken.None);
            }
        }
        
        return Task.CompletedTask;
    }

    private async void FlushAllBatches(object? state)
    {
        try
        {
            var batchesToProcess = new List<ThumbnailCollectionBatch>();
            var cutoffTime = DateTime.UtcNow.AddSeconds(-_batchOptions.BatchTimeoutSeconds);
            
            // Collect batches that need to be flushed
            foreach (var kvp in _batchCollection)
            {
                var batch = kvp.Value;
                lock (batch.Lock)
                {
                    if (batch.Messages.Count > 0 && batch.LastAddedTime < cutoffTime)
                    {
                        batchesToProcess.Add(batch);
                    }
                }
            }
            
            // Process all ready batches
            foreach (var batch in batchesToProcess)
            {
                await ProcessBatchAsync(batch);
            }
            
            if (batchesToProcess.Count > 0)
            {
                _logger.LogInformation("⏰ Flushed {Count} batches due to timeout", batchesToProcess.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in batch flush timer");
        }
    }

    private async Task ProcessBatchAsync(ThumbnailCollectionBatch batch)
    {
        var collectionId = batch.CollectionId;
        List<ThumbnailGenerationMessage> messagesToProcess;
        
        // Extract messages from batch atomically
        lock (batch.Lock)
        {
            if (batch.Messages.Count == 0)
            {
                return; // Already processed or empty
            }
            
            messagesToProcess = new List<ThumbnailGenerationMessage>(batch.Messages);
            batch.Messages.Clear();
            batch.Processing = true;
        }
        
        // Remove batch from collection while processing
        _batchCollection.TryRemove(collectionId, out _);
        
        try
        {
            _logger.LogInformation("🚀 Processing batch of {Count} thumbnails for collection {CollectionId}", 
                messagesToProcess.Count, collectionId);
            
            await ProcessBatchMessagesAsync(collectionId, messagesToProcess);
            
            _logger.LogInformation("✅ Successfully processed batch of {Count} thumbnails for collection {CollectionId}", 
                messagesToProcess.Count, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing batch for collection {CollectionId}", collectionId);
            
            // Mark all messages in this batch as failed to avoid endless retries
            await MarkMessagesAsFailedAsync(messagesToProcess);
        }
    }

    private async Task ProcessBatchMessagesAsync(string collectionId, List<ThumbnailGenerationMessage> messages)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        
        var imageProcessingService = serviceProvider.GetRequiredService<IImageProcessingService>();
        var collectionRepository = serviceProvider.GetRequiredService<ICollectionRepository>();
        var settingsService = serviceProvider.GetRequiredService<IImageProcessingSettingsService>();
        var cacheService = serviceProvider.GetRequiredService<ICacheService>();
        var jobStateRepository = serviceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
        
        // Get settings once for the entire batch
        var format = await settingsService.GetThumbnailFormatAsync();
        var quality = await settingsService.GetThumbnailQualityAsync();
        var collectionObjectId = ObjectId.Parse(collectionId);
        
        // Update job status to "Running" for all jobs in this batch
        await UpdateJobStatusToRunningAsync(jobStateRepository, messages);
        
        // Step 1: Process all images in memory first (no disk I/O yet)
        var processedImages = new List<ProcessedThumbnailData>();
        var failedImages = new List<ThumbnailGenerationMessage>();
        
        _logger.LogInformation("🎨 Processing {Count} images in memory for collection {CollectionId}", 
            messages.Count, collectionId);
        
        foreach (var message in messages)
        {
            try
            {
                // Skip if already exists
                if (await ThumbnailAlreadyExistsAsync(collectionRepository, message, collectionObjectId))
                {
                    _logger.LogDebug("⏭️ Thumbnail already exists for image {ImageId}, skipping", message.ImageId);
                    continue;
                }
                
                // Validate file size before processing
                if (!await ValidateFileSizeAsync(message, jobStateRepository))
                {
                    failedImages.Add(message);
                    continue;
                }
                
                // Process image in memory with smart quality adjustment
                var processedData = await ProcessImageInMemoryAsync(imageProcessingService, message, format, quality);
                if (processedData != null)
                {
                    processedImages.Add(processedData);
                }
                else
                {
                    failedImages.Add(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to process image {ImageId} in memory", message.ImageId);
                await TrackErrorAsync(jobStateRepository, message.JobId, ex);
                failedImages.Add(message);
            }
        }
        
        if (processedImages.Count == 0)
        {
            _logger.LogWarning("⚠️ No images processed successfully for collection {CollectionId}", collectionId);
            return;
        }
        
        // Step 2: Write all thumbnails to disk in organized batches
        _logger.LogInformation("💾 Writing {Count} thumbnails to disk for collection {CollectionId}", 
            processedImages.Count, collectionId);
        
        var thumbnailPaths = await WriteThumbnailsToDiskAsync(processedImages, collectionObjectId, cacheService, format);
        
        // Step 3: Update database atomically for this collection
        _logger.LogInformation("📝 Updating database for collection {CollectionId} with {Count} thumbnails", 
            collectionId, thumbnailPaths.Count);
        
        await UpdateDatabaseAtomicallyAsync(collectionRepository, collectionObjectId, thumbnailPaths, messages);
        
        // Step 4: Update job progress and statistics
        await UpdateJobProgressAsync(serviceProvider, messages, processedImages, failedImages);
        
        // Update counters
        lock (_counterLock)
        {
            _totalProcessedCount += processedImages.Count;
        }
        
        _logger.LogInformation("✅ Batch processing complete for collection {CollectionId}: {Success}/{Total} successful", 
            collectionId, processedImages.Count, messages.Count);
    }

    private async Task<ProcessedThumbnailData?> ProcessImageInMemoryAsync(
        IImageProcessingService imageProcessingService,
        ThumbnailGenerationMessage message,
        string format,
        int quality)
    {
        try
        {
            byte[] thumbnailData;
            
            // Handle ZIP entries using DTO
            if (message.ArchiveEntry != null)
            {
                var imageBytes = await ArchiveFileHelper.ExtractArchiveEntryBytes(message.ArchiveEntry.GetFullPath(), null);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogWarning("❌ Failed to extract archive entry: {Path}", message.ArchiveEntry.GetFullPath());
                    return null;
                }
                
                thumbnailData = await imageProcessingService.GenerateThumbnailFromBytesAsync(
                    imageBytes, message.ThumbnailWidth, message.ThumbnailHeight, format, quality);
            }
            else
            {
                // This should not happen - ArchiveEntry should always be provided
                _logger.LogError("❌ No ArchiveEntry provided for thumbnail generation");
                return null;
            }
            
            if (thumbnailData == null || thumbnailData.Length == 0)
            {
                _logger.LogWarning("⚠️ No thumbnail data generated for image {ImageId}", message.ImageId);
                return null;
            }
            
            return new ProcessedThumbnailData
            {
                Message = message,
                ThumbnailData = thumbnailData,
                Size = thumbnailData.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing image {ImageId} in memory", message.ImageId);
            return null;
        }
    }

    private async Task<List<ThumbnailPathData>> WriteThumbnailsToDiskAsync(
        List<ProcessedThumbnailData> processedImages,
        ObjectId collectionId,
        ICacheService cacheService,
        string format)
    {
        var thumbnailPaths = new List<ThumbnailPathData>();
        
        // Get cache folder for this collection (consistent selection)
        var cacheFolders = await cacheService.GetCacheFoldersAsync();
        var cacheFoldersList = cacheFolders.ToList();
        var selectedIndex = Math.Abs(collectionId.GetHashCode()) % cacheFoldersList.Count;
        var selectedCacheFolder = cacheFoldersList[selectedIndex];
        
        // Create collection directory
        var collectionDir = Path.Combine(selectedCacheFolder.Path, "thumbnails", collectionId.ToString());
        Directory.CreateDirectory(collectionDir);
        
        // Write all thumbnails to disk
        foreach (var processedImage in processedImages)
        {
            var thumbnailPath = GetThumbnailPath(
                processedImage.Message,
                processedImage.Message.ThumbnailWidth,
                processedImage.Message.ThumbnailHeight,
                collectionId,
                format,
                collectionDir);
            
            await File.WriteAllBytesAsync(thumbnailPath, processedImage.ThumbnailData);
            
            // Update cache folder statistics after successful file write
            await UpdateCacheFolderStatisticsAsync(thumbnailPath, processedImage.Size, processedImage.Message.CollectionId, serviceProvider);
            
            thumbnailPaths.Add(new ThumbnailPathData
            {
                ImageId = processedImage.Message.ImageId,
                ThumbnailPath = thumbnailPath,
                Size = processedImage.Size
            });
        }
        
        return thumbnailPaths;
    }

    /// <summary>
    /// Update cache folder statistics after successful thumbnail generation
    /// </summary>
    private async Task UpdateCacheFolderStatisticsAsync(string thumbnailPath, long fileSize, string collectionId, IServiceProvider serviceProvider)
    {
        try
        {
            // Get cache folder ID directly from the cache folder selection instead of guessing from path
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();
            var cacheFolders = await cacheService.GetCacheFoldersAsync();
            
            // Select the same cache folder that was used for this collection
            var collectionObjectId = ObjectId.Parse(collectionId);
            var selectedCacheFolder = SelectCacheFolderForEqualDistribution(cacheFolders, collectionObjectId);
            
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheFolderRepository = scope.ServiceProvider.GetRequiredService<ICacheFolderRepository>();
            
            // ATOMIC INCREMENT: Thread-safe update in SINGLE transaction using the known cache folder ID
            await cacheFolderRepository.IncrementCacheStatisticsAsync(selectedCacheFolder.Id, fileSize, 1);
            await cacheFolderRepository.AddCachedCollectionAsync(selectedCacheFolder.Id, collectionId);
            
            _logger.LogDebug("📊 Atomically incremented cache folder {Name} (ID: {Id}) size by {Size} bytes, file count by 1 (single transaction)", 
                selectedCacheFolder.Name, selectedCacheFolder.Id, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to update cache folder statistics for thumbnail: {Path}", thumbnailPath);
            // Don't throw - thumbnail is already saved, this is just statistics
        }
    }

    private async Task UpdateDatabaseAtomicallyAsync(
        ICollectionRepository collectionRepository,
        ObjectId collectionId,
        List<ThumbnailPathData> thumbnailPaths,
        List<ThumbnailGenerationMessage> originalMessages)
    {
        try
        {
            // Get collection
            var collection = await collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection {collectionId} not found");
            }
            
            // Create thumbnail embedded objects
            var thumbnailsToAdd = new List<ThumbnailEmbedded>();
            
            foreach (var thumbnailPath in thumbnailPaths)
            {
                var originalMessage = originalMessages.FirstOrDefault(m => m.ImageId == thumbnailPath.ImageId);
                if (originalMessage != null)
                {
                    var thumbnailEmbedded = new ThumbnailEmbedded(
                        originalMessage.ImageId,
                        thumbnailPath.ThumbnailPath,
                        originalMessage.ThumbnailWidth,
                        originalMessage.ThumbnailHeight,
                        thumbnailPath.Size,
                        Path.GetExtension(thumbnailPath.ThumbnailPath).TrimStart('.').ToUpperInvariant(),
                        95 // quality
                    );
                    
                    thumbnailsToAdd.Add(thumbnailEmbedded);
                }
            }
            
            // Add all thumbnails atomically in a single operation
            await collectionRepository.AtomicAddThumbnailsAsync(collectionId, thumbnailsToAdd);
            
            _logger.LogInformation("✅ Added {Count} thumbnails to collection {CollectionId} atomically", 
                thumbnailsToAdd.Count, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating database for collection {CollectionId}", collectionId);
            throw;
        }
    }

    private async Task<bool> ThumbnailAlreadyExistsAsync(
        ICollectionRepository collectionRepository,
        ThumbnailGenerationMessage message,
        ObjectId collectionId)
    {
        try
        {
            var collection = await collectionRepository.GetByIdAsync(collectionId);
            if (collection == null) return false;
            
            var existingThumbnail = collection.Thumbnails?.FirstOrDefault(t =>
                t.ImageId == message.ImageId &&
                t.Width == message.ThumbnailWidth &&
                t.Height == message.ThumbnailHeight);
            
            // Check if thumbnail exists in collection AND on disk
            if (existingThumbnail != null && File.Exists(existingThumbnail.ThumbnailPath))
            {
                return true;
            }
            
            // CRITICAL: Resume Incomplete Logic - Check if thumbnail file exists on disk but NOT in collection array
            // This handles Resume Incomplete scenario where thumbnail array was cleared but disk files still exist
            if (existingThumbnail == null)
            {
                var thumbnailPath = await GetThumbnailPathForResumeCheck(
                    message,
                    message.ThumbnailWidth,
                    message.ThumbnailHeight,
                    collectionId);
                
                if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
                {
                    _logger.LogInformation("📝 Resume Incomplete: Thumbnail file exists on disk but not in collection for image {ImageId}, will re-add", message.ImageId);
                    
                    // Re-add the existing thumbnail file to collection
                    var fileInfo = new FileInfo(thumbnailPath);
                    var thumbnailEmbedded = new ThumbnailEmbedded(
                        message.ImageId,
                        thumbnailPath,
                        message.ThumbnailWidth,
                        message.ThumbnailHeight,
                        fileInfo.Length,
                        fileInfo.Extension.TrimStart('.').ToUpperInvariant(),
                        95 // quality
                    );
                    
                    await collectionRepository.AtomicAddThumbnailAsync(collectionId, thumbnailEmbedded);
                    return true; // Skip processing since we just re-added it
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error checking if thumbnail exists for image {ImageId}", message.ImageId);
            return false;
        }
    }

    private async Task UpdateJobProgressAsync(
        IServiceProvider serviceProvider,
        List<ThumbnailGenerationMessage> messages,
        List<ProcessedThumbnailData> processedImages,
        List<ThumbnailGenerationMessage> failedImages)
    {
        try
        {
            // Update job progress for successful thumbnails
            var backgroundJobService = serviceProvider.GetRequiredService<IBackgroundJobService>();
            var jobStateRepository = serviceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
            
            foreach (var processedImage in processedImages)
            {
                var message = processedImage.Message;
                if (!string.IsNullOrEmpty(message.ScanJobId))
                {
                    await backgroundJobService.IncrementJobStageProgressAsync(
                        ObjectId.Parse(message.ScanJobId), "thumbnail", incrementBy: 1);
                }
                
                if (!string.IsNullOrEmpty(message.JobId))
                {
                    await jobStateRepository.AtomicIncrementCompletedAsync(
                        message.JobId, message.ImageId, 0);
                }
            }
            
            // Update job progress for failed thumbnails - mark them as done to avoid retrying
            foreach (var message in failedImages)
            {
                if (!string.IsNullOrEmpty(message.ScanJobId))
                {
                    await backgroundJobService.IncrementJobStageProgressAsync(
                        ObjectId.Parse(message.ScanJobId), "thumbnail", incrementBy: 1);
                }
                
                if (!string.IsNullOrEmpty(message.JobId))
                {
                    await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                }
                
                _logger.LogDebug("✅ Marked failed thumbnail image {ImageId} as done to avoid retrying", message.ImageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error updating job progress");
        }
    }

    private string GetThumbnailPath(
        ThumbnailGenerationMessage message,
        int width,
        int height,
        ObjectId collectionId,
        string format,
        string collectionDir)
    {
        var extension = format.ToLowerInvariant() switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg"
        };
        
        string fileName;
        // Use DTO structure to avoid legacy string splitting bugs
        if (message.ArchiveEntry != null)
        {
            // This is an archive entry - extract the entry name from DTO
            fileName = Path.GetFileNameWithoutExtension(message.ArchiveEntry.EntryName);
        }
        else
        {
            // This should not happen - ArchiveEntry should always be provided
            _logger.LogError("❌ No ArchiveEntry provided for thumbnail path generation");
            fileName = "unknown";
        }
        
        var thumbnailFileName = $"{fileName}_{width}x{height}{extension}";
        return Path.Combine(collectionDir, thumbnailFileName);
    }

    private async Task<string> GetThumbnailPathForResumeCheck(
        ThumbnailGenerationMessage message,
        int width,
        int height,
        ObjectId collectionId)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var settingsService = scope.ServiceProvider.GetRequiredService<IImageProcessingSettingsService>();
            
            var format = await settingsService.GetThumbnailFormatAsync();
            var cacheFolders = await cacheService.GetCacheFoldersAsync();
            var cacheFoldersList = cacheFolders.ToList();
            
            if (cacheFoldersList.Count == 0)
            {
                return string.Empty;
            }
            
            // Use same hash-based distribution as the main logic
            var hash = collectionId.GetHashCode();
            var selectedIndex = Math.Abs(hash) % cacheFoldersList.Count;
            var selectedCacheFolder = cacheFoldersList[selectedIndex];
            
            var collectionDir = Path.Combine(selectedCacheFolder.Path, "thumbnails", collectionId.ToString());
            
            var extension = format.ToLowerInvariant() switch
            {
                "jpeg" => ".jpg",
                "jpg" => ".jpg", 
                "png" => ".png",
                "webp" => ".webp",
                _ => ".jpg"
            };
            
            string fileName;
            // Use DTO structure to avoid legacy string splitting bugs
            if (message.ArchiveEntry != null)
            {
                // This is an archive entry - extract the entry name from DTO
                fileName = Path.GetFileNameWithoutExtension(message.ArchiveEntry.EntryName);
            }
            else
            {
                // This should not happen - ArchiveEntry should always be provided
                _logger.LogError("❌ No ArchiveEntry provided for thumbnail path generation");
                fileName = "unknown";
            }
            
            var thumbnailFileName = $"{fileName}_{width}x{height}{extension}";
            return Path.Combine(collectionDir, thumbnailFileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error getting thumbnail path for resume check");
            return string.Empty;
        }
    }

    /// <summary>
    /// Updates job status to "Running" for all jobs in the batch
    /// </summary>
    private async Task UpdateJobStatusToRunningAsync(IFileProcessingJobStateRepository jobStateRepository, List<ThumbnailGenerationMessage> messages)
    {
        try
        {
            var uniqueJobIds = messages.Where(m => !string.IsNullOrEmpty(m.JobId)).Select(m => m.JobId).Distinct();
            foreach (var jobId in uniqueJobIds)
            {
                await jobStateRepository.UpdateStatusAsync(jobId, "Running");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to update job status to Running");
        }
    }

    /// <summary>
    /// Validates file size before processing to prevent OOM
    /// </summary>
    private async Task<bool> ValidateFileSizeAsync(ThumbnailGenerationMessage message, IFileProcessingJobStateRepository jobStateRepository)
    {
        try
        {
            long fileSize = 0;
            long maxSize = 0;
            
            if (message.ArchiveEntry != null)
            {
                fileSize = ArchiveFileHelper.GetArchiveEntrySize(message.ArchiveEntry.GetFullPath(), _logger);
                maxSize = _rabbitMQOptions.MaxZipEntrySizeBytes; // 20GB for archive entries
                
                if (fileSize > maxSize)
                {
                    _logger.LogWarning("⚠️ Archive entry too large ({SizeGB}GB), skipping thumbnail generation for {ImageId}", 
                        fileSize / 1024.0 / 1024.0 / 1024.0, message.ImageId);
                    
                    await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                    return false;
                }
            }
            else
            {
                // This should not happen - ArchiveEntry should always be provided
                _logger.LogError("❌ No ArchiveEntry provided for thumbnail generation");
                return false;
            }
            
            if (fileSize > maxSize)
            {
                _logger.LogWarning("⚠️ Image file too large ({SizeMB}MB), skipping thumbnail generation for {ImageId}", 
                    fileSize / 1024.0 / 1024.0, message.ImageId);
                    
                await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error validating file size for {ImageId}", message.ImageId);
            return false;
        }
    }

    /// <summary>
    /// Tracks error types for statistics
    /// </summary>
    private async Task TrackErrorAsync(IFileProcessingJobStateRepository jobStateRepository, string? jobId, Exception ex)
    {
        try
        {
            if (!string.IsNullOrEmpty(jobId))
            {
                await jobStateRepository.TrackErrorAsync(jobId, ex.GetType().Name);
                
                // Check failure threshold and alert if needed (every 10 failures)
                var jobState = await jobStateRepository.GetByJobIdAsync(jobId);
                if (jobState != null && jobState.FailedImages % 10 == 0)
                {
                    _logger.LogWarning("⚠️ Job {JobId} has {FailedImages} failures, consider investigating", 
                        jobId, jobState.FailedImages);
                }
            }
        }
        catch (Exception trackEx)
        {
            _logger.LogWarning(trackEx, "⚠️ Failed to track error for job {JobId}", jobId);
        }
    }

    public new void Dispose()
    {
        _batchTimer?.Dispose();
        
        // Process any remaining batches before shutdown
        foreach (var batch in _batchCollection.Values)
        {
            if (batch.Messages.Count > 0)
            {
                ProcessBatchAsync(batch).Wait(TimeSpan.FromSeconds(30));
            }
        }
        
        base.Dispose();
    }

    /// <summary>
    /// Mark failed messages as done to avoid endless retries
    /// </summary>
    private async Task MarkMessagesAsFailedAsync(List<ThumbnailGenerationMessage> failedMessages)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var backgroundJobService = serviceProvider.GetRequiredService<IBackgroundJobService>();
            var jobStateRepository = serviceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
            
            foreach (var message in failedMessages)
            {
                // Mark as completed in scan job (so it doesn't retry)
                if (!string.IsNullOrEmpty(message.ScanJobId))
                {
                    await backgroundJobService.IncrementJobStageProgressAsync(
                        ObjectId.Parse(message.ScanJobId), "thumbnail", incrementBy: 1);
                }
                
                // Mark as failed in job state (so it's counted as processed)
                if (!string.IsNullOrEmpty(message.JobId))
                {
                    await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                }
                
                _logger.LogDebug("✅ Marked failed thumbnail image {ImageId} as done to avoid retrying", message.ImageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error marking failed thumbnail messages as done");
        }
    }
}

/// <summary>
/// Configuration for batch processing
/// </summary>
public class BatchProcessingOptions
{
    public int MaxBatchSize { get; set; } = 50; // Process up to 50 thumbnails at once
    public int BatchTimeoutSeconds { get; set; } = 5; // Flush batch after 5 seconds
    public int MaxConcurrentBatches { get; set; } = 4; // Process up to 4 collections simultaneously
}


/// <summary>
/// Data for a processed thumbnail
/// </summary>
public class ProcessedThumbnailData
{
    public ThumbnailGenerationMessage Message { get; set; } = null!;
    public byte[] ThumbnailData { get; set; } = null!;
    public long Size { get; set; }
}

/// <summary>
/// Data for thumbnail path information
/// </summary>
public class ThumbnailPathData
{
    public string ImageId { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public long Size { get; set; }
}
