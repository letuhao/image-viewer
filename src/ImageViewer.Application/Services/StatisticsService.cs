using MongoDB.Bson;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Application.DTOs.Statistics;
using ImageViewer.Application.DTOs.Cache;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Statistics service implementation using embedded design
/// Refactored to use ImageEmbedded instead of separate Image entity
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IViewSessionRepository _viewSessionRepository;
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
        ICollectionRepository collectionRepository,
        IViewSessionRepository viewSessionRepository,
        IBackgroundJobRepository backgroundJobRepository,
        ICacheService cacheService,
        ILogger<StatisticsService> logger)
    {
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _viewSessionRepository = viewSessionRepository ?? throw new ArgumentNullException(nameof(viewSessionRepository));
        _backgroundJobRepository = backgroundJobRepository ?? throw new ArgumentNullException(nameof(backgroundJobRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Getting statistics for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new KeyNotFoundException($"Collection with ID {collectionId} not found");
            }

            var activeImages = collection.GetActiveImages();
            var totalImages = activeImages.Count;
            var totalSize = activeImages.Sum(i => i.FileSize);
            var averageFileSize = totalImages > 0 ? totalSize / totalImages : 0;
            var cachedImages = 0; // TODO: Use embedded Collection.Images[].CacheInfo
            var cachePercentage = totalImages > 0 ? (double)cachedImages / totalImages * 100 : 0;

            // Get view sessions for this collection
            var viewSessions = await _viewSessionRepository.GetByCollectionIdAsync(collectionId);
            var viewSessionsList = viewSessions.ToList();

            // Get popular images (top 10 by view count)
            var popularImages = activeImages
                .OrderByDescending(i => i.ViewCount)
                .Take(10)
                .Select(i => new PopularImageDto
                {
                    Id = ObjectId.Parse(i.Id),
                    Filename = i.Filename,
                    ViewCount = i.ViewCount
                })
                .ToList();

            return new CollectionStatisticsDto
            {
                CollectionId = collectionId,
                ViewCount = (int)collection.Statistics.TotalViews,
                TotalViewTime = viewSessionsList.Sum(vs => vs.TotalViewTime.TotalSeconds),
                SearchCount = 0, // CollectionStatistics doesn't have TotalSearches
                LastViewed = collection.Statistics.LastViewed,
                LastSearched = null, // CollectionStatistics doesn't have LastSearched
                AverageViewTime = viewSessionsList.Any() 
                    ? viewSessionsList.Average(vs => vs.TotalViewTime.TotalSeconds) 
                    : 0,
                TotalImages = totalImages,
                TotalSize = totalSize,
                AverageFileSize = averageFileSize,
                CachedImages = cachedImages,
                CachePercentage = cachePercentage,
                PopularImages = popularImages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection statistics: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<SystemStatisticsDto> GetSystemStatisticsAsync()
    {
        _logger.LogInformation("Getting system statistics");

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            var collectionsList = collections.ToList();
            var viewSessions = await _viewSessionRepository.GetAllAsync();
            var viewSessionsList = viewSessions.ToList();

            var totalCollections = collectionsList.Count;
            var totalImages = collectionsList.Sum(c => c.GetActiveImages().Count);
            var totalSize = collectionsList.Sum(c => c.GetActiveImages().Sum(i => i.FileSize));
            var totalCacheSize = collectionsList.Sum(c => 
                c.GetActiveImages().Where(i => i.CacheInfo != null).Sum(i => i.CacheInfo!.CacheSize));
            
            var totalViewSessions = viewSessionsList.Count;
            var totalViewTime = viewSessionsList.Sum(vs => vs.TotalViewTime.TotalSeconds);
            var averageImagesPerCollection = totalCollections > 0 ? (double)totalImages / totalCollections : 0;
            var averageViewTimePerSession = totalViewSessions > 0 ? totalViewTime / totalViewSessions : 0;

            return new SystemStatisticsDto
            {
                TotalCollections = totalCollections,
                TotalImages = totalImages,
                TotalSize = totalSize,
                TotalCacheSize = totalCacheSize,
                TotalViewSessions = totalViewSessions,
                TotalViewTime = totalViewTime,
                AverageImagesPerCollection = averageImagesPerCollection,
                AverageViewTimePerSession = averageViewTimePerSession
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system statistics");
            throw;
        }
    }

    public async Task<ImageStatisticsDto> GetImageStatisticsAsync(ObjectId imageId)
    {
        _logger.LogInformation("Getting image statistics: {ImageId}", imageId);

        try
        {
            // Since images are embedded, we need to search across collections
            // For now, return statistics for a specific image if found
            var collections = await _collectionRepository.GetAllAsync();
            var imageIdStr = imageId.ToString();

            foreach (var collection in collections)
            {
                var image = collection.Images.FirstOrDefault(i => i.Id == imageIdStr && !i.IsDeleted);
                if (image != null)
                {
                    return new ImageStatisticsDto
                    {
                        TotalImages = 1,
                        TotalSize = image.FileSize,
                        AverageFileSize = image.FileSize,
                        CachedImages = 0, // TODO: Use embedded Collection.Images[].CacheInfo
                        CachePercentage = 0, // TODO: Use embedded Collection.Images[].CacheInfo
                        FormatStatistics = new[]
                        {
                            new FormatStatisticsDto
                            {
                                Format = image.Format,
                                Count = 1,
                                TotalSize = image.FileSize,
                                AverageSize = image.FileSize
                            }
                        }
                    };
                }
            }

            throw new KeyNotFoundException($"Image with ID {imageId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image statistics: {ImageId}", imageId);
            throw;
        }
    }

    public async Task<CacheStatisticsDto> GetCacheStatisticsAsync()
    {
        _logger.LogInformation("Getting cache statistics");

        try
        {
            // Delegate to CacheService
            return await _cacheService.GetCacheStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            throw;
        }
    }

    public async Task<UserActivityStatisticsDto> GetUserActivityStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Getting user activity statistics from {FromDate} to {ToDate}", fromDate, toDate);

        try
        {
            var viewSessions = await _viewSessionRepository.GetAllAsync();
            var filteredSessions = viewSessions
                .Where(vs => (!fromDate.HasValue || vs.StartedAt >= fromDate.Value) &&
                             (!toDate.HasValue || vs.StartedAt <= toDate.Value))
                .ToList();

            var totalSessions = filteredSessions.Count;
            var totalViewTime = filteredSessions.Sum(vs => vs.TotalViewTime.TotalSeconds);
            var averageViewTime = totalSessions > 0 ? totalViewTime / totalSessions : 0;

            // Group by date
            var dailyActivity = filteredSessions
                .GroupBy(vs => vs.StartedAt.Date)
                .Select(g => new DailyActivityDto
                {
                    Date = g.Key,
                    Sessions = g.Count(),
                    TotalViewTime = g.Sum(vs => vs.TotalViewTime.TotalSeconds)
                })
                .OrderBy(da => da.Date)
                .ToList();

            return new UserActivityStatisticsDto
            {
                TotalSessions = totalSessions,
                TotalViewTime = totalViewTime,
                AverageViewTime = averageViewTime,
                DailyActivity = dailyActivity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity statistics");
            throw;
        }
    }

    public async Task<PerformanceStatisticsDto> GetPerformanceStatisticsAsync()
    {
        _logger.LogInformation("Getting performance statistics");

        try
        {
            var backgroundJobs = await _backgroundJobRepository.GetAllAsync();
            var completedJobs = backgroundJobs.Where(j => j.Status == "Completed").ToList();
            var failedJobs = backgroundJobs.Where(j => j.Status == "Failed").ToList();
            
            var totalRequests = backgroundJobs.Count();
            var successfulRequests = completedJobs.Count;
            var failedRequests = failedJobs.Count;
            var successRate = totalRequests > 0 ? (double)successfulRequests / totalRequests * 100 : 0;

            // Calculate average processing time from completed jobs
            var avgResponseTime = completedJobs.Any() && completedJobs.Any(j => j.CompletedAt.HasValue)
                ? completedJobs
                    .Where(j => j.CompletedAt.HasValue)
                    .Average(j => (j.CompletedAt!.Value - j.CreatedAt).TotalMilliseconds)
                : 0;

            return new PerformanceStatisticsDto
            {
                AverageResponseTime = avgResponseTime,
                AverageImageLoadTime = 0, // TODO: Track from metrics
                AverageThumbnailGenerationTime = 0, // TODO: Track from metrics
                AverageCacheHitRate = 0, // TODO: Track from metrics
                TotalRequests = totalRequests,
                SuccessfulRequests = successfulRequests,
                FailedRequests = failedRequests,
                SuccessRate = successRate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance statistics");
            throw;
        }
    }

    public async Task<StorageStatisticsDto> GetStorageStatisticsAsync()
    {
        _logger.LogInformation("Getting storage statistics");

        try
        {
            var collections = await _collectionRepository.GetAllAsync();
            var collectionsList = collections.ToList();

            var totalImageSize = collectionsList.Sum(c => c.GetActiveImages().Sum(i => i.FileSize));
            var totalCacheSize = collectionsList.Sum(c => 
                c.GetActiveImages().Where(i => i.CacheInfo != null).Sum(i => i.CacheInfo!.CacheSize));

            var cacheStatistics = await _cacheService.GetCacheStatisticsAsync();
            var cacheFolders = cacheStatistics.CacheFolders.Select(cf => new CacheFolderStorageDto
            {
                Id = cf.Id,
                Name = cf.Name,
                Path = cf.Path,
                MaxSize = cf.MaxSize,
                CurrentSize = cf.CurrentSize,
                UsagePercentage = cf.MaxSize > 0 ? (double)cf.CurrentSize / cf.MaxSize * 100 : 0
            }).ToList();

            return new StorageStatisticsDto
            {
                TotalImageSize = totalImageSize,
                TotalCacheSize = totalCacheSize,
                TotalStorageSize = totalImageSize + totalCacheSize,
                CacheFolders = cacheFolders
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage statistics");
            throw;
        }
    }

    public async Task<IEnumerable<PopularImageDto>> GetPopularImagesAsync(ObjectId collectionId, int limit = 10)
    {
        _logger.LogInformation("Getting popular images for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new KeyNotFoundException($"Collection with ID {collectionId} not found");
            }

            var popularImages = collection.GetActiveImages()
                .OrderByDescending(i => i.ViewCount)
                .Take(limit)
                .Select(i => new PopularImageDto
                {
                    Id = ObjectId.Parse(i.Id),
                    Filename = i.Filename,
                    ViewCount = i.ViewCount
                })
                .ToList();

            return popularImages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular images for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int limit = 20)
    {
        _logger.LogInformation("Getting recent activity (limit: {Limit})", limit);

        try
        {
            var viewSessions = await _viewSessionRepository.GetAllAsync();
            var recentSessions = viewSessions
                .OrderByDescending(vs => vs.StartedAt)
                .Take(limit)
                .Select(vs => new RecentActivityDto
                {
                    Id = Guid.NewGuid(), // Generate ID for DTO
                    Type = "ViewSession",
                    Description = $"Viewed collection {vs.CollectionId}",
                    Timestamp = vs.StartedAt,
                    Duration = vs.TotalViewTime.TotalSeconds
                })
                .ToList();

            return recentSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activity");
            throw;
        }
    }

    public async Task<StatisticsSummaryDto> GetStatisticsSummaryAsync()
    {
        _logger.LogInformation("Getting statistics summary");

        try
        {
            var systemStats = await GetSystemStatisticsAsync();
            var cacheStats = await GetCacheStatisticsAsync();
            var performanceStats = await GetPerformanceStatisticsAsync();

            // Get overall image statistics
            var collections = await _collectionRepository.GetAllAsync();
            var allImages = collections.SelectMany(c => c.GetActiveImages()).ToList();
            
            var imageStats = new ImageStatisticsDto
            {
                TotalImages = allImages.Count,
                TotalSize = allImages.Sum(i => i.FileSize),
                AverageFileSize = allImages.Any() ? allImages.Sum(i => i.FileSize) / allImages.Count : 0,
                CachedImages = allImages.Count(i => i.CacheInfo != null),
                CachePercentage = allImages.Any() 
                    ? (double)allImages.Count(i => i.CacheInfo != null) / allImages.Count * 100 
                    : 0,
                FormatStatistics = allImages
                    .GroupBy(i => i.Format)
                    .Select(g => new FormatStatisticsDto
                    {
                        Format = g.Key,
                        Count = g.Count(),
                        TotalSize = g.Sum(i => i.FileSize),
                        AverageSize = g.Average(i => i.FileSize)
                    })
                    .OrderByDescending(fs => fs.Count)
                    .ToList()
            };

            return new StatisticsSummaryDto
            {
                System = systemStats,
                Images = imageStats,
                Cache = cacheStats,
                Performance = performanceStats,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics summary");
            throw;
        }
    }
}

