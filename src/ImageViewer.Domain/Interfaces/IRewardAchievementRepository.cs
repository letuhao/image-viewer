using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for RewardAchievement entity
/// </summary>
public interface IRewardAchievementRepository : IRepository<RewardAchievement>
{
    Task<IEnumerable<RewardAchievement>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardAchievement>> GetByTypeAsync(string achievementType, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardAchievement>> GetByTierAsync(string tier, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardAchievement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<RewardAchievement>> GetTopAchievementsAsync(int limit = 10, CancellationToken cancellationToken = default);
}
