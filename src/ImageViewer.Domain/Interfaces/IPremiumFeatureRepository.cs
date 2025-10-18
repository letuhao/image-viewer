using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for PremiumFeature entity
/// </summary>
public interface IPremiumFeatureRepository : IRepository<PremiumFeature>
{
    Task<IEnumerable<PremiumFeature>> GetByTypeAsync(string featureType, CancellationToken cancellationToken = default);
    Task<IEnumerable<PremiumFeature>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PremiumFeature>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<PremiumFeature?> GetByFeatureNameAsync(string featureName, CancellationToken cancellationToken = default);
    Task<IEnumerable<PremiumFeature>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default);
}
