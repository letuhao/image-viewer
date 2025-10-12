using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Service for managing Redis-based sorted collection index for fast navigation and sibling queries.
/// Index structure:
/// - Sorted Sets: One per sort field (updatedAt, createdAt, name, imageCount, totalSize) x 2 directions (asc/desc)
/// - Hash: Collection summary data (id, name, firstImageId, firstImageThumbnailUrl, imageCount, etc.)
/// </summary>
public interface ICollectionIndexService
{
    /// <summary>
    /// Rebuild entire collection index from database.
    /// Called on startup or manually via API.
    /// </summary>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update a single collection in the index.
    /// Called when collection is created or updated.
    /// </summary>
    Task AddOrUpdateCollectionAsync(Collection collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a collection from the index.
    /// Called when collection is deleted.
    /// </summary>
    Task RemoveCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collection navigation (previous/next) with accurate position.
    /// Uses Redis sorted set ZRANK for O(1) position lookup.
    /// </summary>
    Task<CollectionNavigationResult> GetNavigationAsync(
        ObjectId collectionId, 
        string sortBy = "updatedAt", 
        string sortDirection = "desc", 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collection siblings with pagination.
    /// Uses Redis sorted set ZRANGE for O(log(N)+M) sibling lookup.
    /// </summary>
    Task<CollectionSiblingsResult> GetSiblingsAsync(
        ObjectId collectionId, 
        int page = 1, 
        int pageSize = 20, 
        string sortBy = "updatedAt", 
        string sortDirection = "desc", 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if index exists and is up-to-date.
    /// </summary>
    Task<bool> IsIndexValidAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get index statistics (total collections, last rebuild time, etc.)
    /// </summary>
    Task<CollectionIndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated collections (for collection list page).
    /// Uses Redis sorted set ZRANGE for fast pagination.
    /// </summary>
    Task<CollectionPageResult> GetCollectionPageAsync(
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collections by library with pagination and sorting.
    /// Uses secondary index: collection_index:by_library:{libraryId}:sorted:{field}:{direction}
    /// </summary>
    Task<CollectionPageResult> GetCollectionsByLibraryAsync(
        ObjectId libraryId,
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collections by type with pagination and sorting.
    /// Uses secondary index: collection_index:by_type:{type}:sorted:{field}:{direction}
    /// </summary>
    Task<CollectionPageResult> GetCollectionsByTypeAsync(
        int collectionType,
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total collections count (O(1) operation).
    /// Uses Redis ZCARD for instant count.
    /// </summary>
    Task<int> GetTotalCollectionsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collections count by library (O(1) operation).
    /// </summary>
    Task<int> GetCollectionsCountByLibraryAsync(ObjectId libraryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collections count by type (O(1) operation).
    /// </summary>
    Task<int> GetCollectionsCountByTypeAsync(int collectionType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of navigation query
/// </summary>
public class CollectionNavigationResult
{
    public string? PreviousCollectionId { get; set; }
    public string? NextCollectionId { get; set; }
    public int CurrentPosition { get; set; }
    public int TotalCollections { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

/// <summary>
/// Result of siblings query with collection summaries
/// </summary>
public class CollectionSiblingsResult
{
    public List<CollectionSummary> Siblings { get; set; } = new();
    public int CurrentPosition { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// Result of paginated collection query
/// </summary>
public class CollectionPageResult
{
    public List<CollectionSummary> Collections { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

/// <summary>
/// Lightweight collection summary stored in Redis hash
/// </summary>
public class CollectionSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FirstImageId { get; set; }
    public string? FirstImageThumbnailUrl { get; set; }
    public int ImageCount { get; set; }
    public int ThumbnailCount { get; set; }
    public int CacheCount { get; set; }
    public long TotalSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Additional fields for filtering and display
    public string LibraryId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Type { get; set; } // CollectionType enum as int
    public List<string> Tags { get; set; } = new();
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Index statistics
/// </summary>
public class CollectionIndexStats
{
    public int TotalCollections { get; set; }
    public DateTime? LastRebuildTime { get; set; }
    public bool IsValid { get; set; }
    public Dictionary<string, int> SortedSetSizes { get; set; } = new();
}

