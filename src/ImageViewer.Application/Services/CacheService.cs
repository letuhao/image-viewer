using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Constants;
using ImageViewer.Application.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Cache service implementation
/// </summary>
public class CacheService : ICacheService
{
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IImageRepository _imageRepository;
    private readonly ICacheInfoRepository _cacheInfoRepository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CacheService> _logger;
    private readonly ImageSizeOptions _sizeOptions;

    public CacheService(
        ICacheFolderRepository cacheFolderRepository,
        ICollectionRepository collectionRepository,
        IImageRepository imageRepository,
        ICacheInfoRepository cacheInfoRepository,
        IImageProcessingService imageProcessingService,
        IUnitOfWork unitOfWork,
        ILogger<CacheService> logger,
        IOptions<ImageSizeOptions> sizeOptions)
    {
        _cacheFolderRepository = cacheFolderRepository ?? throw new ArgumentNullException(nameof(cacheFolderRepository));
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));
        _cacheInfoRepository = cacheInfoRepository ?? throw new ArgumentNullException(nameof(cacheInfoRepository));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sizeOptions = sizeOptions?.Value ?? new ImageSizeOptions();
    }

    public async Task<CacheStatisticsDto> GetCacheStatisticsAsync()
    {
        _logger.LogInformation("Getting cache statistics");

        var cacheFolders = await _cacheFolderRepository.GetAllAsync();
        var collections = await _collectionRepository.GetAllAsync();
        var images = await _imageRepository.GetAllAsync();

        var totalCollections = collections.Count();
        var collectionsWithCache = collections.Count(c => c.CacheBindings.Any());
        var totalImages = images.Count();
        var cachedImages = images.Count(i => i.CacheInfo != null);
        var totalCacheSize = cacheFolders.Sum(cf => cf.CurrentSize);
        var cachePercentage = totalImages > 0 ? (double)cachedImages / totalImages * 100 : 0;

        var cacheFolderStats = cacheFolders.Select(cf => new CacheFolderStatisticsDto
        {
            Id = cf.Id,
            Name = cf.Name,
            Path = cf.Path,
            Priority = cf.Priority,
            MaxSize = cf.MaxSize,
            CurrentSize = cf.CurrentSize,
            FileCount = cf.CurrentSize / 1024, // Estimate file count
            IsActive = cf.IsActive,
            LastUsed = cf.UpdatedAt
        });

        return new CacheStatisticsDto
        {
            Summary = new CacheSummaryDto
            {
                TotalCollections = totalCollections,
                CollectionsWithCache = collectionsWithCache,
                TotalImages = totalImages,
                CachedImages = cachedImages,
                TotalCacheSize = totalCacheSize,
                CachePercentage = cachePercentage
            },
            CacheFolders = cacheFolderStats
        };
    }

    public async Task<IEnumerable<CacheFolderDto>> GetCacheFoldersAsync()
    {
        _logger.LogInformation("Getting cache folders");

        var cacheFolders = await _cacheFolderRepository.GetAllAsync();
        return cacheFolders.Select(cf => new CacheFolderDto
        {
            Id = cf.Id,
            Name = cf.Name,
            Path = cf.Path,
            Priority = cf.Priority,
            MaxSize = cf.MaxSize,
            CurrentSize = cf.CurrentSize,
            IsActive = cf.IsActive,
            CreatedAt = cf.CreatedAt,
            UpdatedAt = cf.UpdatedAt
        });
    }

    public async Task<CacheFolderDto> CreateCacheFolderAsync(CreateCacheFolderDto dto)
    {
        _logger.LogInformation("Creating cache folder: {Name}", dto.Name);

        var cacheFolder = new Domain.Entities.CacheFolder(
            dto.Name,
            dto.Path,
            (int)dto.MaxSize,
            dto.Priority
        );

        await _cacheFolderRepository.CreateAsync(cacheFolder);

        _logger.LogInformation("Cache folder created with ID: {Id}", cacheFolder.Id);

        return new CacheFolderDto
        {
            Id = cacheFolder.Id,
            Name = cacheFolder.Name,
            Path = cacheFolder.Path,
            Priority = cacheFolder.Priority,
            MaxSize = cacheFolder.MaxSize,
            CurrentSize = cacheFolder.CurrentSize,
            IsActive = cacheFolder.IsActive,
            CreatedAt = cacheFolder.CreatedAt,
            UpdatedAt = cacheFolder.UpdatedAt
        };
    }

    public async Task<CacheFolderDto> UpdateCacheFolderAsync(ObjectId id, UpdateCacheFolderDto dto)
    {
        _logger.LogInformation("Updating cache folder: {Id}", id);

        var cacheFolder = await _cacheFolderRepository.GetByIdAsync(id);
        if (cacheFolder == null)
        {
            throw new ArgumentException($"Cache folder with ID {id} not found");
        }

        cacheFolder.UpdateName(dto.Name);
        cacheFolder.UpdatePath(dto.Path);
        cacheFolder.UpdatePriority(dto.Priority);
        cacheFolder.UpdateMaxSize(dto.MaxSize);
        cacheFolder.SetActive(dto.IsActive);

        await _cacheFolderRepository.UpdateAsync(cacheFolder);

        _logger.LogInformation("Cache folder updated: {Id}", id);

        return new CacheFolderDto
        {
            Id = cacheFolder.Id,
            Name = cacheFolder.Name,
            Path = cacheFolder.Path,
            Priority = cacheFolder.Priority,
            MaxSize = cacheFolder.MaxSize,
            CurrentSize = cacheFolder.CurrentSize,
            IsActive = cacheFolder.IsActive,
            CreatedAt = cacheFolder.CreatedAt,
            UpdatedAt = cacheFolder.UpdatedAt
        };
    }

    public async Task DeleteCacheFolderAsync(ObjectId id)
    {
        _logger.LogInformation("Deleting cache folder: {Id}", id);

        var cacheFolder = await _cacheFolderRepository.GetByIdAsync(id);
        if (cacheFolder == null)
        {
            throw new ArgumentException($"Cache folder with ID {id} not found");
        }

        await _cacheFolderRepository.DeleteAsync(cacheFolder.Id);

        _logger.LogInformation("Cache folder deleted: {Id}", id);
    }

    public async Task<CacheFolderDto> GetCacheFolderAsync(ObjectId id)
    {
        _logger.LogInformation("Getting cache folder: {Id}", id);

        var cacheFolder = await _cacheFolderRepository.GetByIdAsync(id);
        if (cacheFolder == null)
        {
            throw new ArgumentException($"Cache folder with ID {id} not found");
        }

        return new CacheFolderDto
        {
            Id = cacheFolder.Id,
            Name = cacheFolder.Name,
            Path = cacheFolder.Path,
            Priority = cacheFolder.Priority,
            MaxSize = cacheFolder.MaxSize,
            CurrentSize = cacheFolder.CurrentSize,
            IsActive = cacheFolder.IsActive,
            CreatedAt = cacheFolder.CreatedAt,
            UpdatedAt = cacheFolder.UpdatedAt
        };
    }

    public async Task ClearCollectionCacheAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Clearing cache for collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        // Clear cache info for all images in collection
        var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
        foreach (var image in images)
        {
            image.ClearCacheInfo();
        }


        _logger.LogInformation("Cache cleared for collection: {CollectionId}", collectionId);
    }

    public async Task ClearAllCacheAsync()
    {
        _logger.LogInformation("Clearing all cache");

        var images = await _imageRepository.GetAllAsync();
        foreach (var image in images)
        {
            image.ClearCacheInfo();
        }


        _logger.LogInformation("All cache cleared");
    }

    public async Task<CollectionCacheStatusDto> GetCollectionCacheStatusAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Getting cache status for collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
        var totalImages = images.Count();
        var cachedImages = images.Count(i => i.CacheInfo != null);
        var cachePercentage = totalImages > 0 ? (double)cachedImages / totalImages * 100 : 0;

        var cachedImagesList = images.Where(i => i.CacheInfo != null).ToList();
        var lastCacheUpdate = cachedImagesList.Any() ? 
            cachedImagesList.Max(i => i.UpdatedAt) : 
            DateTime.UtcNow;

        return new CollectionCacheStatusDto
        {
            CollectionId = collectionId,
            TotalImages = totalImages,
            CachedImages = cachedImages,
            CachePercentage = cachePercentage,
            LastCacheUpdate = lastCacheUpdate
        };
    }

    public async Task RegenerateCollectionCacheAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Regenerating cache for collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        // Clear existing cache
        await ClearCollectionCacheAsync(collectionId);

        try
        {
            // Get all images in the collection
            var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
            if (!images.Any())
            {
                _logger.LogWarning("No images found for collection: {CollectionId}", collectionId);
                return;
            }

            // Get cache folder for this collection
            var cacheFolder = await GetCacheFolderForCollectionAsync(collectionId);
            if (cacheFolder == null)
            {
                throw new InvalidOperationException($"No cache folder available for collection: {collectionId}");
            }

            // Process images in batches
            const int batchSize = 10;
            var totalImages = images.Count();
            var processedCount = 0;

            for (int i = 0; i < totalImages; i += batchSize)
            {
                var batch = images.Skip(i).Take(batchSize);
                
                foreach (var image in batch)
                {
                    try
                    {
                        // Get full image path
                        var fullImagePath = Path.Combine(image.Collection.Path, image.RelativePath);
                        
                        // Optionally pre-generate thumbnail if needed (skip persisting to avoid overriding cache info)
                        // var thumbnailData = await _imageProcessingService.GenerateThumbnailAsync(
                        //     fullImagePath, ImageDefaults.ThumbnailWidth, ImageDefaults.ThumbnailHeight, CancellationToken.None);
                        // (Intentionally not saving thumbnail via cache service to keep a single cache record per image)

                        // Generate cache image
                        var cacheData = await _imageProcessingService.ResizeImageAsync(
                            fullImagePath, _sizeOptions.CacheWidth, _sizeOptions.CacheHeight, _sizeOptions.JpegQuality, CancellationToken.None);
                        
                        if (cacheData != null)
                        {
                            await SaveCachedImageAsync(image.Id, $"{_sizeOptions.CacheWidth}x{_sizeOptions.CacheHeight}", cacheData, CancellationToken.None);
                        }

                        processedCount++;
                        _logger.LogDebug("Processed image {ProcessedCount}/{TotalImages} for collection {CollectionId}", 
                            processedCount, totalImages, collectionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image {ImageId} for collection {CollectionId}", 
                            image.Id, collectionId);
                    }
                }

                // Save changes after each batch
                await _unitOfWork.SaveChangesAsync();
            }

            // Update cache folder statistics
            await UpdateCacheFolderStatisticsAsync(cacheFolder.Id);

            _logger.LogInformation("Cache regeneration completed for collection: {CollectionId}. Processed {ProcessedCount}/{TotalImages} images", 
                collectionId, processedCount, totalImages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating cache for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task RegenerateCollectionCacheAsync(ObjectId collectionId, IEnumerable<(int Width, int Height)> sizes)
    {
        _logger.LogInformation("Regenerating cache for collection: {CollectionId} with multiple sizes", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        await ClearCollectionCacheAsync(collectionId);

        try
        {
            var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
            if (!images.Any())
            {
                _logger.LogWarning("No images found for collection: {CollectionId}", collectionId);
                return;
            }

            var cacheFolder = await GetCacheFolderForCollectionAsync(collectionId);
            if (cacheFolder == null)
            {
                throw new InvalidOperationException($"No cache folder available for collection: {collectionId}");
            }

            const int batchSize = 10;
            var totalImages = images.Count();
            var processedCount = 0;

            for (int i = 0; i < totalImages; i += batchSize)
            {
                var batch = images.Skip(i).Take(batchSize);

                foreach (var image in batch)
                {
                    try
                    {
                        var fullImagePath = Path.Combine(image.Collection.Path, image.RelativePath);

                        foreach (var (Width, Height) in sizes)
                        {
                            var cacheData = await _imageProcessingService.ResizeImageAsync(
                                fullImagePath, Width, Height, _sizeOptions.JpegQuality, CancellationToken.None);

                            if (cacheData != null)
                            {
                                await SaveCachedImageAsync(image.Id, $"{Width}x{Height}", cacheData, CancellationToken.None);
                            }
                        }

                        processedCount++;
                        _logger.LogDebug("Processed image {ProcessedCount}/{TotalImages} for collection {CollectionId}", 
                            processedCount, totalImages, collectionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image {ImageId} for collection {CollectionId}", 
                            image.Id, collectionId);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }

            await UpdateCacheFolderStatisticsAsync(cacheFolder.Id);

            _logger.LogInformation("Cache regeneration completed for collection: {CollectionId}. Processed {ProcessedCount}/{TotalImages} images", 
                collectionId, processedCount, totalImages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating cache for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<byte[]?> GetCachedImageAsync(ObjectId imageId, string dimensions, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting cached image: {ImageId} with dimensions: {Dimensions}", imageId, dimensions);

        try
        {
            // Get cache info from database
            var cacheInfo = await _cacheInfoRepository.GetByImageIdAsync(imageId);
            if (cacheInfo == null || !cacheInfo.IsValid || cacheInfo.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogDebug("No valid cache found for image: {ImageId}", imageId);
                return null;
            }

            // Check if cache file exists
            if (!File.Exists(cacheInfo.CachePath))
            {
                _logger.LogWarning("Cache file not found for image: {ImageId} at path: {CachePath}", imageId, cacheInfo.CachePath);
                
                // Mark cache as invalid
                cacheInfo.MarkAsInvalid();
                await _cacheInfoRepository.UpdateAsync(cacheInfo);
                await _unitOfWork.SaveChangesAsync();
                
                return null;
            }

            // Read and return cached image data
            var imageData = await File.ReadAllBytesAsync(cacheInfo.CachePath, cancellationToken);
            _logger.LogDebug("Retrieved cached image: {ImageId}, size: {Size} bytes", imageId, imageData.Length);
            
            return imageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached image: {ImageId}", imageId);
            return null;
        }
    }

    public async Task SaveCachedImageAsync(ObjectId imageId, string dimensions, byte[] imageData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving cached image: {ImageId} with dimensions: {Dimensions}", imageId, dimensions);

        try
        {
            // Get image to find collection
            var image = await _imageRepository.GetByIdAsync(imageId);
            if (image == null)
            {
                throw new KeyNotFoundException($"Image with ID {imageId} not found");
            }

            // Get cache folder for this collection
            var cacheFolder = await GetCacheFolderForCollectionAsync(image.CollectionId);
            if (cacheFolder == null)
            {
                throw new InvalidOperationException($"No cache folder available for collection: {image.CollectionId}");
            }

            // Ensure cache folder exists
            if (!Directory.Exists(cacheFolder.Path))
            {
                Directory.CreateDirectory(cacheFolder.Path);
            }

            // Construct cache file path
            var cacheFileName = $"{imageId}_{dimensions.Replace("x", "_")}.jpg";
            var cachePath = Path.Combine(cacheFolder.Path, cacheFileName);

            // Write image data to cache file
            await File.WriteAllBytesAsync(cachePath, imageData, cancellationToken);

            // Update or create cache info in database
            var existingCacheInfo = await _cacheInfoRepository.GetByImageIdAsync(imageId);
            if (existingCacheInfo != null)
            {
                // Update existing cache info
                existingCacheInfo.UpdateCachePath(cachePath);
                existingCacheInfo.UpdateFileSize(imageData.Length);
                existingCacheInfo.UpdateDimensions(dimensions);
                existingCacheInfo.ExtendExpiration(DateTime.UtcNow.AddDays(30));
                existingCacheInfo.MarkAsValid();
                
                await _cacheInfoRepository.UpdateAsync(existingCacheInfo);
            }
            else
            {
                // Create new cache info
                var cacheInfo = new ImageCacheInfo(
                    imageId,
                    cachePath,
                    dimensions,
                    imageData.Length,
                    DateTime.UtcNow.AddDays(30));

                await _cacheInfoRepository.CreateAsync(cacheInfo);
            }

            // Update cache folder statistics
            await UpdateCacheFolderStatisticsAsync(cacheFolder.Id);

            _logger.LogDebug("Saved cached image: {ImageId} to {CachePath}, size: {Size} bytes", 
                imageId, cachePath, imageData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cached image: {ImageId}", imageId);
            throw;
        }
    }

    private async Task<CacheFolder?> GetCacheFolderForCollectionAsync(ObjectId collectionId)
    {
        try
        {
            // Get the collection to find its cache bindings
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                return null;
            }

            // Find the first available cache folder for this collection
            var cacheBinding = collection.CacheBindings.FirstOrDefault();
            if (!string.IsNullOrEmpty(cacheBinding?.CacheFolder))
            {
                var cacheFolder = await _cacheFolderRepository.GetByPathAsync(cacheBinding.CacheFolder);
                if (cacheFolder != null)
                    return cacheFolder;
            }

            // If no specific cache folder is bound, get the first available cache folder
            var availableCacheFolders = await _cacheFolderRepository.GetAllAsync();
            return availableCacheFolders.FirstOrDefault(cf => cf.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folder for collection: {CollectionId}", collectionId);
            return null;
        }
    }

    private async Task UpdateCacheFolderStatisticsAsync(ObjectId cacheFolderId)
    {
        try
        {
            var cacheFolder = await _cacheFolderRepository.GetByIdAsync(cacheFolderId);
            if (cacheFolder == null)
            {
                return;
            }

            // Calculate current size and file count
            var cacheInfos = await _cacheInfoRepository.GetByCacheFolderIdAsync(cacheFolderId);
            var currentSize = cacheInfos.Sum(ci => ci.FileSizeBytes);
            var fileCount = cacheInfos.Count();

            // Update cache folder statistics
            cacheFolder.UpdateStatistics(currentSize, fileCount);
            await _cacheFolderRepository.UpdateAsync(cacheFolder);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Updated cache folder statistics for {CacheFolderId}: {CurrentSize} bytes, {FileCount} files", 
                cacheFolderId, currentSize, fileCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cache folder statistics for {CacheFolderId}", cacheFolderId);
        }
    }

    public async Task CleanupExpiredCacheAsync()
    {
        try
        {
            _logger.LogInformation("Starting cleanup of expired cache entries");

            var expiredEntries = await _cacheInfoRepository.GetExpiredAsync();
            var deletedCount = 0;

            foreach (var entry in expiredEntries)
            {
                try
                {
                    // Delete cache file if it exists
                    if (File.Exists(entry.CachePath))
                    {
                        File.Delete(entry.CachePath);
                    }

                    // Delete thumbnail file if it exists (construct path from cache path)
                    var thumbnailPath = entry.CachePath.Replace("_cache.jpg", "_thumb.jpg");
                    if (File.Exists(thumbnailPath))
                    {
                        File.Delete(thumbnailPath);
                    }

                    // Remove from database
                    await _cacheInfoRepository.DeleteAsync(entry.Id);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up expired cache entry {CacheInfoId}", entry.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cleanup of expired cache entries completed. Deleted {DeletedCount} entries", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during expired cache cleanup");
            throw;
        }
    }

    public async Task CleanupOldCacheAsync(DateTime cutoffDate)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of old cache entries older than {CutoffDate}", cutoffDate);

            var oldEntries = await _cacheInfoRepository.GetOlderThanAsync(cutoffDate);
            var deletedCount = 0;

            foreach (var entry in oldEntries)
            {
                try
                {
                    // Delete cache file if it exists
                    if (File.Exists(entry.CachePath))
                    {
                        File.Delete(entry.CachePath);
                    }

                    // Delete thumbnail file if it exists (construct path from cache path)
                    var thumbnailPath = entry.CachePath.Replace("_cache.jpg", "_thumb.jpg");
                    if (File.Exists(thumbnailPath))
                    {
                        File.Delete(thumbnailPath);
                    }

                    // Remove from database
                    await _cacheInfoRepository.DeleteAsync(entry.Id);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up old cache entry {CacheInfoId}", entry.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cleanup of old cache entries completed. Deleted {DeletedCount} entries", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during old cache cleanup");
            throw;
        }
    }
}
