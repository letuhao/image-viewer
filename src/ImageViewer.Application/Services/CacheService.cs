using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Application.Mappings;
using ImageViewer.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageViewer.Application.Services;

/// <summary>
/// Cache service implementation using embedded design
/// Refactored to use ImageEmbedded.CacheInfo instead of separate ImageCacheInfo entity
/// </summary>
public class CacheService : ICacheService
{
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<CacheService> _logger;
    private readonly ImageSizeOptions _sizeOptions;

    public CacheService(
        ICacheFolderRepository cacheFolderRepository,
        ICollectionRepository collectionRepository,
        ILogger<CacheService> logger,
        IOptions<ImageSizeOptions> sizeOptions)
    {
        _cacheFolderRepository = cacheFolderRepository ?? throw new ArgumentNullException(nameof(cacheFolderRepository));
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sizeOptions = sizeOptions?.Value ?? new ImageSizeOptions();
    }

    public async Task<CacheStatisticsDto> GetCacheStatisticsAsync()
    {
        _logger.LogInformation("Getting cache statistics");

        try
        {
            // Get all cache folders
            var cacheFolders = await _cacheFolderRepository.GetAllAsync();
            var cacheFoldersList = cacheFolders.ToList();

            // Use optimized aggregation pipeline for cache statistics (10-100x faster)
            var (totalImages, cachedImages, totalCacheSize, collectionsWithCache) = 
                await _collectionRepository.GetCacheStatisticsAsync();

            // Get total collection count efficiently
            var totalCollections = await _collectionRepository.GetActiveCollectionCountAsync();

            var summary = new CacheSummaryDto
            {
                TotalCollections = (int)totalCollections,
                CollectionsWithCache = collectionsWithCache,
                TotalImages = totalImages,
                CachedImages = cachedImages,
                TotalCacheSize = totalCacheSize,
                CachePercentage = totalImages > 0 
                    ? (double)cachedImages / totalImages * 100 
                    : 0
            };

            var folderStats = cacheFoldersList.Select(cf => cf.ToStatisticsDto()).ToList();

            return new CacheStatisticsDto
            {
                Summary = summary,
                CacheFolders = folderStats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            throw;
        }
    }

    public async Task<IEnumerable<CacheFolderDto>> GetCacheFoldersAsync()
    {
        _logger.LogDebug("Getting cache folders");

        try
        {
            var cacheFolders = await _cacheFolderRepository.GetAllAsync();
            return cacheFolders.Select(cf => new CacheFolderDto
            {
                Id = cf.Id,
                Name = cf.Name,
                Path = cf.Path,
                MaxSize = cf.MaxSizeBytes,
                CurrentSize = cf.CurrentSize,
                Priority = cf.Priority,
                IsActive = cf.IsActive,
                CreatedAt = cf.CreatedAt,
                UpdatedAt = cf.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folders");
            throw;
        }
    }

    public async Task<CacheFolderDto> CreateCacheFolderAsync(CreateCacheFolderDto dto)
    {
        _logger.LogInformation("Creating cache folder: {Name}", dto.Name);

        try
        {
            var cacheFolder = new CacheFolder(dto.Name, dto.Path, dto.MaxSize, dto.Priority);
            // IsActive is set to true by default in CacheFolder constructor

            var created = await _cacheFolderRepository.CreateAsync(cacheFolder);

            return new CacheFolderDto
            {
                Id = created.Id,
                Name = created.Name,
                Path = created.Path,
                MaxSize = created.MaxSizeBytes,
                CurrentSize = created.CurrentSize,
                Priority = created.Priority,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cache folder: {Name}", dto.Name);
            throw;
        }
    }

    public async Task<CacheFolderDto> UpdateCacheFolderAsync(ObjectId id, UpdateCacheFolderDto dto)
    {
        _logger.LogInformation("Updating cache folder: {Id}", id);

        try
        {
            var cacheFolder = await _cacheFolderRepository.GetByIdAsync(id);
            if (cacheFolder == null)
            {
                throw new KeyNotFoundException($"Cache folder with ID {id} not found");
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
                cacheFolder.UpdateName(dto.Name);

            if (!string.IsNullOrWhiteSpace(dto.Path))
                cacheFolder.UpdatePath(dto.Path);

            if (dto.MaxSize > 0)
                cacheFolder.UpdateMaxSize(dto.MaxSize);

            if (dto.Priority > 0)
                cacheFolder.UpdatePriority(dto.Priority);

            cacheFolder.SetActive(dto.IsActive);

            var updated = await _cacheFolderRepository.UpdateAsync(cacheFolder);

            return new CacheFolderDto
            {
                Id = updated.Id,
                Name = updated.Name,
                Path = updated.Path,
                MaxSize = updated.MaxSizeBytes,
                CurrentSize = updated.CurrentSize,
                Priority = updated.Priority,
                IsActive = updated.IsActive,
                CreatedAt = updated.CreatedAt,
                UpdatedAt = updated.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cache folder: {Id}", id);
            throw;
        }
    }

    public async Task DeleteCacheFolderAsync(ObjectId id)
    {
        _logger.LogInformation("Deleting cache folder: {Id}", id);

        try
        {
            var cacheFolder = await _cacheFolderRepository.GetByIdAsync(id);
            if (cacheFolder == null)
            {
                throw new KeyNotFoundException($"Cache folder with ID {id} not found");
            }

            await _cacheFolderRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache folder: {Id}", id);
            throw;
        }
    }

    public async Task<CacheFolderDto> GetCacheFolderAsync(ObjectId id)
    {
        _logger.LogInformation("Getting cache folder: {Id}", id);

        try
        {
            var cacheFolder = await _cacheFolderRepository.GetByIdAsync(id);
            if (cacheFolder == null)
            {
                throw new KeyNotFoundException($"Cache folder with ID {id} not found");
            }

            return new CacheFolderDto
            {
                Id = cacheFolder.Id,
                Name = cacheFolder.Name,
                Path = cacheFolder.Path,
                MaxSize = cacheFolder.MaxSizeBytes,
                CurrentSize = cacheFolder.CurrentSize,
                Priority = cacheFolder.Priority,
                IsActive = cacheFolder.IsActive,
                CreatedAt = cacheFolder.CreatedAt,
                UpdatedAt = cacheFolder.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folder: {Id}", id);
            throw;
        }
    }

    public async Task ClearCollectionCacheAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Clearing cache for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new KeyNotFoundException($"Collection with ID {collectionId} not found");
            }

            // Clear cache info from all images in the collection
            foreach (var image in collection.Images)
            {
                if (image.CacheInfo != null)
                {
                    // Delete the physical cache file
                    if (File.Exists(image.CacheInfo.CachePath))
                    {
                        try
                        {
                            File.Delete(image.CacheInfo.CachePath);
                            _logger.LogDebug("Deleted cache file: {CachePath}", image.CacheInfo.CachePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete cache file: {CachePath}", image.CacheInfo.CachePath);
                        }
                    }

                    // Clear cache info from embedded image
                    image.ClearCacheInfo();
                }
            }

            await _collectionRepository.UpdateAsync(collection);
            _logger.LogInformation("Cache cleared for collection: {CollectionId}", collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task ClearAllCacheAsync()
    {
        _logger.LogInformation("Clearing all cache");

        try
        {
            var collections = await _collectionRepository.GetAllAsync();

            foreach (var collection in collections)
            {
                foreach (var image in collection.Images)
                {
                    if (image.CacheInfo != null)
                    {
                        // Delete the physical cache file
                        if (File.Exists(image.CacheInfo.CachePath))
                        {
                            try
                            {
                                File.Delete(image.CacheInfo.CachePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete cache file: {CachePath}", image.CacheInfo.CachePath);
                            }
                        }

                        // Clear cache info
                        image.ClearCacheInfo();
                    }
                }

                await _collectionRepository.UpdateAsync(collection);
            }

            _logger.LogInformation("All cache cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache");
            throw;
        }
    }

    public async Task<CollectionCacheStatusDto> GetCollectionCacheStatusAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Getting cache status for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new KeyNotFoundException($"Collection with ID {collectionId} not found");
            }

            var activeImages = collection.Images.Where(i => !i.IsDeleted).ToList();
            var totalImages = activeImages.Count;
            var cachedImages = activeImages.Count(i => i.CacheInfo != null);
            var cachePercentage = totalImages > 0 ? (double)cachedImages / totalImages * 100 : 0;
            var totalCacheSize = activeImages.Where(i => i.CacheInfo != null).Sum(i => i.CacheInfo!.CacheSize);

            return new CollectionCacheStatusDto
            {
                CollectionId = collectionId,
                TotalImages = totalImages,
                CachedImages = cachedImages,
                CachePercentage = cachePercentage,
                LastCacheUpdate = activeImages
                    .Where(i => i.CacheInfo != null)
                    .Select(i => i.CacheInfo!.GeneratedAt)
                    .DefaultIfEmpty(null)
                    .Max()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache status for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task RegenerateCollectionCacheAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Regenerating cache for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new KeyNotFoundException($"Collection with ID {collectionId} not found");
            }

            // Use IImageService to regenerate cache for all images
            // This will be called via background jobs, so just log for now
            _logger.LogInformation("Cache regeneration for collection {CollectionId} should be handled by background jobs", collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating cache for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task RegenerateCollectionCacheAsync(ObjectId collectionId, IEnumerable<(int Width, int Height)> sizes)
    {
        _logger.LogInformation("Regenerating cache for collection: {CollectionId} with custom sizes", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new KeyNotFoundException($"Collection with ID {collectionId} not found");
            }

            // Use IImageService to regenerate cache for all images with custom sizes
            // This will be called via background jobs, so just log for now
            _logger.LogInformation("Cache regeneration for collection {CollectionId} with {SizeCount} custom sizes should be handled by background jobs", 
                collectionId, sizes.Count());
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
            // Parse imageId as string and search across all collections
            var imageIdStr = imageId.ToString();
            var collections = await _collectionRepository.GetAllAsync();

            foreach (var collection in collections)
            {
                var image = collection.Images.FirstOrDefault(i => i.Id == imageIdStr && !i.IsDeleted);
                if (image?.CacheInfo != null)
                {
                    var cachePath = image.CacheInfo.CachePath;
                    if (File.Exists(cachePath))
                    {
                        return await File.ReadAllBytesAsync(cachePath, cancellationToken);
                    }
                }
            }

            _logger.LogDebug("No valid cache found for image: {ImageId}", imageId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached image: {ImageId}", imageId);
            throw;
        }
    }

    public async Task SaveCachedImageAsync(ObjectId imageId, string dimensions, byte[] imageData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving cached image: {ImageId} with dimensions: {Dimensions}", imageId, dimensions);

        try
        {
            // This method should not be called directly - cache is saved via IImageService.GenerateCacheAsync
            _logger.LogWarning("SaveCachedImageAsync called directly - this should be handled by IImageService.GenerateCacheAsync");
            throw new NotSupportedException("Use IImageService.GenerateCacheAsync instead");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cached image: {ImageId}", imageId);
            throw;
        }
    }

    public async Task CleanupExpiredCacheAsync()
    {
        _logger.LogInformation("Starting cleanup of expired cache");

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            int deletedCount = 0;

            foreach (var collection in collections)
            {
                bool modified = false;
                foreach (var image in collection.Images)
                {
                    if (image.CacheInfo != null)
                    {
                        // Check if cache file still exists
                        if (!File.Exists(image.CacheInfo.CachePath))
                        {
                            image.SetCacheInfo(null!);
                            modified = true;
                            deletedCount++;
                        }
                    }
                }

                if (modified)
                {
                    await _collectionRepository.UpdateAsync(collection);
                }
            }

            _logger.LogInformation("Cleanup completed. Removed {Count} expired cache entries", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
            throw;
        }
    }

    public async Task CleanupOldCacheAsync(DateTime cutoffDate)
    {
        _logger.LogInformation("Starting cleanup of old cache entries (before {CutoffDate})", cutoffDate);

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            int deletedCount = 0;

            foreach (var collection in collections)
            {
                bool modified = false;
                foreach (var image in collection.Images)
                {
                    if (image.CacheInfo != null && image.CacheInfo.GeneratedAt < cutoffDate)
                    {
                        // Delete the physical cache file
                        if (File.Exists(image.CacheInfo.CachePath))
                        {
                            try
                            {
                                File.Delete(image.CacheInfo.CachePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete cache file: {CachePath}", image.CacheInfo.CachePath);
                            }
                        }

                        image.SetCacheInfo(null!);
                        modified = true;
                        deletedCount++;
                    }
                }

                if (modified)
                {
                    await _collectionRepository.UpdateAsync(collection);
                }
            }

            _logger.LogInformation("Cleanup completed. Removed {Count} old cache entries", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during old cache cleanup");
            throw;
        }
    }

    public async Task<CacheDistributionStatisticsDto> GetCacheDistributionStatisticsAsync()
    {
        _logger.LogInformation("Getting cache folder distribution statistics");

        try
        {
            var cacheFolders = await _cacheFolderRepository.GetAllAsync();
            var cacheFoldersList = cacheFolders.ToList();
            var collections = await _collectionRepository.GetAllAsync();

            // Calculate distribution across cache folders
            var folderDistribution = new Dictionary<string, CacheFolderDistributionDto>();

            foreach (var cacheFolder in cacheFoldersList)
            {
                folderDistribution[cacheFolder.Id.ToString()] = new CacheFolderDistributionDto
                {
                    CacheFolderId = cacheFolder.Id,
                    CacheFolderName = cacheFolder.Name,
                    CacheFolderPath = cacheFolder.Path,
                    FileCount = 0,
                    TotalSizeBytes = 0,
                    MaxSizeBytes = cacheFolder.MaxSizeBytes,
                    UsagePercentage = 0,
                    IsActive = cacheFolder.IsActive
                };
            }

            // Count cache items per folder
            foreach (var collection in collections)
            {
                foreach (var image in collection.Images.Where(i => !i.IsDeleted && i.CacheInfo != null))
                {
                    // Find which cache folder this cache belongs to
                    var cachePath = image.CacheInfo!.CachePath;
                    var cacheFolder = cacheFoldersList.FirstOrDefault(cf => cachePath.StartsWith(cf.Path));
                    
                    if (cacheFolder != null)
                    {
                        var folderId = cacheFolder.Id.ToString();
                        if (folderDistribution.ContainsKey(folderId))
                        {
                            folderDistribution[folderId].FileCount++;
                            folderDistribution[folderId].TotalSizeBytes += image.CacheInfo.CacheSize;
                        }
                    }
                }
            }

            var distributions = folderDistribution.Values.ToList();
            var totalItems = distributions.Sum(d => d.FileCount);
            var totalSize = distributions.Sum(d => d.TotalSizeBytes);

            // Calculate usage percentages
            foreach (var dist in distributions)
            {
                dist.UsagePercentage = dist.MaxSizeBytes > 0 
                    ? (double)dist.TotalSizeBytes / dist.MaxSizeBytes * 100 
                    : 0;
            }

            // Calculate balance metrics
            var avgItemsPerFolder = cacheFoldersList.Count > 0 ? (double)totalItems / cacheFoldersList.Count : 0;
            var avgSizePerFolder = cacheFoldersList.Count > 0 ? (double)totalSize / cacheFoldersList.Count : 0;
            
            var fileCountVariance = distributions.Any() 
                ? distributions.Average(d => Math.Pow(d.FileCount - avgItemsPerFolder, 2))
                : 0;
            var sizeVariance = distributions.Any()
                ? distributions.Average(d => Math.Pow(d.TotalSizeBytes - avgSizePerFolder, 2))
                : 0;
            var fileCountStdDev = Math.Sqrt(fileCountVariance);
            var sizeStdDev = Math.Sqrt(sizeVariance);
            
            var isWellBalanced = fileCountStdDev < avgItemsPerFolder * 0.2; // Within 20% of average

            return new CacheDistributionStatisticsDto
            {
                TotalCacheFolders = cacheFoldersList.Count,
                TotalFiles = totalItems,
                TotalSizeBytes = totalSize,
                AverageFilesPerFolder = avgItemsPerFolder,
                AverageSizePerFolder = avgSizePerFolder,
                DistributionBalance = new DistributionBalanceDto
                {
                    FileCountVariance = fileCountVariance,
                    SizeVariance = sizeVariance,
                    FileCountStandardDeviation = fileCountStdDev,
                    SizeStandardDeviation = sizeStdDev,
                    IsWellBalanced = isWellBalanced
                },
                CacheFolderDistributions = distributions,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache distribution statistics");
            throw;
        }
    }
}

