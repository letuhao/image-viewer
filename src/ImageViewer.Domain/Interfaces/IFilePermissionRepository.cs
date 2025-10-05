using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for FilePermission entity
/// </summary>
public interface IFilePermissionRepository : IRepository<FilePermission>
{
    Task<IEnumerable<FilePermission>> GetByFileIdAsync(ObjectId fileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FilePermission>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FilePermission>> GetByPermissionTypeAsync(string permissionType, CancellationToken cancellationToken = default);
    Task<IEnumerable<FilePermission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FilePermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default);
}
