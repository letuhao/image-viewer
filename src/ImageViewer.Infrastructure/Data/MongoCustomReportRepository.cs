using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoCustomReportRepository : MongoRepository<CustomReport>, ICustomReportRepository
{
    public MongoCustomReportRepository(IMongoDatabase database, ILogger<MongoCustomReportRepository> logger)
        : base(database.GetCollection<CustomReport>("customReports"), logger)
    {
    }

    public async Task<IEnumerable<CustomReport>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(report => report.CreatedBy == userId)
                .SortByDescending(report => report.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get custom reports for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<CustomReport>> GetByReportTypeAsync(string reportType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(report => report.ReportType == reportType)
                .SortByDescending(report => report.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get custom reports for type {ReportType}", reportType);
            throw;
        }
    }

    public async Task<IEnumerable<CustomReport>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(report => report.Status == status)
                .SortByDescending(report => report.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get custom reports for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<CustomReport>> GetPublicReportsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(report => report.IsPublic == true)
                .SortByDescending(report => report.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get public custom reports");
            throw;
        }
    }

    public async Task<IEnumerable<CustomReport>> GetScheduledReportsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(report => report.IsScheduled == true)
                .SortByDescending(report => report.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get scheduled custom reports");
            throw;
        }
    }
}
