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
using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Data;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Batch consumer for cache generation messages
/// Collects messages, processes them in memory, then writes to disk and updates DB atomically
/// </summary>
public class BatchCacheGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly BatchProcessingOptions _batchOptions;
    
    // Batch collection - organized by collection ID to maintain consistency
    private readonly ConcurrentDictionary<string, CacheCollectionBatch> _batchCollection;
    private readonly Timer _batchTimer;
    private readonly object _batchLock = new object();
    
    private int _totalProcessedCount = 0;
    private readonly object _counterLock = new object();

    public BatchCacheGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        IOptions<BatchProcessingOptions> batchOptions,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BatchCacheGenerationConsumer> logger)
        : base(connection, rabbitMQOptions, logger, "cache.generation", "batch-cache-generation-consumer")
    {
        try
        {
            logger.LogInformation("🔧 BatchCacheGenerationConsumer constructor starting...");
            
            _serviceScopeFactory = serviceScopeFactory;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _batchOptions = batchOptions.Value;
            _batchCollection = new ConcurrentDictionary<string, CacheCollectionBatch>();
            
            // Timer to flush batches periodically (every 5 seconds)
            _batchTimer = new Timer(FlushAllBatches, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            logger.LogInformation("🚀 BatchCacheGenerationConsumer initialized successfully - Batch size: {BatchSize}, Timeout: {Timeout}s", 
                _batchOptions.MaxBatchSize, _batchOptions.BatchTimeoutSeconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to initialize BatchCacheGenerationConsumer");
            throw;
        }
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📦 Received cache generation message: {Message}", message);
            
            // Check if service provider is disposed (shutdown protection)
            try
            {
                _ = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping batch cache processing.");
                return;
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var cacheMessage = JsonSerializer.Deserialize<CacheGenerationMessage>(message, options);
            if (cacheMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize CacheGenerationMessage from: {Message}", message);
                return;
            }

            // Add message to batch for processing
            await AddToBatchAsync(cacheMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing cache generation message: {Message}", message);
        }
    }

    private Task AddToBatchAsync(CacheGenerationMessage message)
    {
        var collectionId = message.CollectionId;
        
        // Get or create batch for this collection
        var batch = _batchCollection.GetOrAdd(collectionId, _ => new CacheCollectionBatch(collectionId));
        
        lock (batch.Lock)
        {
            batch.Messages.Add(message);
            batch.LastAddedTime = DateTime.UtcNow;
            
            _logger.LogInformation("📦 Added cache message to batch for collection {CollectionId}, batch size: {Size}/{MaxSize}", 
                collectionId, batch.Messages.Count, _batchOptions.MaxBatchSize);
            
            // Check if batch is ready to process
            if (batch.Messages.Count >= _batchOptions.MaxBatchSize)
            {
                _logger.LogInformation("📦 Cache batch for collection {CollectionId} reached max size ({Size}), processing immediately", 
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
            var batchesToProcess = new List<CacheCollectionBatch>();
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
                _logger.LogInformation("⏰ Flushed {Count} cache batches due to timeout", batchesToProcess.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in cache batch flush timer");
        }
    }

    private async Task ProcessBatchAsync(CacheCollectionBatch batch)
    {
        var collectionId = batch.CollectionId;
        List<CacheGenerationMessage> messagesToProcess;
        
        // Extract messages from batch atomically
        lock (batch.Lock)
        {
            if (batch.Messages.Count == 0)
            {
                return; // Already processed or empty
            }
            
            messagesToProcess = new List<CacheGenerationMessage>(batch.Messages);
            batch.Messages.Clear();
            batch.Processing = true;
        }
        
        // Remove batch from collection while processing
        _batchCollection.TryRemove(collectionId, out _);
        
        try
        {
            _logger.LogInformation("🚀 Processing batch of {Count} cache images for collection {CollectionId}", 
                messagesToProcess.Count, collectionId);
            
            await ProcessBatchCacheMessagesAsync(collectionId, messagesToProcess);
            
            _logger.LogInformation("✅ Successfully processed batch of {Count} cache images for collection {CollectionId}", 
                messagesToProcess.Count, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing cache batch for collection {CollectionId}", collectionId);
            
            // Mark all messages in this batch as failed to avoid endless retries
            await MarkMessagesAsFailedAsync(messagesToProcess);
        }
    }

    private async Task ProcessBatchCacheMessagesAsync(string collectionId, List<CacheGenerationMessage> messages)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        
        var imageProcessingService = serviceProvider.GetRequiredService<IImageProcessingService>();
        var collectionRepository = serviceProvider.GetRequiredService<ICollectionRepository>();
        var settingsService = serviceProvider.GetRequiredService<IImageProcessingSettingsService>();
        var jobStateRepository = serviceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
        
        // Load quality settings from database (not from message defaults)
        var cacheFormat = await settingsService.GetCacheFormatAsync();
        var cacheQuality = await settingsService.GetCacheQualityAsync();
        
        _logger.LogInformation("🔧 BatchCacheGenerationConsumer: Loaded settings - Format: {Format}, Quality: {Quality}", 
            cacheFormat, cacheQuality);
        
        var collectionObjectId = ObjectId.Parse(collectionId);
        
        // Update job status to "Running" for all jobs in this batch
        await UpdateJobStatusToRunningAsync(jobStateRepository, messages);
        
        // Step 1: Validate file sizes and process all cache images in memory first (no disk I/O yet)
        var processedImages = new List<ProcessedCacheData>();
        var failedImages = new List<CacheGenerationMessage>();
        
        _logger.LogInformation("🎨 Processing {Count} cache images in memory for collection {CollectionId}", 
            messages.Count, collectionId);
        
        foreach (var message in messages)
        {
            try
            {
                // Skip if already exists
                if (await CacheAlreadyExistsAsync(collectionRepository, message, collectionObjectId))
                {
                    _logger.LogDebug("⏭️ Cache already exists for image {ImageId}, skipping", message.ImageId);
                    continue;
                }
                
                // Validate file size before processing
                if (!await ValidateFileSizeAsync(message, jobStateRepository))
                {
                    failedImages.Add(message);
                    continue;
                }
                
                // Process cache image in memory with smart quality adjustment
                _logger.LogDebug("🎨 Processing cache for image {ImageId} with format: {Format}, quality: {Quality}", 
                    message.ImageId, cacheFormat, cacheQuality);
                    
                var processedData = await ProcessCacheImageInMemoryAsync(
                    imageProcessingService, 
                    message, 
                    cacheFormat,
                    cacheQuality);
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
                _logger.LogError(ex, "❌ Failed to process cache image {ImageId} in memory", message.ImageId);
                await TrackErrorAsync(jobStateRepository, message.JobId, ex);
                failedImages.Add(message);
            }
        }
        
        if (processedImages.Count == 0)
        {
            _logger.LogWarning("⚠️ No cache images processed successfully for collection {CollectionId}", collectionId);
            return;
        }
        
        // Step 2: Write all cache images to disk in organized batches
        _logger.LogInformation("💾 Writing {Count} cache images to disk for collection {CollectionId} with format: {Format}", 
            processedImages.Count, collectionId, cacheFormat);
        
        var cachePaths = await WriteCacheImagesToDiskAsync(processedImages, collectionObjectId, cacheFormat, serviceProvider);
        
        // Step 2.5: Update cache folder sizes (commented out - not available in ICacheService)
        // await UpdateCacheFolderSizesAsync(serviceProvider, cachePaths);
        
        // Step 3: Update database atomically for this collection
        _logger.LogInformation("📝 Updating database for collection {CollectionId} with {Count} cache images", 
            collectionId, cachePaths.Count);
        
        await UpdateDatabaseAtomicallyAsync(collectionRepository, collectionObjectId, cachePaths, messages, cacheFormat);
        
        // Step 4: Update job progress and statistics
        await UpdateJobProgressAsync(serviceProvider, messages, processedImages, failedImages);
        
        // Update counters
        lock (_counterLock)
        {
            _totalProcessedCount += processedImages.Count;
        }
        
        _logger.LogInformation("✅ Cache batch processing complete for collection {CollectionId}: {Success}/{Total} successful", 
            collectionId, processedImages.Count, messages.Count);
    }

    private async Task<ProcessedCacheData?> ProcessCacheImageInMemoryAsync(
        IImageProcessingService imageProcessingService,
        CacheGenerationMessage message,
        string cacheFormat,
        int cacheQuality)
    {
        try
        {
            byte[] cacheData;
            
            // Smart quality adjustment: avoid degrading low-quality source images
            int adjustedQuality = await DetermineOptimalCacheQuality(
                message, 
                imageProcessingService, 
                cacheFormat, 
                cacheQuality);
                
            _logger.LogDebug("🎨 ProcessCacheImageInMemoryAsync: Using format {Format}, quality {Quality} (adjusted from {OriginalQuality})", 
                cacheFormat, adjustedQuality, cacheQuality);
            
            // Handle archive entries using DTO
            if (message.ArchiveEntry != null)
            {
                var imageBytes = await ArchiveFileHelper.ExtractArchiveEntryBytes(message.ArchiveEntry.GetFullPath(), null);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogWarning("❌ Failed to extract archive entry: {Path}", message.ArchiveEntry.GetFullPath());
                    return null;
                }
                
                cacheData = await imageProcessingService.GenerateCacheFromBytesAsync(
                    imageBytes, message.CacheWidth, message.CacheHeight, cacheFormat, adjustedQuality);
            }
            else
            {
                // This should not happen - ArchiveEntry should always be provided
                _logger.LogError("❌ No ArchiveEntry provided for cache generation");
                return null;
            }
            
            if (cacheData == null || cacheData.Length == 0)
            {
                _logger.LogWarning("⚠️ No cache data generated for image {ImageId}", message.ImageId);
                return null;
            }
            
            return new ProcessedCacheData
            {
                Message = message,
                CacheData = cacheData,
                Size = cacheData.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing cache image {ImageId} in memory", message.ImageId);
            return null;
        }
    }

    private async Task<List<CachePathData>> WriteCacheImagesToDiskAsync(
        List<ProcessedCacheData> processedImages,
        ObjectId collectionId,
        string cacheFormat,
        IServiceProvider serviceProvider)
    {
        var cachePaths = new List<CachePathData>();
        
        // Determine cache paths for all messages if not set
        // CRITICAL: Use the same format that was used for processing, not the message format
        foreach (var processedImage in processedImages)
        {
            if (string.IsNullOrEmpty(processedImage.Message.CachePath))
            {
                // Override the message format with the correct format from settings
                processedImage.Message.Format = cacheFormat;
                processedImage.Message.CachePath = await DetermineCachePath(processedImage.Message, serviceProvider);
            }
        }
        
        // Use the first message's cache path to determine the directory
        var firstMessage = processedImages.First().Message;
        var cacheDir = Path.GetDirectoryName(firstMessage.CachePath);
        
        if (string.IsNullOrEmpty(cacheDir))
        {
            throw new InvalidOperationException("Cache directory path is empty");
        }
        
        // Ensure cache directory exists
        Directory.CreateDirectory(cacheDir);
        
        // Write all cache images to disk
        foreach (var processedImage in processedImages)
        {
            var cachePath = processedImage.Message.CachePath;
            
            _logger.LogDebug("💾 Writing cache file: {CachePath} (Size: {Size} bytes)", 
                cachePath, processedImage.CacheData.Length);
            
            await File.WriteAllBytesAsync(cachePath, processedImage.CacheData);
            
            cachePaths.Add(new CachePathData
            {
                ImageId = processedImage.Message.ImageId,
                CachePath = cachePath,
                Size = processedImage.Size
            });
        }
        
        return cachePaths;
    }

    private async Task UpdateDatabaseAtomicallyAsync(
        ICollectionRepository collectionRepository,
        ObjectId collectionId,
        List<CachePathData> cachePaths,
        List<CacheGenerationMessage> originalMessages,
        string cacheFormat)
    {
        try
        {
            // Get collection
            var collection = await collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection {collectionId} not found");
            }
            
            // Create cache embedded objects
            var cachesToAdd = new List<CacheImageEmbedded>();
            
            foreach (var cachePath in cachePaths)
            {
                var originalMessage = originalMessages.FirstOrDefault(m => m.ImageId == cachePath.ImageId);
                if (originalMessage != null)
                {
                    var cacheEmbedded = new CacheImageEmbedded(
                        originalMessage.ImageId,
                        cachePath.CachePath,
                        originalMessage.CacheWidth,
                        originalMessage.CacheHeight,
                        cachePath.Size,
                        cacheFormat.ToUpperInvariant(),
                        originalMessage.Quality
                    );
                    
                    cachesToAdd.Add(cacheEmbedded);
                }
            }
            
            // Add all cache images atomically in a single operation
            await collectionRepository.AtomicAddCacheImagesAsync(collectionId, cachesToAdd);
            
            _logger.LogInformation("✅ Added {Count} cache images to collection {CollectionId} atomically", 
                cachesToAdd.Count, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating database for collection {CollectionId}", collectionId);
            throw;
        }
    }

    private async Task<bool> CacheAlreadyExistsAsync(
        ICollectionRepository collectionRepository,
        CacheGenerationMessage message,
        ObjectId collectionId)
    {
        try
        {
            // Check if cache file exists on disk
            if (File.Exists(message.CachePath))
            {
                _logger.LogDebug("📁 Cache file already exists on disk for image {ImageId}, re-adding to collection", message.ImageId);
                
                // Re-add the existing cache file to collection
                var fileInfo = new FileInfo(message.CachePath);
                var cacheEmbedded = new CacheImageEmbedded(
                    message.ImageId,
                    message.CachePath,
                    message.CacheWidth,
                    message.CacheHeight,
                    fileInfo.Length,
                    message.Format.ToUpperInvariant(),
                    message.Quality
                );
                
                await collectionRepository.AtomicAddCacheImageAsync(collectionId, cacheEmbedded);
                return true; // Skip processing since we just re-added it
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error checking if cache exists for image {ImageId}", message.ImageId);
            return false;
        }
    }

    private async Task UpdateJobProgressAsync(
        IServiceProvider serviceProvider,
        List<CacheGenerationMessage> messages,
        List<ProcessedCacheData> processedImages,
        List<CacheGenerationMessage> failedImages)
    {
        try
        {
            // Update job progress for successful cache images
            var backgroundJobService = serviceProvider.GetRequiredService<IBackgroundJobService>();
            var jobStateRepository = serviceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
            
            foreach (var processedImage in processedImages)
            {
                var message = processedImage.Message;
                if (!string.IsNullOrEmpty(message.ScanJobId))
                {
                    await backgroundJobService.IncrementJobStageProgressAsync(
                        ObjectId.Parse(message.ScanJobId), "cache", incrementBy: 1);
                }
                
                if (!string.IsNullOrEmpty(message.JobId))
                {
                    await jobStateRepository.AtomicIncrementCompletedAsync(
                        message.JobId, message.ImageId, 0);
                }
            }
            
            // Update job progress for failed cache images - mark them as done to avoid retrying
            foreach (var message in failedImages)
            {
                if (!string.IsNullOrEmpty(message.ScanJobId))
                {
                    await backgroundJobService.IncrementJobStageProgressAsync(
                        ObjectId.Parse(message.ScanJobId), "cache", incrementBy: 1);
                }
                
                if (!string.IsNullOrEmpty(message.JobId))
                {
                    await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                }
                
                _logger.LogDebug("✅ Marked failed cache image {ImageId} as done to avoid retrying", message.ImageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error updating cache job progress");
        }
    }

    /// <summary>
    /// Updates job status to "Running" for all jobs in the batch
    /// </summary>
    private async Task UpdateJobStatusToRunningAsync(IFileProcessingJobStateRepository jobStateRepository, List<CacheGenerationMessage> messages)
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
    private async Task<bool> ValidateFileSizeAsync(CacheGenerationMessage message, IFileProcessingJobStateRepository jobStateRepository)
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
                    _logger.LogWarning("⚠️ Archive entry too large ({SizeGB}GB), skipping cache generation for {ImageId}", 
                        fileSize / 1024.0 / 1024.0 / 1024.0, message.ImageId);
                    
                    await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                    return false;
                }
            }
            else
            {
                // This should not happen - ArchiveEntry should always be provided
                _logger.LogError("❌ No ArchiveEntry provided for cache generation");
                return false;
            }
            
            if (fileSize > maxSize)
            {
                _logger.LogWarning("⚠️ Image file too large ({SizeMB}MB), skipping cache generation for {ImageId}", 
                    fileSize / 1024.0 / 1024.0, message.ImageId);
                    
                    await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                    return false;
                }
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
    /// Determines optimal cache quality to avoid degrading low-quality source images
    /// </summary>
    private async Task<int> DetermineOptimalCacheQuality(
        CacheGenerationMessage cacheMessage, 
        IImageProcessingService imageProcessingService,
        string cacheFormat,
        int requestedQuality,
        CancellationToken cancellationToken = default)
    {
        try
        {
            long fileSize = 0;
            
            // Get file size for quality analysis
            if (cacheMessage.ArchiveEntry != null)
            {
                fileSize = ArchiveFileHelper.GetArchiveEntrySize(cacheMessage.ArchiveEntry.GetFullPath(), _logger);
            }
            else
            {
                // This should not happen - ArchiveEntry should always be provided
                _logger.LogError("❌ No ArchiveEntry provided for cache quality analysis");
                return requestedQuality;
            }
            
            if (fileSize == 0)
            {
                return requestedQuality; // Fallback to requested quality
            }
            
            // Extract image dimensions for quality analysis
            ImageDimensions? dimensions = null;
            if (cacheMessage.ArchiveEntry != null)
            {
                var imageBytes = await ArchiveFileHelper.ExtractArchiveEntryBytes(cacheMessage.ArchiveEntry.GetFullPath(), null);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    dimensions = await imageProcessingService.GetImageDimensionsFromBytesAsync(imageBytes);
                }
            }
            else
            {
                // This should not happen - ArchiveEntry should always be provided
                _logger.LogError("❌ No ArchiveEntry provided for cache dimensions analysis");
                return requestedQuality;
            }
            
            if (dimensions != null)
            {
                var totalPixels = dimensions.Width * dimensions.Height;
                var bytesPerPixel = (double)fileSize / totalPixels;
                
                // Estimate source quality based on bytes per pixel
                int estimatedSourceQuality;
                if (bytesPerPixel >= 2.0)
                    estimatedSourceQuality = 95; // High quality source
                else if (bytesPerPixel >= 1.0)
                    estimatedSourceQuality = 85; // Medium-high quality
                else if (bytesPerPixel >= 0.5)
                    estimatedSourceQuality = 75; // Medium quality
                else
                    estimatedSourceQuality = 60; // Low quality source
                
                // Don't use cache quality higher than source quality
                if (requestedQuality > estimatedSourceQuality)
                {
                    _logger.LogDebug("Source image appears to be {EstimatedQuality}% quality ({BytesPerPixel:F2} bytes/pixel), " +
                        "adjusting cache quality from {RequestedQuality}% to {AdjustedQuality}%",
                        estimatedSourceQuality, bytesPerPixel, requestedQuality, estimatedSourceQuality);
                    return estimatedSourceQuality;
                }
                
                // If image is smaller than cache target, preserve original quality
                if (dimensions.Width <= cacheMessage.CacheWidth && dimensions.Height <= cacheMessage.CacheHeight)
                {
                    _logger.LogDebug("Source image ({Width}x{Height}) is smaller than cache target ({CacheWidth}x{CacheHeight}), " +
                        "using quality 100 to preserve original",
                        dimensions.Width, dimensions.Height, cacheMessage.CacheWidth, cacheMessage.CacheHeight);
                    return 100; // Preserve original quality
                }
            }
            
            return requestedQuality;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze image quality for {ImagePath}, using requested quality {Quality}", 
                cacheMessage.ArchiveEntry?.GetFullPath() ?? "Unknown", requestedQuality);
            return requestedQuality; // Fallback to requested quality
        }
    }

    /// <summary>
    /// Updates cache folder sizes after writing cache images
    /// TODO: Implement when ICacheService supports GetCacheFolderByPathAsync and IncrementFolderSizeAsync
    /// </summary>
    private async Task UpdateCacheFolderSizesAsync(IServiceProvider serviceProvider, List<CachePathData> cachePaths)
    {
        // TODO: Implement cache folder size tracking when ICacheService supports it
        await Task.CompletedTask;
        _logger.LogDebug("📊 Cache folder size tracking not yet implemented");
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
    /// Determine cache path for a cache generation message
    /// </summary>
    private async Task<string> DetermineCachePath(CacheGenerationMessage cacheMessage, IServiceProvider serviceProvider)
    {
        try
        {
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();
            
            // Determine file extension based on format
            var extension = cacheMessage.Format.ToLowerInvariant() switch
            {
                "jpeg" => ".jpg",
                "jpg" => ".jpg",
                "png" => ".png",
                "webp" => ".webp",
                "original" => Path.GetExtension(cacheMessage.ArchiveEntry?.EntryName ?? ""), // Preserve original extension
                _ => ".jpg" // Default fallback
            };
            
            // Use cache service to determine the proper cache path
            var cacheFolders = await cacheService.GetCacheFoldersAsync();
            if (!cacheFolders.Any())
            {
                _logger.LogWarning("⚠️ No cache folders configured, using default cache directory");
                return Path.Combine("cache", $"{cacheMessage.ImageId}_cache_{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}{extension}");
            }

            // Select cache folder using hash-based distribution for equal load balancing
            var collectionId = ObjectId.Parse(cacheMessage.CollectionId);
            var cacheFolder = SelectCacheFolderForEqualDistribution(cacheFolders, collectionId);
            
            // Create proper folder structure: CacheFolder/cache/CollectionId/ImageId_CacheWidthxCacheHeight.{ext}
            var collectionIdStr = cacheMessage.CollectionId;
            var cacheDir = Path.Combine(cacheFolder.Path, "cache", collectionIdStr);
            var fileName = $"{cacheMessage.ImageId}_cache_{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}{extension}";
            
            _logger.LogDebug("📁 Selected cache folder {CacheFolderName} for collection {CollectionId}, image {ImageId} (format: {Format})", 
                cacheFolder.Name, collectionIdStr, cacheMessage.ImageId, cacheMessage.Format);
            return Path.Combine(cacheDir, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error determining cache path for image {ImageId}", cacheMessage.ImageId);
            // Fallback to default path
            var extension = cacheMessage.Format.ToLowerInvariant() switch
            {
                "jpeg" => ".jpg",
                "jpg" => ".jpg",
                "png" => ".png",
                "webp" => ".webp",
                "original" => Path.GetExtension(cacheMessage.ArchiveEntry?.EntryName ?? ""),
                _ => ".jpg"
            };
            return Path.Combine("cache", $"{cacheMessage.ImageId}_cache_{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}{extension}");
        }
    }

    /// <summary>
    /// Select cache folder for equal distribution based on collection ID hash
    /// </summary>
    private CacheFolderDto SelectCacheFolderForEqualDistribution(IEnumerable<CacheFolderDto> cacheFolders, ObjectId collectionId)
    {
        // CRITICAL: Sort by Id to ensure consistent ordering across all calls
        var sortedFolders = cacheFolders.OrderBy(f => f.Id).ToList();
        
        if (!sortedFolders.Any())
        {
            throw new InvalidOperationException("No cache folders available for distribution");
        }
        
        // Use collection ID hash for consistent distribution
        var hash = collectionId.GetHashCode();
        var index = Math.Abs(hash) % sortedFolders.Count;
        
        return sortedFolders[index];
    }

    /// <summary>
    /// Mark failed messages as done to avoid endless retries
    /// </summary>
    private async Task MarkMessagesAsFailedAsync(List<CacheGenerationMessage> failedMessages)
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
                        ObjectId.Parse(message.ScanJobId), "cache", incrementBy: 1);
                }
                
                // Mark as failed in job state (so it's counted as processed)
                if (!string.IsNullOrEmpty(message.JobId))
                {
                    await jobStateRepository.AtomicIncrementFailedAsync(message.JobId, message.ImageId);
                }
                
                _logger.LogDebug("✅ Marked failed cache image {ImageId} as done to avoid retrying", message.ImageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error marking failed cache messages as done");
        }
    }
}

/// <summary>
/// Data for a processed cache image
/// </summary>
public class ProcessedCacheData
{
    public CacheGenerationMessage Message { get; set; } = null!;
    public byte[] CacheData { get; set; } = null!;
    public long Size { get; set; }
}

/// <summary>
/// Data for cache path information
/// </summary>
public class CachePathData
{
    public string ImageId { get; set; } = string.Empty;
    public string CachePath { get; set; } = string.Empty;
    public long Size { get; set; }
}
