using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for CollectionArchive operations
/// </summary>
public interface ICollectionArchiveRepository
{
    /// <summary>
    /// Create a new archived collection
    /// </summary>
    Task<CollectionArchive> CreateAsync(CollectionArchive collectionArchive);
    
    /// <summary>
    /// Get archived collection by ID
    /// </summary>
    Task<CollectionArchive?> GetByIdAsync(ObjectId archiveId);
    
    /// <summary>
    /// Get archived collection by original collection ID
    /// </summary>
    Task<CollectionArchive?> GetByOriginalIdAsync(ObjectId originalCollectionId);
    
    /// <summary>
    /// Get all archived collections with pagination
    /// </summary>
    Task<IEnumerable<CollectionArchive>> GetArchivedCollectionsAsync(int page = 1, int pageSize = 20, string sortBy = "archivedAt", string sortDirection = "desc");
    
    /// <summary>
    /// Get total count of archived collections
    /// </summary>
    Task<long> GetTotalArchivedCountAsync();
    
    /// <summary>
    /// Search archived collections by name or path
    /// </summary>
    Task<IEnumerable<CollectionArchive>> SearchArchivedCollectionsAsync(string query, int page = 1, int pageSize = 20);
    
    /// <summary>
    /// Get archived collections by archive reason
    /// </summary>
    Task<IEnumerable<CollectionArchive>> GetArchivedCollectionsByReasonAsync(string archiveReason, int page = 1, int pageSize = 20);
    
    /// <summary>
    /// Delete archived collection permanently
    /// </summary>
    Task DeleteAsync(ObjectId archiveId);
    
    /// <summary>
    /// Get archive statistics
    /// </summary>
    Task<ArchiveStatistics> GetArchiveStatisticsAsync();
}

/// <summary>
/// Archive statistics model
/// </summary>
public class ArchiveStatistics
{
    public long TotalArchivedCollections { get; set; }
    public long TotalArchivedImages { get; set; }
    public long TotalArchivedSize { get; set; }
    public Dictionary<string, long> CollectionsByReason { get; set; } = new();
    public Dictionary<string, long> CollectionsByType { get; set; } = new();
    public DateTime? OldestArchive { get; set; }
    public DateTime? NewestArchive { get; set; }
}
