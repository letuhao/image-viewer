using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for RewardBadge entity
/// </summary>
public interface IRewardBadgeRepository : IRepository<RewardBadge>
{
    Task<IEnumerable<RewardBadge>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardBadge>> GetByTypeAsync(string badgeType, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardBadge>> GetByRarityAsync(string rarity, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardBadge>> GetPublicBadgesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardBadge>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
