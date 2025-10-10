using ImageViewer.Application.Options;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.IO.Compression;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Redis-based image caching service implementation
/// 中文：基于Redis的图片缓存服务实现
/// Tiếng Việt: Triển khai dịch vụ bộ nhớ đệm hình ảnh Redis
/// </summary>
public class RedisImageCacheService : IImageCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisImageCacheService> _logger;
    private readonly RedisOptions _options;
    private long _hitCount = 0;
    private long _missCount = 0;

    public RedisImageCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisImageCacheService> logger,
        IOptions<RedisOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<byte[]?> GetCachedImageAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _cache.GetAsync(key, cancellationToken);
            
            if (cachedData == null)
            {
                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("Cache MISS for key: {Key}", key);
                return null;
            }

            Interlocked.Increment(ref _hitCount);
            _logger.LogDebug("Cache HIT for key: {Key}, size: {Size} bytes", key, cachedData.Length);

            // Decompress if compression is enabled
            if (_options.EnableCompression)
            {
                return await DecompressAsync(cachedData, cancellationToken);
            }

            return cachedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached image for key: {Key}", key);
            return null;
        }
    }

    public async Task SetCachedImageAsync(string key, byte[] imageBytes, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataToCache = imageBytes;

            // Compress if compression is enabled
            if (_options.EnableCompression)
            {
                dataToCache = await CompressAsync(imageBytes, cancellationToken);
                _logger.LogDebug("Compressed image from {Original} to {Compressed} bytes ({Ratio:F2}% reduction)", 
                    imageBytes.Length, dataToCache.Length, 
                    (1 - (double)dataToCache.Length / imageBytes.Length) * 100);
            }

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(_options.ImageCacheExpirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(_options.SlidingExpirationMinutes)
            };

            await _cache.SetAsync(key, dataToCache, cacheOptions, cancellationToken);
            _logger.LogInformation("Cached image at key: {Key}, size: {Size} bytes, expiration: {Expiration} minutes", 
                key, dataToCache.Length, _options.ImageCacheExpirationMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching image for key: {Key}", key);
        }
    }

    public async Task RemoveCachedImageAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogInformation("Removed cached image at key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached image for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for key: {Key}", key);
            return false;
        }
    }

    public async Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync("memory");
            var stats = await server.InfoAsync("stats");

            var usedMemory = long.Parse(info.First(x => x.Key == "used_memory").Value);
            var maxMemory = long.Parse(info.First(x => x.Key == "maxmemory").Value);
            var totalKeys = await server.DatabaseSizeAsync();

            return new CacheStatistics
            {
                TotalKeys = totalKeys,
                UsedMemoryBytes = usedMemory,
                MaxMemoryBytes = maxMemory,
                HitCount = _hitCount,
                MissCount = _missCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics
            {
                HitCount = _hitCount,
                MissCount = _missCount
            };
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            _logger.LogWarning("Cleared all cached images from Redis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache");
        }
    }

    public string GetThumbnailCacheKey(string collectionId, string thumbnailId)
    {
        return $"{_options.InstanceName}thumbnail:{collectionId}:{thumbnailId}";
    }

    public string GetCacheImageCacheKey(string collectionId, string cacheImageId)
    {
        return $"{_options.InstanceName}cache:{collectionId}:{cacheImageId}";
    }

    /// <summary>
    /// Compress byte array using GZip
    /// </summary>
    private static async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest))
        {
            await gzipStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Decompress byte array using GZip
    /// </summary>
    private static async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken)
    {
        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            await gzipStream.CopyToAsync(outputStream, cancellationToken);
        }
        return outputStream.ToArray();
    }
}

