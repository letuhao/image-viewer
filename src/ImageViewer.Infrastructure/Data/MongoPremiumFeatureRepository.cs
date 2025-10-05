using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoPremiumFeatureRepository : MongoRepository<PremiumFeature>, IPremiumFeatureRepository
{
    public MongoPremiumFeatureRepository(IMongoDatabase database, ILogger<MongoPremiumFeatureRepository> logger)
        : base(database.GetCollection<PremiumFeature>("premiumFeatures"), logger)
    {
    }

    public async Task<IEnumerable<PremiumFeature>> GetByTypeAsync(string featureType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(feature => feature.Type == featureType)
                .SortBy(feature => feature.Name)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get premium features for type {FeatureType}", featureType);
            throw;
        }
    }

    public async Task<IEnumerable<PremiumFeature>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(feature => feature.IsActive == true)
                .SortBy(feature => feature.Name)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active premium features");
            throw;
        }
    }

    public async Task<IEnumerable<PremiumFeature>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(feature => feature.Category == category)
                .SortBy(feature => feature.Name)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get premium features for category {Category}", category);
            throw;
        }
    }

    public async Task<PremiumFeature?> GetByFeatureNameAsync(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(feature => feature.Name == featureName).FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get premium feature for name {FeatureName}", featureName);
            throw;
        }
    }

    public async Task<IEnumerable<PremiumFeature>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<PremiumFeature>.Filter.And(
                Builders<PremiumFeature>.Filter.Gte(feature => feature.Price, minPrice),
                Builders<PremiumFeature>.Filter.Lte(feature => feature.Price, maxPrice)
            );

            return await _collection.Find(filter)
                .SortBy(feature => feature.Price)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get premium features for price range {MinPrice} to {MaxPrice}", minPrice, maxPrice);
            throw;
        }
    }
}
