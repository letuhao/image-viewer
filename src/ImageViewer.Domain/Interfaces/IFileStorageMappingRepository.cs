using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for FileStorageMapping entity
/// </summary>
public interface IFileStorageMappingRepository : IRepository<FileStorageMapping>
{
    Task<FileStorageMapping?> GetByFileIdAsync(ObjectId fileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileStorageMapping>> GetByStorageLocationIdAsync(ObjectId storageLocationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileStorageMapping>> GetByStorageTypeAsync(string storageType, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileStorageMapping>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
