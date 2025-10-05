using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for UserPremiumFeature entity
/// </summary>
public interface IUserPremiumFeatureRepository : IRepository<UserPremiumFeature>
{
    Task<IEnumerable<UserPremiumFeature>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPremiumFeature>> GetByFeatureIdAsync(ObjectId featureId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPremiumFeature>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPremiumFeature>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserPremiumFeature>> GetByBillingPeriodAsync(string billingPeriod, CancellationToken cancellationToken = default);
}
