using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for NodePerformanceMetrics entity
/// </summary>
public interface INodePerformanceMetricsRepository : IRepository<NodePerformanceMetrics>
{
    Task<IEnumerable<NodePerformanceMetrics>> GetByNodeIdAsync(ObjectId nodeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NodePerformanceMetrics>> GetByMetricTypeAsync(string metricType, CancellationToken cancellationToken = default);
    Task<IEnumerable<NodePerformanceMetrics>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<NodePerformanceMetrics>> GetLatestMetricsAsync(ObjectId nodeId, int limit = 10, CancellationToken cancellationToken = default);
}
