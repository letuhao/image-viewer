using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.DTOs;
using ImageViewer.Application.Helpers;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for cleaning up __MACOSX metadata files from collections
/// </summary>
public class MacOSXCleanupService : IMacOSXCleanupService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<MacOSXCleanupService> _logger;

    public MacOSXCleanupService(
        ICollectionRepository collectionRepository,
        ILogger<MacOSXCleanupService> logger)
    {
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MacOSXCleanupResult> CleanupMacOSXFilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🧹 Starting __MACOSX cleanup process...");
        
        var result = new MacOSXCleanupResult();
        var startTime = DateTime.UtcNow;

        try
        {
            // Get all collections
            var collections = await _collectionRepository.GetAllAsync();
            _logger.LogInformation($"📊 Found {collections.Count()} collections to scan");

            foreach (var collection in collections)
            {
                try
                {
                    var collectionResult = await CleanupCollectionMacOSXFilesAsync(collection, cancellationToken);
                    
                    if (collectionResult.ImagesRemoved > 0 || collectionResult.ThumbnailsRemoved > 0 || collectionResult.CacheImagesRemoved > 0)
                    {
                        result.AffectedCollections++;
                        result.TotalImagesRemoved += collectionResult.ImagesRemoved;
                        result.TotalThumbnailsRemoved += collectionResult.ThumbnailsRemoved;
                        result.TotalCacheImagesRemoved += collectionResult.CacheImagesRemoved;
                        result.TotalSpaceFreed += collectionResult.SpaceFreed;
                        
                        result.AffectedCollectionDetails.Add(new CollectionCleanupDetail
                        {
                            CollectionId = collection.Id.ToString(),
                            CollectionName = collection.Name,
                            CollectionPath = collection.Path,
                            ImagesRemoved = collectionResult.ImagesRemoved,
                            ThumbnailsRemoved = collectionResult.ThumbnailsRemoved,
                            CacheImagesRemoved = collectionResult.CacheImagesRemoved,
                            SpaceFreed = collectionResult.SpaceFreed
                        });

                        _logger.LogInformation("✅ Cleaned collection '{Name}' ({Id}): {Images} images, {Thumbnails} thumbnails, {Cache} cache images removed, {Space} bytes freed",
                            collection.Name, collection.Id, collectionResult.ImagesRemoved, collectionResult.ThumbnailsRemoved, collectionResult.CacheImagesRemoved, collectionResult.SpaceFreed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to cleanup collection '{Name}' ({Id})", collection.Name, collection.Id);
                    result.Errors.Add($"Collection '{collection.Name}' ({collection.Id}): {ex.Message}");
                }
            }

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("🎉 __MACOSX cleanup completed successfully in {Duration}ms. Affected {Collections} collections, removed {Images} images, {Thumbnails} thumbnails, {Cache} cache images, freed {Space} bytes",
                result.Duration.TotalMilliseconds, result.AffectedCollections, result.TotalImagesRemoved, result.TotalThumbnailsRemoved, result.TotalCacheImagesRemoved, result.TotalSpaceFreed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ __MACOSX cleanup failed");
            result.Success = false;
            result.Duration = DateTime.UtcNow - startTime;
            result.Errors.Add($"Global error: {ex.Message}");
            return result;
        }
    }

    private async Task<Domain.DTOs.CollectionCleanupResult> CleanupCollectionMacOSXFilesAsync(Collection collection, CancellationToken cancellationToken)
    {
        var result = new Domain.DTOs.CollectionCleanupResult();
        
        try
        {
            var needsUpdate = false;
            
            // Clean up images
            if (collection.Images?.Any() == true)
            {
                var originalCount = collection.Images.Count;
                var macosxImages = collection.Images
                    .Where(img => MacOSXFilterHelper.IsMacOSXPath(img.RelativePath) || MacOSXFilterHelper.IsMacOSXPath(img.Filename))
                    .ToList();

                if (macosxImages.Any())
                {
                    foreach (var image in macosxImages)
                    {
                        collection.RemoveImage(image.Id);
                        result.SpaceFreed += image.FileSize;
                        result.ImagesRemoved++;
                    }
                    
                    needsUpdate = true;
                    _logger.LogDebug("🗑️ Removed {Count} __MACOSX images from collection '{Name}'", macosxImages.Count, collection.Name);
                }
            }

            // Clean up thumbnails
            if (collection.Thumbnails?.Any() == true)
            {
                var macosxThumbnails = collection.Thumbnails
                    .Where(thumb => MacOSXFilterHelper.IsMacOSXPath(thumb.ImageId) || MacOSXFilterHelper.IsMacOSXPath(thumb.ThumbnailPath))
                    .ToList();

                if (macosxThumbnails.Any())
                {
                    foreach (var thumbnail in macosxThumbnails)
                    {
                        collection.RemoveThumbnail(thumbnail.Id);
                        result.SpaceFreed += thumbnail.FileSize;
                        result.ThumbnailsRemoved++;
                    }
                    
                    needsUpdate = true;
                    _logger.LogDebug("🗑️ Removed {Count} __MACOSX thumbnails from collection '{Name}'", macosxThumbnails.Count, collection.Name);
                }
            }

            // Clean up cache images
            if (collection.CacheImages?.Any() == true)
            {
                var macosxCacheImages = collection.CacheImages
                    .Where(cache => MacOSXFilterHelper.IsMacOSXPath(cache.ImageId) || MacOSXFilterHelper.IsMacOSXPath(cache.CachePath))
                    .ToList();

                if (macosxCacheImages.Any())
                {
                    foreach (var cacheImage in macosxCacheImages)
                    {
                        collection.RemoveCacheImage(cacheImage.Id);
                        result.SpaceFreed += cacheImage.FileSize;
                        result.CacheImagesRemoved++;
                    }
                    
                    needsUpdate = true;
                    _logger.LogDebug("🗑️ Removed {Count} __MACOSX cache images from collection '{Name}'", macosxCacheImages.Count, collection.Name);
                }
            }

            // Update collection if changes were made
            if (needsUpdate)
            {
                await _collectionRepository.UpdateAsync(collection);
                _logger.LogDebug("💾 Updated collection '{Name}' after cleanup", collection.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to cleanup collection '{Name}'", collection.Name);
            throw;
        }
    }


    public async Task<MacOSXCleanupPreview> PreviewMacOSXCleanupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Previewing __MACOSX cleanup...");
        
        var preview = new MacOSXCleanupPreview();
        var startTime = DateTime.UtcNow;

        try
        {
            // Get all collections
            var collections = await _collectionRepository.GetAllAsync();
            _logger.LogInformation($"📊 Found {collections.Count()} collections to scan for preview");

            foreach (var collection in collections)
            {
                try
                {
                    var collectionPreview = PreviewCollectionMacOSXFiles(collection);
                    
                    if (collectionPreview.ImagesToRemove > 0 || collectionPreview.ThumbnailsToRemove > 0 || collectionPreview.CacheImagesToRemove > 0)
                    {
                        preview.AffectedCollections++;
                        preview.TotalImagesToRemove += collectionPreview.ImagesToRemove;
                        preview.TotalThumbnailsToRemove += collectionPreview.ThumbnailsToRemove;
                        preview.TotalCacheImagesToRemove += collectionPreview.CacheImagesToRemove;
                        preview.TotalSpaceToFree += collectionPreview.SpaceToFree;
                        
                        preview.AffectedCollectionDetails.Add(new CollectionCleanupPreviewDetail
                        {
                            CollectionId = collection.Id.ToString(),
                            CollectionName = collection.Name,
                            CollectionPath = collection.Path,
                            ImagesToRemove = collectionPreview.ImagesToRemove,
                            ThumbnailsToRemove = collectionPreview.ThumbnailsToRemove,
                            CacheImagesToRemove = collectionPreview.CacheImagesToRemove,
                            SpaceToFree = collectionPreview.SpaceToFree
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to preview collection '{Name}' ({Id})", collection.Name, collection.Id);
                    preview.Errors.Add($"Collection '{collection.Name}' ({collection.Id}): {ex.Message}");
                }
            }

            preview.Success = true;
            preview.Duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("🔍 __MACOSX cleanup preview completed in {Duration}ms. Would affect {Collections} collections, remove {Images} images, {Thumbnails} thumbnails, {Cache} cache images, free {Space} bytes",
                preview.Duration.TotalMilliseconds, preview.AffectedCollections, preview.TotalImagesToRemove, preview.TotalThumbnailsToRemove, preview.TotalCacheImagesToRemove, preview.TotalSpaceToFree);

            return preview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ __MACOSX cleanup preview failed");
            preview.Success = false;
            preview.Duration = DateTime.UtcNow - startTime;
            preview.Errors.Add($"Global error: {ex.Message}");
            return preview;
        }
    }

    private static Domain.DTOs.CollectionCleanupPreview PreviewCollectionMacOSXFiles(Collection collection)
    {
        var preview = new Domain.DTOs.CollectionCleanupPreview();
        
        // Preview images
        if (collection.Images?.Any() == true)
        {
            var macosxImages = collection.Images
                .Where(img => MacOSXFilterHelper.IsMacOSXPath(img.RelativePath) || MacOSXFilterHelper.IsMacOSXPath(img.Filename))
                .ToList();

            preview.ImagesToRemove = macosxImages.Count;
            preview.SpaceToFree += macosxImages.Sum(img => img.FileSize);
        }

        // Preview thumbnails
        if (collection.Thumbnails?.Any() == true)
        {
            var macosxThumbnails = collection.Thumbnails
                .Where(thumb => MacOSXFilterHelper.IsMacOSXPath(thumb.ImageId) || MacOSXFilterHelper.IsMacOSXPath(thumb.ThumbnailPath))
                .ToList();

            preview.ThumbnailsToRemove = macosxThumbnails.Count;
            preview.SpaceToFree += macosxThumbnails.Sum(thumb => thumb.FileSize);
        }

        // Preview cache images
        if (collection.CacheImages?.Any() == true)
        {
            var macosxCacheImages = collection.CacheImages
                .Where(cache => MacOSXFilterHelper.IsMacOSXPath(cache.ImageId) || MacOSXFilterHelper.IsMacOSXPath(cache.CachePath))
                .ToList();

            preview.CacheImagesToRemove = macosxCacheImages.Count;
            preview.SpaceToFree += macosxCacheImages.Sum(cache => cache.FileSize);
        }

        return preview;
    }
}
