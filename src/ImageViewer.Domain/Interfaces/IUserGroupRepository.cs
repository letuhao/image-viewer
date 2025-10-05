using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for UserGroup entity
/// </summary>
public interface IUserGroupRepository : IRepository<UserGroup>
{
    Task<IEnumerable<UserGroup>> GetByOwnerIdAsync(ObjectId ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGroup>> GetByMemberIdAsync(ObjectId memberId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGroup>> GetByTypeAsync(string groupType, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGroup>> GetPublicGroupsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(ObjectId groupId, ObjectId userId, CancellationToken cancellationToken = default);
}
