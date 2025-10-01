using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Cache info repository implementation
/// </summary>
public class CacheInfoRepository : Repository<ImageCacheInfo>, ICacheInfoRepository
{
    public CacheInfoRepository(ImageViewerDbContext context, ILogger<CacheInfoRepository> logger) : base(context, logger)
    {
    }

    public async Task<ImageCacheInfo?> GetByImageIdAsync(Guid imageId)
    {
        try
        {
            return await _dbSet.FirstOrDefaultAsync(ci => ci.ImageId == imageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache info by image ID: {ImageId}", imageId);
            throw;
        }
    }

    public async Task<IEnumerable<ImageCacheInfo>> GetByCacheFolderIdAsync(Guid cacheFolderId)
    {
        try
        {
            return await _dbSet.ToListAsync(); // Note: ImageCacheInfo doesn't have CacheFolderId property
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache info by cache folder ID: {CacheFolderId}", cacheFolderId);
            throw;
        }
    }

    public async Task<IEnumerable<ImageCacheInfo>> GetExpiredAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _dbSet.Where(ci => ci.ExpiresAt < now).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired cache entries");
            throw;
        }
    }

    public async Task<IEnumerable<ImageCacheInfo>> GetOlderThanAsync(DateTime cutoffDate)
    {
        try
        {
            return await _dbSet.Where(ci => ci.CachedAt < cutoffDate).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache entries older than {CutoffDate}", cutoffDate);
            throw;
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var cacheInfos = await _dbSet.ToListAsync();
            return new CacheStatistics
            {
                TotalCacheEntries = cacheInfos.Count,
                TotalCacheSize = cacheInfos.Sum(ci => ci.FileSizeBytes),
                ValidCacheEntries = cacheInfos.Count(ci => ci.IsValid),
                ExpiredCacheEntries = cacheInfos.Count(ci => ci.ExpiresAt < DateTime.UtcNow),
                AverageCacheSize = cacheInfos.Any() ? cacheInfos.Average(ci => ci.FileSizeBytes) : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            throw;
        }
    }
}
