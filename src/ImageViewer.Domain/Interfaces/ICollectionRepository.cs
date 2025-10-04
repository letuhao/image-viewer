using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for Collection operations
/// </summary>
public interface ICollectionRepository : IRepository<Collection>
{
    #region Query Methods
    
    Task<Collection> GetByPathAsync(string path);
    Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId);
    Task<IEnumerable<Collection>> GetActiveCollectionsAsync();
    Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type);
    Task<long> GetCollectionCountAsync();
    Task<long> GetActiveCollectionCountAsync();
    
    #endregion
    
    #region Search Methods
    
    Task<IEnumerable<Collection>> SearchCollectionsAsync(string query);
    Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilter filter);
    
    #endregion
    
    #region Statistics Methods
    
    Task<CollectionStatistics> GetCollectionStatisticsAsync();
    Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit = 10);
    Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit = 10);
    
    #endregion
}

/// <summary>
/// Collection filter for advanced queries
/// </summary>
public class CollectionFilter
{
    public ObjectId? LibraryId { get; set; }
    public CollectionType? Type { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastActivityAfter { get; set; }
    public DateTime? LastActivityBefore { get; set; }
    public string? Path { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
}

/// <summary>
/// Collection statistics for reporting
/// </summary>
public class CollectionStatistics
{
    public long TotalCollections { get; set; }
    public long ActiveCollections { get; set; }
    public long NewCollectionsThisMonth { get; set; }
    public long NewCollectionsThisWeek { get; set; }
    public long NewCollectionsToday { get; set; }
    public Dictionary<ObjectId, long> CollectionsByLibrary { get; set; } = new();
    public Dictionary<CollectionType, long> CollectionsByType { get; set; } = new();
    public Dictionary<string, long> CollectionsByTag { get; set; } = new();
    public Dictionary<string, long> CollectionsByCategory { get; set; } = new();
}