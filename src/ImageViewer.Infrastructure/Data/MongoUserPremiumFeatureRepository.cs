using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoUserPremiumFeatureRepository : MongoRepository<UserPremiumFeature>, IUserPremiumFeatureRepository
{
    public MongoUserPremiumFeatureRepository(IMongoDatabase database, ILogger<MongoUserPremiumFeatureRepository> logger)
        : base(database.GetCollection<UserPremiumFeature>("userPremiumFeatures"), logger)
    {
    }

    public async Task<IEnumerable<UserPremiumFeature>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(subscription => subscription.UserId == userId)
                .SortByDescending(subscription => subscription.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user premium features for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserPremiumFeature>> GetByFeatureIdAsync(ObjectId featureId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(subscription => subscription.PremiumFeatureId == featureId)
                .SortByDescending(subscription => subscription.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user premium features for feature {FeatureId}", featureId);
            throw;
        }
    }

    public async Task<IEnumerable<UserPremiumFeature>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _collection.Find(subscription => 
                subscription.IsActive() == true && 
                (subscription.EndDate == null || subscription.EndDate > now))
                .SortByDescending(subscription => subscription.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active premium subscriptions");
            throw;
        }
    }

    public async Task<IEnumerable<UserPremiumFeature>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _collection.Find(subscription => 
                subscription.EndDate != null && 
                subscription.EndDate <= now)
                .SortByDescending(subscription => subscription.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get expired premium subscriptions");
            throw;
        }
    }

    public async Task<IEnumerable<UserPremiumFeature>> GetByBillingPeriodAsync(string billingPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(subscription => subscription.BillingPeriod == billingPeriod)
                .SortByDescending(subscription => subscription.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get premium subscriptions for billing period {BillingPeriod}", billingPeriod);
            throw;
        }
    }
}
