using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of RefreshToken repository
/// </summary>
public class MongoRefreshTokenRepository : MongoRepository<RefreshToken>, IRefreshTokenRepository
{
    public MongoRefreshTokenRepository(MongoDbContext context, ILogger<MongoRefreshTokenRepository> logger) 
        : base(context.RefreshTokens, logger)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.Token, token);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.UserId, userId);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefreshToken>.Filter.And(
            Builders<RefreshToken>.Filter.Eq(t => t.UserId, userId),
            Builders<RefreshToken>.Filter.Eq(t => t.IsRevoked, false),
            Builders<RefreshToken>.Filter.Gt(t => t.ExpiresAt, DateTime.UtcNow)
        );
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(ObjectId userId, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefreshToken>.Filter.And(
            Builders<RefreshToken>.Filter.Eq(t => t.UserId, userId),
            Builders<RefreshToken>.Filter.Eq(t => t.IsRevoked, false)
        );

        var update = Builders<RefreshToken>.Update
            .Set(t => t.IsRevoked, true)
            .Set(t => t.RevokedAt, DateTime.UtcNow)
            .Set(t => t.RevokedByIp, revokedByIp)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefreshToken>.Filter.Lt(t => t.ExpiresAt, DateTime.UtcNow);
        await _collection.DeleteManyAsync(filter, cancellationToken);
    }
}
