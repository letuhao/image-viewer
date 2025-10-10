using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for getting thumbnails as Base64 with Redis caching
/// 中文：缩略图Base64缓存服务
/// Tiếng Việt: Dịch vụ bộ nhớ đệm hình thu nhỏ Base64
/// </summary>
public class ThumbnailCacheService : IThumbnailCacheService
{
    private readonly IImageCacheService _imageCacheService;
    private readonly ILogger<ThumbnailCacheService> _logger;

    public ThumbnailCacheService(
        IImageCacheService imageCacheService,
        ILogger<ThumbnailCacheService> logger)
    {
        _imageCacheService = imageCacheService ?? throw new ArgumentNullException(nameof(imageCacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get thumbnail as Base64 data URL with Redis caching
    /// Returns null if thumbnail not found or invalid
    /// </summary>
    public async Task<string?> GetThumbnailAsBase64Async(
        string collectionId, 
        ThumbnailEmbedded? thumbnail, 
        CancellationToken cancellationToken = default)
    {
        if (thumbnail == null || !thumbnail.IsGenerated || !thumbnail.IsValid || string.IsNullOrEmpty(thumbnail.ThumbnailPath))
        {
            return null;
        }

        try
        {
            // Generate Redis cache key
            var cacheKey = _imageCacheService.GetThumbnailCacheKey(collectionId, thumbnail.Id);

            // Try to get from Redis cache
            var cachedBytes = await _imageCacheService.GetCachedImageAsync(cacheKey, cancellationToken);
            
            if (cachedBytes != null)
            {
                // Convert to base64 data URL
                var base64 = Convert.ToBase64String(cachedBytes);
                var contentType = GetContentType(thumbnail.Format);
                return $"data:{contentType};base64,{base64}";
            }

            // Cache miss - load from disk
            if (!File.Exists(thumbnail.ThumbnailPath))
            {
                _logger.LogWarning("Thumbnail file not found at path: {ThumbnailPath}", thumbnail.ThumbnailPath);
                return null;
            }

            // Read from disk
            var fileBytes = await File.ReadAllBytesAsync(thumbnail.ThumbnailPath, cancellationToken);

            // Cache in Redis for future requests
            await _imageCacheService.SetCachedImageAsync(cacheKey, fileBytes, cancellationToken: cancellationToken);

            // Convert to base64 data URL
            var base64String = Convert.ToBase64String(fileBytes);
            var mimeType = GetContentType(thumbnail.Format);
            return $"data:{mimeType};base64,{base64String}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail as base64 for collection {CollectionId}, thumbnail {ThumbnailId}", 
                collectionId, thumbnail.Id);
            return null;
        }
    }

    /// <summary>
    /// Get content type for thumbnail format
    /// </summary>
    private static string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            _ => "image/jpeg" // Default fallback
        };
    }
}

/// <summary>
/// Interface for thumbnail caching service
/// </summary>
public interface IThumbnailCacheService
{
    /// <summary>
    /// Get thumbnail as Base64 data URL with Redis caching
    /// </summary>
    Task<string?> GetThumbnailAsBase64Async(string collectionId, ThumbnailEmbedded? thumbnail, CancellationToken cancellationToken = default);
}

