namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Service interface for caching images in Redis
/// 中文：Redis图片缓存服务接口
/// Tiếng Việt: Giao diện dịch vụ bộ nhớ đệm hình ảnh Redis
/// </summary>
public interface IImageCacheService
{
    /// <summary>
    /// Get cached image bytes from Redis
    /// Returns null if not found in cache
    /// </summary>
    Task<byte[]?> GetCachedImageAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache image bytes in Redis with expiration
    /// </summary>
    Task SetCachedImageAsync(string key, byte[] imageBytes, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove cached image from Redis
    /// </summary>
    Task RemoveCachedImageAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if image exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics (hit rate, size, count)
    /// </summary>
    Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all cached images
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate cache key for thumbnail
    /// </summary>
    string GetThumbnailCacheKey(string collectionId, string thumbnailId);

    /// <summary>
    /// Generate cache key for cache image
    /// </summary>
    string GetCacheImageCacheKey(string collectionId, string cacheImageId);
}

/// <summary>
/// Cache statistics model
/// </summary>
public class CacheStatistics
{
    public long TotalKeys { get; set; }
    public long UsedMemoryBytes { get; set; }
    public long MaxMemoryBytes { get; set; }
    public double UsagePercentage => MaxMemoryBytes > 0 ? (double)UsedMemoryBytes / MaxMemoryBytes * 100 : 0;
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) * 100 : 0;
}

