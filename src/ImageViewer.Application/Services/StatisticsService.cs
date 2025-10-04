using ImageViewer.Application.DTOs.Statistics;
using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Statistics service implementation
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IImageRepository _imageRepository;
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly IViewSessionRepository _viewSessionRepository;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
        ICollectionRepository collectionRepository,
        IImageRepository imageRepository,
        ICacheFolderRepository cacheFolderRepository,
        IViewSessionRepository viewSessionRepository,
        ILogger<StatisticsService> logger)
    {
        _collectionRepository = collectionRepository;
        _imageRepository = imageRepository;
        _cacheFolderRepository = cacheFolderRepository;
        _viewSessionRepository = viewSessionRepository;
        _logger = logger;
    }

    public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync(Guid collectionId)
    {
        _logger.LogInformation("Getting statistics for collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
        var viewSessions = await _viewSessionRepository.GetByCollectionIdAsync(collectionId);

        var totalImages = images.Count();
        var totalSize = images.Sum(i => i.FileSizeBytes);
        var averageFileSize = totalImages > 0 ? totalSize / totalImages : 0;
        var cachedImages = images.Count(i => i.CacheInfo != null);
        var cachePercentage = totalImages > 0 ? (double)cachedImages / totalImages * 100 : 0;

        var totalViewTime = viewSessions.Sum(vs => vs.ViewDuration.TotalSeconds);
        var averageViewTime = viewSessions.Any() ? totalViewTime / viewSessions.Count() : 0;
        var lastViewed = viewSessions.Max(vs => vs.CreatedAt);
        var lastSearched = viewSessions.Max(vs => vs.CreatedAt); // Assuming search is tracked in view sessions

        var popularImages = images
            .OrderByDescending(i => i.ViewCount)
            .Take(10)
            .Select(i => new PopularImageDto
            {
                Id = i.Id,
                Filename = i.Filename,
                ViewCount = i.ViewCount
            });

        return new CollectionStatisticsDto
        {
            CollectionId = collectionId,
            ViewCount = viewSessions.Count(),
            TotalViewTime = totalViewTime,
            SearchCount = viewSessions.Count(), // Assuming search count
            LastViewed = lastViewed,
            LastSearched = lastSearched,
            AverageViewTime = averageViewTime,
            TotalImages = totalImages,
            TotalSize = totalSize,
            AverageFileSize = averageFileSize,
            CachedImages = cachedImages,
            CachePercentage = cachePercentage,
            PopularImages = popularImages
        };
    }

    public async Task<SystemStatisticsDto> GetSystemStatisticsAsync()
    {
        _logger.LogInformation("Getting system statistics");

        var collections = await _collectionRepository.GetAllAsync();
        var images = await _imageRepository.GetAllAsync();
        var cacheFolders = await _cacheFolderRepository.GetAllAsync();
        var viewSessions = await _viewSessionRepository.GetAllAsync();

        var totalCollections = collections.Count();
        var totalImages = images.Count();
        var totalSize = images.Sum(i => i.FileSizeBytes);
        var totalCacheSize = cacheFolders.Sum(cf => cf.CurrentSize);
        var totalViewSessions = viewSessions.Count();
        var totalViewTime = viewSessions.Sum(vs => vs.ViewDuration.TotalSeconds);

        return new SystemStatisticsDto
        {
            TotalCollections = totalCollections,
            TotalImages = totalImages,
            TotalSize = totalSize,
            TotalCacheSize = totalCacheSize,
            TotalViewSessions = totalViewSessions,
            TotalViewTime = totalViewTime,
            AverageImagesPerCollection = totalCollections > 0 ? (double)totalImages / totalCollections : 0,
            AverageViewTimePerSession = totalViewSessions > 0 ? totalViewTime / totalViewSessions : 0
        };
    }

    public async Task<ImageStatisticsDto> GetImageStatisticsAsync(Guid imageId)
    {
        _logger.LogInformation("Getting image statistics for {ImageId}", imageId);
        var image = await _imageRepository.GetByIdAsync(imageId);
        if (image == null)
        {
            throw new ArgumentException($"Image with ID {imageId} not found");
        }

        var imageStats = new ImageStatisticsDto
        {
            TotalImages = 1,
            TotalSize = image.FileSizeBytes,
            AverageFileSize = image.FileSizeBytes,
            CachedImages = image.CacheInfo != null ? 1 : 0,
            CachePercentage = image.CacheInfo != null ? 100 : 0,
            FormatStatistics = new[]
            {
                new FormatStatisticsDto
                {
                    Format = image.Format,
                    Count = 1,
                    TotalSize = image.FileSizeBytes,
                    AverageSize = image.FileSizeBytes
                }
            }
        };

        return imageStats;
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

    public async Task<UserActivityStatisticsDto> GetUserActivityStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Getting user activity statistics from {FromDate} to {ToDate}", fromDate, toDate);

        var viewSessions = await _viewSessionRepository.GetAllAsync();

        if (fromDate.HasValue)
        {
            viewSessions = viewSessions.Where(vs => vs.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            viewSessions = viewSessions.Where(vs => vs.CreatedAt <= toDate.Value);
        }

        var totalSessions = viewSessions.Count();
        var totalViewTime = viewSessions.Sum(vs => vs.ViewDuration.TotalSeconds);
        var averageViewTime = totalSessions > 0 ? totalViewTime / totalSessions : 0;

        var dailyActivity = viewSessions
            .GroupBy(vs => vs.CreatedAt.Date)
            .Select(g => new DailyActivityDto
            {
                Date = g.Key,
                Sessions = g.Count(),
                TotalViewTime = g.Sum(vs => vs.ViewDuration.TotalSeconds)
            })
            .OrderBy(da => da.Date);

        return new UserActivityStatisticsDto
        {
            TotalSessions = totalSessions,
            TotalViewTime = totalViewTime,
            AverageViewTime = averageViewTime,
            DailyActivity = dailyActivity
        };
    }

    public Task<PerformanceStatisticsDto> GetPerformanceStatisticsAsync()
    {
        _logger.LogInformation("Getting performance statistics");

        // This would typically involve querying performance metrics
        // For now, we'll return mock data
        var statistics = new PerformanceStatisticsDto
        {
            AverageResponseTime = 150, // ms
            AverageImageLoadTime = 300, // ms
            AverageThumbnailGenerationTime = 50, // ms
            AverageCacheHitRate = 85.5, // percentage
            TotalRequests = 10000,
            SuccessfulRequests = 9800,
            FailedRequests = 200,
            SuccessRate = 98.0
        };
        return Task.FromResult(statistics);
    }

    public async Task<StorageStatisticsDto> GetStorageStatisticsAsync()
    {
        _logger.LogInformation("Getting storage statistics");

        var images = await _imageRepository.GetAllAsync();
        var cacheFolders = await _cacheFolderRepository.GetAllAsync();

        var totalImageSize = images.Sum(i => i.FileSizeBytes);
        var totalCacheSize = cacheFolders.Sum(cf => cf.CurrentSize);
        var totalStorageSize = totalImageSize + totalCacheSize;

        return new StorageStatisticsDto
        {
            TotalImageSize = totalImageSize,
            TotalCacheSize = totalCacheSize,
            TotalStorageSize = totalStorageSize,
            CacheFolders = cacheFolders.Select(cf => new CacheFolderStorageDto
            {
                Id = cf.Id,
                Name = cf.Name,
                Path = cf.Path,
                MaxSize = cf.MaxSize,
                CurrentSize = cf.CurrentSize,
                UsagePercentage = cf.MaxSize > 0 ? (double)cf.CurrentSize / cf.MaxSize * 100 : 0
            })
        };
    }

    public async Task<IEnumerable<PopularImageDto>> GetPopularImagesAsync(Guid collectionId, int limit = 10)
    {
        _logger.LogInformation("Getting popular images for collection: {CollectionId}", collectionId);

        var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
        return images
            .OrderByDescending(i => i.ViewCount)
            .Take(limit)
            .Select(i => new PopularImageDto
            {
                Id = i.Id,
                Filename = i.Filename,
                ViewCount = i.ViewCount
            });
    }

    public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int limit = 20)
    {
        _logger.LogInformation("Getting recent activity");

        var viewSessions = await _viewSessionRepository.GetAllAsync();
        return viewSessions
            .OrderByDescending(vs => vs.CreatedAt)
            .Take(limit)
            .Select(vs => new RecentActivityDto
            {
                Id = vs.Id,
                Type = "view",
                Description = $"Viewed collection: {vs.CollectionId}",
                Timestamp = vs.CreatedAt,
                Duration = vs.ViewDuration.TotalSeconds
            });
    }

    public async Task<StatisticsSummaryDto> GetStatisticsSummaryAsync()
    {
        _logger.LogInformation("Getting statistics summary");

        var systemStats = await GetSystemStatisticsAsync();
        var imageStats = await GetImageStatisticsAsync(Guid.Empty);
        var cacheStats = await GetCacheStatisticsAsync();
        var performanceStats = await GetPerformanceStatisticsAsync();

        return new StatisticsSummaryDto
        {
            System = systemStats,
            Images = imageStats,
            Cache = cacheStats,
            Performance = performanceStats,
            LastUpdated = DateTime.UtcNow
        };
    }
}
