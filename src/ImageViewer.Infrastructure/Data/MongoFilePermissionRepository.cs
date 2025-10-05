using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoFilePermissionRepository : MongoRepository<FilePermission>, IFilePermissionRepository
{
    public MongoFilePermissionRepository(IMongoDatabase database, ILogger<MongoFilePermissionRepository> logger)
        : base(database.GetCollection<FilePermission>("filePermissions"), logger)
    {
    }

    public async Task<IEnumerable<FilePermission>> GetByFileIdAsync(ObjectId fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(permission => permission.FileId == fileId)
                .SortByDescending(permission => permission.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get file permissions for file {FileId}", fileId);
            throw;
        }
    }

    public async Task<IEnumerable<FilePermission>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(permission => permission.OwnerId == userId)
                .SortByDescending(permission => permission.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get file permissions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<FilePermission>> GetByPermissionTypeAsync(string permissionType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(permission => permission.PermissionType == permissionType)
                .SortByDescending(permission => permission.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get file permissions for type {PermissionType}", permissionType);
            throw;
        }
    }

    public async Task<IEnumerable<FilePermission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _collection.Find(permission => 
                permission.IsActive == true && 
                (permission.ExpiresAt == null || permission.ExpiresAt > now))
                .SortByDescending(permission => permission.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active file permissions");
            throw;
        }
    }

    public async Task<IEnumerable<FilePermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _collection.Find(permission => 
                permission.ExpiresAt != null && 
                permission.ExpiresAt <= now)
                .SortByDescending(permission => permission.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get expired file permissions");
            throw;
        }
    }
}
