using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for SearchHistory entity
/// </summary>
public interface ISearchHistoryRepository : IRepository<SearchHistory>
{
    /// <summary>
    /// Get search history by user ID
    /// </summary>
    Task<IEnumerable<SearchHistory>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get search history by search type
    /// </summary>
    Task<IEnumerable<SearchHistory>> GetBySearchTypeAsync(string searchType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get search history by query text
    /// </summary>
    Task<IEnumerable<SearchHistory>> GetByQueryAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get search history by date range
    /// </summary>
    Task<IEnumerable<SearchHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get popular search queries
    /// </summary>
    Task<IEnumerable<SearchHistory>> GetPopularQueriesAsync(int limit = 10, CancellationToken cancellationToken = default);
}
