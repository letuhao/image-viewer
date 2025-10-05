using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for Session entity
/// </summary>
public interface ISessionRepository : IRepository<Session>
{
    Task<Session?> GetBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<IEnumerable<Session>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Session>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task TerminateAllUserSessionsAsync(ObjectId userId, ObjectId? terminatedBy = null, CancellationToken cancellationToken = default);
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
