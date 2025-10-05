using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoUserGroupRepository : MongoRepository<UserGroup>, IUserGroupRepository
{
    public MongoUserGroupRepository(IMongoDatabase database, ILogger<MongoUserGroupRepository> logger)
        : base(database.GetCollection<UserGroup>("userGroups"), logger)
    {
    }

    public async Task<IEnumerable<UserGroup>> GetByOwnerIdAsync(ObjectId ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(group => group.CreatedBy == ownerId)
                .SortByDescending(group => group.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user groups for owner {OwnerId}", ownerId);
            throw;
        }
    }

    public async Task<IEnumerable<UserGroup>> GetByMemberIdAsync(ObjectId memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(group => group.MemberCount > 0)
                .SortByDescending(group => group.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user groups for member {MemberId}", memberId);
            throw;
        }
    }

    public async Task<IEnumerable<UserGroup>> GetByTypeAsync(string groupType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(group => group.Type == groupType)
                .SortByDescending(group => group.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get user groups for type {GroupType}", groupType);
            throw;
        }
    }

    public async Task<IEnumerable<UserGroup>> GetPublicGroupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(group => group.IsActive == true)
                .SortByDescending(group => group.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get public user groups");
            throw;
        }
    }

    public async Task<bool> IsUserMemberAsync(ObjectId groupId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _collection.Find(g => g.Id == groupId).FirstOrDefaultAsync(cancellationToken);
            return group?.MemberCount > 0;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is member of group {GroupId}", userId, groupId);
            throw;
        }
    }
}
