using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoRewardAchievementRepository : MongoRepository<RewardAchievement>, IRewardAchievementRepository
{
    public MongoRewardAchievementRepository(IMongoDatabase database, ILogger<MongoRewardAchievementRepository> logger)
        : base(database.GetCollection<RewardAchievement>("rewardAchievements"), logger)
    {
    }

    public async Task<IEnumerable<RewardAchievement>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(achievement => achievement.CreatedBy == userId)
                .SortByDescending(achievement => achievement.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward achievements for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<RewardAchievement>> GetByTypeAsync(string achievementType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(achievement => achievement.Type == achievementType)
                .SortByDescending(achievement => achievement.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward achievements for type {AchievementType}", achievementType);
            throw;
        }
    }

    public async Task<IEnumerable<RewardAchievement>> GetByTierAsync(string tier, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(achievement => achievement.Tier.ToString() == tier)
                .SortByDescending(achievement => achievement.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward achievements for tier {Tier}", tier);
            throw;
        }
    }

    public async Task<IEnumerable<RewardAchievement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<RewardAchievement>.Filter.And(
                Builders<RewardAchievement>.Filter.Gte(achievement => achievement.CreatedAt, startDate),
                Builders<RewardAchievement>.Filter.Lte(achievement => achievement.CreatedAt, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(achievement => achievement.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward achievements for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<RewardAchievement>> GetTopAchievementsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(achievement => achievement.Points)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get top reward achievements");
            throw;
        }
    }
}
