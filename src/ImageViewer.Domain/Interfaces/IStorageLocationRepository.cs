using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for StorageLocation entity
/// </summary>
public interface IStorageLocationRepository : IRepository<StorageLocation>
{
    Task<IEnumerable<StorageLocation>> GetByTypeAsync(string storageType, CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageLocation>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageLocation>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<StorageLocation?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
