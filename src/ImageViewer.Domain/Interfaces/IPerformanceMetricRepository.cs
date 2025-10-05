using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for PerformanceMetric entity
/// </summary>
public interface IPerformanceMetricRepository : IRepository<PerformanceMetric>
{
    /// <summary>
    /// Get performance metrics by metric type
    /// </summary>
    Task<IEnumerable<PerformanceMetric>> GetByMetricTypeAsync(string metricType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get performance metrics by operation
    /// </summary>
    Task<IEnumerable<PerformanceMetric>> GetByOperationAsync(string operation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get performance metrics by user ID
    /// </summary>
    Task<IEnumerable<PerformanceMetric>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get performance metrics by date range
    /// </summary>
    Task<IEnumerable<PerformanceMetric>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get performance metrics by collection ID
    /// </summary>
    Task<IEnumerable<PerformanceMetric>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get performance metrics by library ID
    /// </summary>
    Task<IEnumerable<PerformanceMetric>> GetByLibraryIdAsync(ObjectId libraryId, CancellationToken cancellationToken = default);
}
