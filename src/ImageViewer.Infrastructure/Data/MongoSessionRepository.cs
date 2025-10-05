using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of Session repository
/// </summary>
public class MongoSessionRepository : MongoRepository<Session>, ISessionRepository
{
    public MongoSessionRepository(MongoDbContext context, ILogger<MongoSessionRepository> logger) 
        : base(context.Sessions, logger)
    {
    }

    public async Task<Session?> GetBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Session>.Filter.Eq(s => s.SessionToken, sessionToken);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Session>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Session>.Filter.Eq(s => s.UserId, userId);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Session>.Filter.And(
            Builders<Session>.Filter.Eq(s => s.UserId, userId),
            Builders<Session>.Filter.Eq(s => s.IsActive, true),
            Builders<Session>.Filter.Gt(s => s.ExpiresAt, DateTime.UtcNow)
        );
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Session>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Session>.Filter.Lt(s => s.ExpiresAt, DateTime.UtcNow);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task TerminateAllUserSessionsAsync(ObjectId userId, ObjectId? terminatedBy = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Session>.Filter.And(
            Builders<Session>.Filter.Eq(s => s.UserId, userId),
            Builders<Session>.Filter.Eq(s => s.IsActive, true)
        );

        var update = Builders<Session>.Update
            .Set(s => s.IsActive, false)
            .Set(s => s.TerminatedAt, DateTime.UtcNow)
            .Set(s => s.TerminatedBy, terminatedBy)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Session>.Filter.Lt(s => s.ExpiresAt, DateTime.UtcNow);
        await _collection.DeleteManyAsync(filter, cancellationToken);
    }
}
