using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for PerformanceMetric entity
/// </summary>
public class MongoPerformanceMetricRepository : MongoRepository<PerformanceMetric>, IPerformanceMetricRepository
{
    public MongoPerformanceMetricRepository(IMongoDatabase database, ILogger<MongoPerformanceMetricRepository> logger) 
        : base(database.GetCollection<PerformanceMetric>("performanceMetrics"), logger)
    {
    }

    public async Task<IEnumerable<PerformanceMetric>> GetByMetricTypeAsync(string metricType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(metric => metric.MetricType == metricType)
                .SortByDescending(metric => metric.SampledAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for type {MetricType}", metricType);
            throw;
        }
    }

    public async Task<IEnumerable<PerformanceMetric>> GetByOperationAsync(string operation, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(metric => metric.Operation == operation)
                .SortByDescending(metric => metric.SampledAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for operation {Operation}", operation);
            throw;
        }
    }

    public async Task<IEnumerable<PerformanceMetric>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(metric => metric.UserId == userId)
                .SortByDescending(metric => metric.SampledAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<PerformanceMetric>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<PerformanceMetric>.Filter.And(
                Builders<PerformanceMetric>.Filter.Gte(metric => metric.SampledAt, startDate),
                Builders<PerformanceMetric>.Filter.Lte(metric => metric.SampledAt, endDate)
            );
            
            return await _collection.Find(filter)
                .SortByDescending(metric => metric.SampledAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<PerformanceMetric>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(metric => metric.Tags.ContainsKey("CollectionId") && metric.Tags["CollectionId"] == collectionId.ToString())
                .SortByDescending(metric => metric.SampledAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<PerformanceMetric>> GetByLibraryIdAsync(ObjectId libraryId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(metric => metric.Tags.ContainsKey("LibraryId") && metric.Tags["LibraryId"] == libraryId.ToString())
                .SortByDescending(metric => metric.SampledAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for library {LibraryId}", libraryId);
            throw;
        }
    }
}
