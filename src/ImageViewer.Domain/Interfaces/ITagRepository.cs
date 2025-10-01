using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Tag repository interface
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    /// <summary>
    /// Get tag by name
    /// </summary>
    Task<Tag?> GetByNameAsync(string name);

    /// <summary>
    /// Search tags by name
    /// </summary>
    Task<IEnumerable<Tag>> SearchByNameAsync(string query);

    /// <summary>
    /// Get popular tags
    /// </summary>
    Task<IEnumerable<Tag>> GetPopularTagsAsync(int limit = 20);

    /// <summary>
    /// Get tags by collection ID
    /// </summary>
    Task<IEnumerable<Tag>> GetByCollectionIdAsync(Guid collectionId);

    /// <summary>
    /// Get tag usage count
    /// </summary>
    Task<int> GetUsageCountAsync(Guid tagId);
}
