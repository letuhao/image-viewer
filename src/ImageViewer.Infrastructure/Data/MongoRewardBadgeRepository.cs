using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoRewardBadgeRepository : MongoRepository<RewardBadge>, IRewardBadgeRepository
{
    public MongoRewardBadgeRepository(IMongoDatabase database, ILogger<MongoRewardBadgeRepository> logger)
        : base(database.GetCollection<RewardBadge>("rewardBadges"), logger)
    {
    }

    public async Task<IEnumerable<RewardBadge>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(badge => badge.CreatedBy == userId)
                .SortByDescending(badge => badge.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward badges for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<RewardBadge>> GetByTypeAsync(string badgeType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(badge => badge.Type == badgeType)
                .SortByDescending(badge => badge.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward badges for type {BadgeType}", badgeType);
            throw;
        }
    }

    public async Task<IEnumerable<RewardBadge>> GetByRarityAsync(string rarity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(badge => badge.Rarity == rarity)
                .SortByDescending(badge => badge.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward badges for rarity {Rarity}", rarity);
            throw;
        }
    }

    public async Task<IEnumerable<RewardBadge>> GetPublicBadgesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(badge => badge.IsShowcase == true)
                .SortByDescending(badge => badge.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get public reward badges");
            throw;
        }
    }

    public async Task<IEnumerable<RewardBadge>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<RewardBadge>.Filter.And(
                Builders<RewardBadge>.Filter.Gte(badge => badge.CreatedAt, startDate),
                Builders<RewardBadge>.Filter.Lte(badge => badge.CreatedAt, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(badge => badge.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get reward badges for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }
}
