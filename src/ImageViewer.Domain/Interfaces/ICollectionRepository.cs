using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Collection repository interface
/// </summary>
public interface ICollectionRepository : IRepository<Collection>
{
    Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Collection?> GetByPathAsync(string path, CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetByTypeAsync(Domain.Enums.CollectionType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetActiveCollectionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetActiveCollectionsQueryableAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetCollectionsWithImagesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName, CancellationToken cancellationToken = default);
    Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default);
}
