using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// View session repository implementation
/// </summary>
public class ViewSessionRepository : Repository<ViewSession>, IViewSessionRepository
{
    public ViewSessionRepository(ImageViewerDbContext context, ILogger<ViewSessionRepository> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<ViewSession>> GetByCollectionIdAsync(Guid collectionId)
    {
        try
        {
            return await _dbSet
                .Where(vs => vs.CollectionId == collectionId)
                .Include(vs => vs.Collection)
                .Include(vs => vs.CurrentImage)
                .OrderByDescending(vs => vs.StartedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting view sessions by collection ID {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<ViewSession>> GetByUserIdAsync(string userId)
    {
        // Note: ViewSession entity doesn't currently track user ID
        // This method is kept for interface compliance but returns empty result
        _logger.LogWarning("GetByUserIdAsync called but ViewSession entity doesn't track user ID");
        return new List<ViewSession>();
    }

    public async Task<IEnumerable<ViewSession>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await _dbSet
                .Where(vs => vs.StartedAt >= fromDate && vs.StartedAt <= toDate)
                .Include(vs => vs.Collection)
                .Include(vs => vs.CurrentImage)
                .OrderByDescending(vs => vs.StartedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting view sessions by date range {FromDate} to {ToDate}", fromDate, toDate);
            throw;
        }
    }

    public async Task<IEnumerable<ViewSession>> GetRecentAsync(int limit = 20)
    {
        try
        {
            return await _dbSet
                .Include(vs => vs.Collection)
                .Include(vs => vs.CurrentImage)
                .OrderByDescending(vs => vs.StartedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent view sessions with limit {Limit}", limit);
            throw;
        }
    }

    public async Task<ViewSessionStatistics> GetStatisticsAsync()
    {
        try
        {
            var sessions = await _dbSet.ToListAsync();
            
            return new ViewSessionStatistics
            {
                TotalSessions = sessions.Count,
                TotalViewTime = sessions.Sum(s => s.TotalViewTime.TotalSeconds),
                AverageViewTime = sessions.Count > 0 ? sessions.Average(s => s.TotalViewTime.TotalSeconds) : 0,
                UniqueCollections = sessions.Select(s => s.CollectionId).Distinct().Count(),
                UniqueUsers = 1 // ViewSession entity doesn't currently track user ID
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting view session statistics");
            throw;
        }
    }

    public async Task<IEnumerable<PopularCollection>> GetPopularCollectionsAsync(int limit = 10)
    {
        try
        {
            return await _dbSet
                .GroupBy(vs => new { vs.CollectionId, vs.Collection.Name })
                .Select(g => new PopularCollection
                {
                    CollectionId = g.Key.CollectionId,
                    CollectionName = g.Key.Name,
                    ViewCount = g.Count(),
                    TotalViewTime = g.Sum(vs => vs.TotalViewTime.TotalSeconds)
                })
                .OrderByDescending(pc => pc.ViewCount)
                .ThenByDescending(pc => pc.TotalViewTime)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular collections with limit {Limit}", limit);
            throw;
        }
    }
}
