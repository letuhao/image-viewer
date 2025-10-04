using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// View session repository interface
/// </summary>
public interface IViewSessionRepository : IRepository<ViewSession>
{
    /// <summary>
    /// Get view sessions by collection ID
    /// </summary>
    Task<IEnumerable<ViewSession>> GetByCollectionIdAsync(ObjectId collectionId);

    /// <summary>
    /// Get view sessions by user ID
    /// </summary>
    Task<IEnumerable<ViewSession>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Get view sessions by date range
    /// </summary>
    Task<IEnumerable<ViewSession>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get recent view sessions
    /// </summary>
    Task<IEnumerable<ViewSession>> GetRecentAsync(int limit = 20);

    /// <summary>
    /// Get view session statistics
    /// </summary>
    Task<ViewSessionStatistics> GetStatisticsAsync();

    /// <summary>
    /// Get popular collections
    /// </summary>
    Task<IEnumerable<PopularCollection>> GetPopularCollectionsAsync(int limit = 10);
}

/// <summary>
/// View session statistics
/// </summary>
public class ViewSessionStatistics
{
    public int TotalSessions { get; set; }
    public double TotalViewTime { get; set; }
    public double AverageViewTime { get; set; }
    public int UniqueCollections { get; set; }
    public int UniqueUsers { get; set; }
}

/// <summary>
/// Popular collection
/// </summary>
public class PopularCollection
{
    public Guid CollectionId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public double TotalViewTime { get; set; }
}
