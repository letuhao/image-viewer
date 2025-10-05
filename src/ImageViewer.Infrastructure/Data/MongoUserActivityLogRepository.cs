using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoUserActivityLogRepository : MongoRepository<UserActivityLog>, IUserActivityLogRepository
{
    public MongoUserActivityLogRepository(IMongoDatabase database, ILogger<MongoUserActivityLogRepository> logger)
        : base(database.GetCollection<UserActivityLog>("userActivityLogs"), logger)
    {
    }

    public async Task<IEnumerable<UserActivityLog>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(activity => activity.UserId == userId)
                .SortByDescending(activity => activity.ActivityDate)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user activity logs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserActivityLog>> GetByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(activity => activity.ActivityType == activityType)
                .SortByDescending(activity => activity.ActivityDate)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user activity logs for type {ActivityType}", activityType);
            throw;
        }
    }

    public async Task<IEnumerable<UserActivityLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<UserActivityLog>.Filter.And(
                Builders<UserActivityLog>.Filter.Gte(activity => activity.ActivityDate, startDate),
                Builders<UserActivityLog>.Filter.Lte(activity => activity.ActivityDate, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(activity => activity.ActivityDate)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user activity logs for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<UserActivityLog>> GetRecentActivityAsync(ObjectId userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(activity => activity.UserId == userId)
                .SortByDescending(activity => activity.ActivityDate)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get recent activity for user {UserId}", userId);
            throw;
        }
    }
}
