using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for UserActivityLog entity
/// </summary>
public interface IUserActivityLogRepository : IRepository<UserActivityLog>
{
    Task<IEnumerable<UserActivityLog>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserActivityLog>> GetByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserActivityLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserActivityLog>> GetRecentActivityAsync(ObjectId userId, int limit = 10, CancellationToken cancellationToken = default);
}