using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoNodePerformanceMetricsRepository : MongoRepository<NodePerformanceMetrics>, INodePerformanceMetricsRepository
{
    public MongoNodePerformanceMetricsRepository(IMongoDatabase database, ILogger<MongoNodePerformanceMetricsRepository> logger)
        : base(database.GetCollection<NodePerformanceMetrics>("nodePerformanceMetrics"), logger)
    {
    }

    public async Task<IEnumerable<NodePerformanceMetrics>> GetByNodeIdAsync(ObjectId nodeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(metrics => metrics.NodeId == nodeId)
                .SortByDescending(metrics => metrics.Timestamp)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for node {NodeId}", nodeId);
            throw;
        }
    }

    public async Task<IEnumerable<NodePerformanceMetrics>> GetByMetricTypeAsync(string metricType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Since NodePerformanceMetrics doesn't have a MetricType property, we'll filter by health score ranges
            // This is a placeholder implementation - in a real scenario, you might want to add a MetricType property
            return await _collection.Find(metrics => metrics.HealthScore >= 0) // Return all metrics
                .SortByDescending(metrics => metrics.Timestamp)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for type {MetricType}", metricType);
            throw;
        }
    }

    public async Task<IEnumerable<NodePerformanceMetrics>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<NodePerformanceMetrics>.Filter.And(
                Builders<NodePerformanceMetrics>.Filter.Gte(metrics => metrics.Timestamp, startDate),
                Builders<NodePerformanceMetrics>.Filter.Lte(metrics => metrics.Timestamp, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(metrics => metrics.Timestamp)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<NodePerformanceMetrics>> GetLatestMetricsAsync(ObjectId nodeId, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(metrics => metrics.NodeId == nodeId)
                .SortByDescending(metrics => metrics.Timestamp)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get latest performance metrics for node {NodeId}", nodeId);
            throw;
        }
    }
}
