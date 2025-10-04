using ImageViewer.Application.DTOs.Cache;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Cache service interface for managing cache operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatisticsDto> GetCacheStatisticsAsync();

    /// <summary>
    /// Get cache folders
    /// </summary>
    Task<IEnumerable<CacheFolderDto>> GetCacheFoldersAsync();

    /// <summary>
    /// Create cache folder
    /// </summary>
    Task<CacheFolderDto> CreateCacheFolderAsync(CreateCacheFolderDto dto);

    /// <summary>
    /// Update cache folder
    /// </summary>
    Task<CacheFolderDto> UpdateCacheFolderAsync(ObjectId id, UpdateCacheFolderDto dto);

    /// <summary>
    /// Delete cache folder
    /// </summary>
    Task DeleteCacheFolderAsync(ObjectId id);

    /// <summary>
    /// Get cache folder by ID
    /// </summary>
    Task<CacheFolderDto> GetCacheFolderAsync(ObjectId id);

    /// <summary>
    /// Clear cache for collection
    /// </summary>
    Task ClearCollectionCacheAsync(ObjectId collectionId);

    /// <summary>
    /// Clear all cache
    /// </summary>
    Task ClearAllCacheAsync();

    /// <summary>
    /// Get cache status for collection
    /// </summary>
    Task<CollectionCacheStatusDto> GetCollectionCacheStatusAsync(ObjectId collectionId);

    /// <summary>
    /// Regenerate cache for collection
    /// </summary>
    Task RegenerateCollectionCacheAsync(ObjectId collectionId);
    Task RegenerateCollectionCacheAsync(ObjectId collectionId, IEnumerable<(int Width, int Height)> sizes);

    /// <summary>
    /// Get cached image
    /// </summary>
    Task<byte[]?> GetCachedImageAsync(ObjectId imageId, string dimensions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save cached image
    /// </summary>
    Task SaveCachedImageAsync(ObjectId imageId, string dimensions, byte[] imageData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup expired cache entries
    /// </summary>
    Task CleanupExpiredCacheAsync();

    /// <summary>
    /// Cleanup old cache entries
    /// </summary>
    Task CleanupOldCacheAsync(DateTime cutoffDate);
}
