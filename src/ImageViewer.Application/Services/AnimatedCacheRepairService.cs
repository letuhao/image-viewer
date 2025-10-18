using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Helpers;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service to find and repair incorrectly cached animated files (GIF, WebP, etc.)
/// 中文：查找并修复错误缓存的动画文件服务
/// Tiếng Việt: Dịch vụ tìm và sửa tệp hoạt hình được lưu cache không đúng
/// </summary>
public class AnimatedCacheRepairService : IAnimatedCacheRepairService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<AnimatedCacheRepairService> _logger;

    public AnimatedCacheRepairService(
        ICollectionRepository collectionRepository,
        IMessageQueueService messageQueueService,
        ILogger<AnimatedCacheRepairService> logger)
    {
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _messageQueueService = messageQueueService ?? throw new ArgumentNullException(nameof(messageQueueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AnimatedCacheRepairResult> FindIncorrectlyCachedAnimatedFilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Starting scan for incorrectly cached animated files...");
        
        var result = new AnimatedCacheRepairResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Get all collections
            var collections = await _collectionRepository.GetAllAsync();
            result.TotalCollections = collections.Count();

            foreach (var collection in collections)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Check each image in the collection
                var images = collection.GetActiveImages();
                result.TotalImages += images.Count();

                foreach (var image in images)
                {
                    // Check if this is an animated format
                    if (AnimatedFormatHelper.IsAnimatedFormat(image.Filename))
                    {
                        result.AnimatedFilesFound++;
                        
                        // Check if it has cache images
                        var cacheImages = collection.CacheImages?.Where(c => c.ImageId == image.Id).ToList();
                        if (cacheImages != null && cacheImages.Any())
                        {
                            // Check if cache format matches original format
                            var originalExtension = System.IO.Path.GetExtension(image.Filename).TrimStart('.');
                            var hasIncorrectCache = cacheImages.Any(c => 
                                !string.Equals(c.Format, originalExtension, StringComparison.OrdinalIgnoreCase));

                            if (hasIncorrectCache)
                            {
                                result.IncorrectlyCachedFiles++;
                                result.IncorrectFiles.Add(new IncorrectCacheFileInfo
                                {
                                    CollectionId = collection.Id,
                                    CollectionName = collection.Name,
                                    ImageId = image.Id,
                                    Filename = image.Filename,
                                    OriginalFormat = originalExtension,
                                    CacheFormats = cacheImages.Select(c => c.Format).Distinct().ToList()
                                });
                            }
                        }
                    }
                }
            }

            result.EndTime = DateTime.UtcNow;
            result.Success = true;

            _logger.LogInformation("✅ Scan completed: Found {AnimatedFiles} animated files, {IncorrectFiles} need repair", 
                result.AnimatedFilesFound, result.IncorrectlyCachedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error scanning for incorrectly cached animated files");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<AnimatedCacheRepairResult> RepairIncorrectlyCachedAnimatedFilesAsync(
        bool forceRegenerate = false, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔧 Starting repair of incorrectly cached animated files...");

        // First, find all incorrect files
        var scanResult = await FindIncorrectlyCachedAnimatedFilesAsync(cancellationToken);
        if (!scanResult.Success)
        {
            return scanResult;
        }

        scanResult.RepairStartTime = DateTime.UtcNow;

        try
        {
            foreach (var incorrectFile in scanResult.IncorrectFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Get the collection
                    var collection = await _collectionRepository.GetByIdAsync(incorrectFile.CollectionId);
                    if (collection == null)
                    {
                        _logger.LogWarning("⚠️ Collection {CollectionId} not found", incorrectFile.CollectionId);
                        continue;
                    }

                    // Find the image
                    var image = collection.Images?.FirstOrDefault(i => i.Id == incorrectFile.ImageId);
                    if (image == null)
                    {
                        _logger.LogWarning("⚠️ Image {ImageId} not found in collection {CollectionId}", 
                            incorrectFile.ImageId, incorrectFile.CollectionId);
                        continue;
                    }

                    // Queue a cache regeneration message
                    var cacheMessage = new CacheGenerationMessage
                    {
                        ImageId = incorrectFile.ImageId,
                        CollectionId = incorrectFile.CollectionId.ToString(),
                        ArchiveEntry = ArchiveEntryInfo.FromCollection(
                            collection.Path,
                            collection.Type,
                            image.Filename,
                            image.FileSize),
                        ForceRegenerate = forceRegenerate,
                        CacheWidth = 1920,  // Use default cache size
                        CacheHeight = 1080,
                        Format = incorrectFile.OriginalFormat // Use original format
                    };

                    await _messageQueueService.PublishAsync(cacheMessage, "cache.generation");
                    scanResult.FilesQueuedForRepair++;
                    
                    _logger.LogDebug("✅ Queued repair for {Filename} in collection {CollectionName}", 
                        incorrectFile.Filename, incorrectFile.CollectionName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error queuing repair for {Filename}", incorrectFile.Filename);
                    scanResult.RepairErrors++;
                }
            }

            scanResult.RepairEndTime = DateTime.UtcNow;
            _logger.LogInformation("✅ Repair completed: {Queued} files queued for regeneration, {Errors} errors", 
                scanResult.FilesQueuedForRepair, scanResult.RepairErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during repair process");
            scanResult.Success = false;
            scanResult.ErrorMessage = ex.Message;
        }

        return scanResult;
    }

    public async Task<int> RegenerateAllAnimatedCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 Starting regeneration of all animated file caches...");
        
        int queuedCount = 0;

        try
        {
            // Get all collections
            var collections = await _collectionRepository.GetAllAsync();

            foreach (var collection in collections)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Check each image in the collection
                var images = collection.GetActiveImages();

                foreach (var image in images)
                {
                    // Check if this is an animated format
                    if (AnimatedFormatHelper.IsAnimatedFormat(image.Filename))
                    {
                        // Queue a cache regeneration message
                        var cacheMessage = new CacheGenerationMessage
                        {
                            ImageId = image.Id,
                            CollectionId = collection.Id.ToString(),
                            ArchiveEntry = ArchiveEntryInfo.FromCollection(
                                collection.Path,
                                collection.Type,
                                image.Filename,
                                image.FileSize),
                            ForceRegenerate = true, // Force regeneration
                            CacheWidth = 1920,
                            CacheHeight = 1080,
                            Format = System.IO.Path.GetExtension(image.Filename).TrimStart('.') // Use original format
                        };

                        await _messageQueueService.PublishAsync(cacheMessage, "cache.generation");
                        queuedCount++;
                    }
                }
            }

            _logger.LogInformation("✅ Regeneration queued: {Count} animated files", queuedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during regeneration process");
            throw;
        }

        return queuedCount;
    }
}

