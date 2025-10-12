using System.Text.Json;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using StackExchange.Redis;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Redis-based collection index for ultra-fast navigation and sibling queries.
/// Uses Redis sorted sets for O(log N) position lookup and O(log N + M) range queries.
/// </summary>
public class RedisCollectionIndexService : ICollectionIndexService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<RedisCollectionIndexService> _logger;

    // Redis key patterns
    private const string SORTED_SET_PREFIX = "collection_index:sorted:";
    private const string HASH_PREFIX = "collection_index:data:";
    private const string STATS_KEY = "collection_index:stats";
    private const string LAST_REBUILD_KEY = "collection_index:last_rebuild";
    private const string THUMBNAIL_PREFIX = "collection_index:thumb:";

    public RedisCollectionIndexService(
        IConnectionMultiplexer redis,
        ICollectionRepository collectionRepository,
        ILogger<RedisCollectionIndexService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _collectionRepository = collectionRepository;
        _logger = logger;
    }

    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔄 Starting collection index rebuild...");
            var startTime = DateTime.UtcNow;

            // Get all non-deleted collections
            var collections = await _collectionRepository.FindAsync(
                MongoDB.Driver.Builders<Collection>.Filter.Eq(c => c.IsDeleted, false),
                MongoDB.Driver.Builders<Collection>.Sort.Ascending(c => c.Id),
                int.MaxValue,
                0
            );

            var collectionList = collections.ToList();
            _logger.LogInformation("📊 Found {Count} collections to index", collectionList.Count);

            // Clear existing index
            await ClearIndexAsync();

            // Build sorted sets and hash entries
            var batch = _db.CreateBatch();
            var tasks = new List<Task>();

            foreach (var collection in collectionList)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("⚠️ Index rebuild cancelled");
                    return;
                }

                // Add to sorted sets (one per sort field/direction combination)
                tasks.Add(AddToSortedSetsAsync(batch, collection));

                // Add summary to hash
                tasks.Add(AddToHashAsync(batch, collection));
            }

            // Execute batch
            batch.Execute();
            await Task.WhenAll(tasks);

            // Update statistics
            await _db.StringSetAsync(LAST_REBUILD_KEY, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await _db.StringSetAsync(STATS_KEY + ":total", collectionList.Count);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Collection index rebuilt successfully. {Count} collections indexed in {Duration}ms", 
                collectionList.Count, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to rebuild collection index");
            throw;
        }
    }

    public async Task AddOrUpdateCollectionAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding/updating collection {CollectionId} in index", collection.Id);

            // Add to sorted sets
            await AddToSortedSetsAsync(_db, collection);

            // Add/update hash
            await AddToHashAsync(_db, collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update collection {CollectionId} in index", collection.Id);
            // Don't throw - index rebuild can fix this
        }
    }

    public async Task RemoveCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing collection {CollectionId} from index", collectionId);

            var collectionIdStr = collectionId.ToString();

            // Remove from all sorted sets
            var sortFields = new[] { "updatedAt", "createdAt", "name", "imageCount", "totalSize" };
            var sortDirections = new[] { "asc", "desc" };

            var tasks = new List<Task>();
            foreach (var field in sortFields)
            {
                foreach (var direction in sortDirections)
                {
                    var key = GetSortedSetKey(field, direction);
                    tasks.Add(_db.SortedSetRemoveAsync(key, collectionIdStr));
                }
            }

            // Remove from hash
            tasks.Add(_db.KeyDeleteAsync(GetHashKey(collectionIdStr)));

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove collection {CollectionId} from index", collectionId);
            // Don't throw - index rebuild can fix this
        }
    }

    public async Task<CollectionNavigationResult> GetNavigationAsync(
        ObjectId collectionId, 
        string sortBy = "updatedAt", 
        string sortDirection = "desc", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collectionIdStr = collectionId.ToString();
            var key = GetSortedSetKey(sortBy, sortDirection);

            // Get position using ZRANK (O(log N) - super fast!)
            var rank = await _db.SortedSetRankAsync(key, collectionIdStr, sortDirection == "desc" ? Order.Descending : Order.Ascending);
            
            if (!rank.HasValue)
            {
                _logger.LogWarning("Collection {CollectionId} not found in index", collectionId);
                // Fallback: try to get from database
                var collection = await _collectionRepository.GetByIdAsync(collectionId);
                if (collection != null && !collection.IsDeleted)
                {
                    await AddOrUpdateCollectionAsync(collection);
                    rank = await _db.SortedSetRankAsync(key, collectionIdStr, sortDirection == "desc" ? Order.Descending : Order.Ascending);
                }
            }

            var currentPosition = rank.HasValue ? (int)rank.Value + 1 : 0; // 1-based

            // Get total count
            var totalCount = await _db.SortedSetLengthAsync(key);

            // Get previous and next collection IDs
            string? previousId = null;
            string? nextId = null;

            if (rank.HasValue)
            {
                // Get previous (rank - 1)
                if (rank.Value > 0)
                {
                    var prevEntries = await _db.SortedSetRangeByRankAsync(key, rank.Value - 1, rank.Value - 1, sortDirection == "desc" ? Order.Descending : Order.Ascending);
                    previousId = prevEntries.FirstOrDefault().ToString();
                }

                // Get next (rank + 1)
                if (rank.Value < totalCount - 1)
                {
                    var nextEntries = await _db.SortedSetRangeByRankAsync(key, rank.Value + 1, rank.Value + 1, sortDirection == "desc" ? Order.Descending : Order.Ascending);
                    nextId = nextEntries.FirstOrDefault().ToString();
                }
            }

            return new CollectionNavigationResult
            {
                PreviousCollectionId = previousId,
                NextCollectionId = nextId,
                CurrentPosition = currentPosition,
                TotalCollections = (int)totalCount,
                HasPrevious = previousId != null,
                HasNext = nextId != null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get navigation for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<CollectionSiblingsResult> GetSiblingsAsync(
        ObjectId collectionId, 
        int page = 1, 
        int pageSize = 20, 
        string sortBy = "updatedAt", 
        string sortDirection = "desc", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collectionIdStr = collectionId.ToString();
            var key = GetSortedSetKey(sortBy, sortDirection);

            // Get current position
            var rank = await _db.SortedSetRankAsync(key, collectionIdStr, sortDirection == "desc" ? Order.Descending : Order.Ascending);
            
            if (!rank.HasValue)
            {
                _logger.LogWarning("Collection {CollectionId} not found in index for siblings", collectionId);
                return new CollectionSiblingsResult
                {
                    Siblings = new List<CollectionSummary>(),
                    CurrentPosition = 0,
                    TotalCount = 0
                };
            }

            var currentPosition = (int)rank.Value;
            var totalCount = await _db.SortedSetLengthAsync(key);

            // Calculate range for pagination around current position
            var startRank = (page - 1) * pageSize;
            var endRank = startRank + pageSize - 1;

            // Get collection IDs in range (O(log N + M))
            var collectionIds = await _db.SortedSetRangeByRankAsync(key, startRank, endRank, sortDirection == "desc" ? Order.Descending : Order.Ascending);

            // Get collection summaries from hash
            var siblings = new List<CollectionSummary>();
            foreach (var id in collectionIds)
            {
                var summary = await GetCollectionSummaryAsync(id.ToString());
                if (summary != null)
                {
                    // Note: Thumbnail URLs are loaded separately by the controller
                    siblings.Add(summary);
                }
            }

            return new CollectionSiblingsResult
            {
                Siblings = siblings,
                CurrentPosition = currentPosition + 1, // 1-based
                TotalCount = (int)totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get siblings for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<bool> IsIndexValidAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if at least one sorted set exists and has entries
            var key = GetSortedSetKey("updatedAt", "desc");
            var count = await _db.SortedSetLengthAsync(key);
            
            if (count == 0)
                return false;

            // Check if last rebuild time exists
            var lastRebuildExists = await _db.KeyExistsAsync(LAST_REBUILD_KEY);
            return lastRebuildExists;
        }
        catch
        {
            return false;
        }
    }

    public async Task<CollectionIndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = new CollectionIndexStats();

            // Get total count
            var totalStr = await _db.StringGetAsync(STATS_KEY + ":total");
            stats.TotalCollections = totalStr.HasValue ? (int)totalStr : 0;

            // Get last rebuild time
            var lastRebuildStr = await _db.StringGetAsync(LAST_REBUILD_KEY);
            if (lastRebuildStr.HasValue)
            {
                var unixTime = (long)lastRebuildStr;
                stats.LastRebuildTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
            }

            // Get sorted set sizes
            var sortFields = new[] { "updatedAt", "createdAt", "name", "imageCount", "totalSize" };
            var sortDirections = new[] { "asc", "desc" };

            foreach (var field in sortFields)
            {
                foreach (var direction in sortDirections)
                {
                    var key = GetSortedSetKey(field, direction);
                    var count = await _db.SortedSetLengthAsync(key);
                    stats.SortedSetSizes[$"{field}_{direction}"] = (int)count;
                }
            }

            stats.IsValid = await IsIndexValidAsync();

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get index stats");
            throw;
        }
    }

    #region Helper Methods

    private string GetSortedSetKey(string sortBy, string sortDirection)
    {
        return $"{SORTED_SET_PREFIX}{sortBy}:{sortDirection}";
    }

    private string GetHashKey(string collectionId)
    {
        return $"{HASH_PREFIX}{collectionId}";
    }

    private string GetSecondaryIndexKey(string indexType, string indexValue, string sortBy, string sortDirection)
    {
        return $"{SORTED_SET_PREFIX}{indexType}:{indexValue}:{sortBy}:{sortDirection}";
    }

    private string GetThumbnailKey(string collectionId)
    {
        return $"{THUMBNAIL_PREFIX}{collectionId}";
    }

    private async Task AddToSortedSetsAsync(IDatabaseAsync db, Collection collection)
    {
        var collectionIdStr = collection.Id.ToString();
        var tasks = new List<Task>();

        // Primary indexes - all sort field combinations
        var sortFields = new[] { "updatedAt", "createdAt", "name", "imageCount", "totalSize" };
        var sortDirections = new[] { "asc", "desc" };

        foreach (var field in sortFields)
        {
            foreach (var direction in sortDirections)
            {
                var score = GetScoreForField(collection, field, direction);
                tasks.Add(db.SortedSetAddAsync(GetSortedSetKey(field, direction), collectionIdStr, score));
            }
        }

        // Secondary indexes - by library
        var libraryId = collection.LibraryId.ToString();
        foreach (var field in sortFields)
        {
            foreach (var direction in sortDirections)
            {
                var score = GetScoreForField(collection, field, direction);
                var key = GetSecondaryIndexKey("by_library", libraryId, field, direction);
                tasks.Add(db.SortedSetAddAsync(key, collectionIdStr, score));
            }
        }

        // Secondary indexes - by type
        var type = ((int)collection.Type).ToString();
        foreach (var field in sortFields)
        {
            foreach (var direction in sortDirections)
            {
                var score = GetScoreForField(collection, field, direction);
                var key = GetSecondaryIndexKey("by_type", type, field, direction);
                tasks.Add(db.SortedSetAddAsync(key, collectionIdStr, score));
            }
        }

        await Task.WhenAll(tasks);
    }

    private double GetScoreForField(Collection collection, string field, string direction)
    {
        var multiplier = direction == "desc" ? -1 : 1;

        return field.ToLower() switch
        {
            "updatedat" => collection.UpdatedAt.Ticks * multiplier,
            "createdat" => collection.CreatedAt.Ticks * multiplier,
            "name" => (collection.Name?.GetHashCode() ?? 0) * multiplier,
            "imagecount" => collection.Statistics.TotalItems * multiplier,
            "totalsize" => collection.Statistics.TotalSize * multiplier,
            _ => collection.UpdatedAt.Ticks * multiplier
        };
    }

    private async Task AddToHashAsync(IDatabaseAsync db, Collection collection)
    {
        var summary = new CollectionSummary
        {
            Id = collection.Id.ToString(),
            Name = collection.Name ?? "",
            FirstImageId = collection.Images?.FirstOrDefault()?.Id.ToString(),
            ImageCount = collection.Images?.Count ?? 0,
            ThumbnailCount = collection.Thumbnails?.Count ?? 0,
            CacheCount = collection.CacheImages?.Count ?? 0,
            TotalSize = collection.Statistics.TotalSize,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            
            // New fields for filtering and display
            LibraryId = collection.LibraryId.ToString(),
            Description = collection.Description,
            Type = (int)collection.Type,
            Tags = collection.Tags?.ToList() ?? new List<string>(),
            Path = collection.Path ?? ""
        };

        var json = JsonSerializer.Serialize(summary);
        await db.StringSetAsync(GetHashKey(collection.Id.ToString()), json);
    }

    private async Task<CollectionSummary?> GetCollectionSummaryAsync(string collectionId)
    {
        var json = await _db.StringGetAsync(GetHashKey(collectionId));
        if (!json.HasValue)
            return null;

        return JsonSerializer.Deserialize<CollectionSummary>(json.ToString());
    }

    private async Task ClearIndexAsync()
    {
        _logger.LogDebug("Clearing existing index...");

        var sortFields = new[] { "updatedAt", "createdAt", "name", "imageCount", "totalSize" };
        var sortDirections = new[] { "asc", "desc" };

        var tasks = new List<Task>();
        foreach (var field in sortFields)
        {
            foreach (var direction in sortDirections)
            {
                var key = GetSortedSetKey(field, direction);
                tasks.Add(_db.KeyDeleteAsync(key));
            }
        }

        await Task.WhenAll(tasks);
    }

    #endregion

    #region New Methods for Collection Pagination and Filtering

    public async Task<CollectionPageResult> GetCollectionPageAsync(
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSortedSetKey(sortBy, sortDirection);
            var startRank = (page - 1) * pageSize;
            var endRank = startRank + pageSize - 1;

            // Get collection IDs for this page
            var collectionIds = await _db.SortedSetRangeByRankAsync(
                key, 
                startRank, 
                endRank, 
                sortDirection == "desc" ? Order.Descending : Order.Ascending);

            // Get collection summaries
            var collections = new List<CollectionSummary>();
            foreach (var id in collectionIds)
            {
                var summary = await GetCollectionSummaryAsync(id.ToString());
                if (summary != null)
                {
                    // Note: Thumbnail URLs are loaded separately by the controller
                    // to avoid tight coupling with thumbnail service
                    collections.Add(summary);
                }
            }

            // Get total count
            var totalCount = await _db.SortedSetLengthAsync(key);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new CollectionPageResult
            {
                Collections = collections,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = (int)totalCount,
                TotalPages = totalPages,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection page {Page} with size {PageSize}", page, pageSize);
            throw;
        }
    }

    public async Task<CollectionPageResult> GetCollectionsByLibraryAsync(
        ObjectId libraryId,
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSecondaryIndexKey("by_library", libraryId.ToString(), sortBy, sortDirection);
            var startRank = (page - 1) * pageSize;
            var endRank = startRank + pageSize - 1;

            // Get collection IDs for this page
            var collectionIds = await _db.SortedSetRangeByRankAsync(
                key,
                startRank,
                endRank,
                sortDirection == "desc" ? Order.Descending : Order.Ascending);

            // Get collection summaries
            var collections = new List<CollectionSummary>();
            foreach (var id in collectionIds)
            {
                var summary = await GetCollectionSummaryAsync(id.ToString());
                if (summary != null)
                {
                    collections.Add(summary);
                }
            }

            // Get total count
            var totalCount = await _db.SortedSetLengthAsync(key);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new CollectionPageResult
            {
                Collections = collections,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = (int)totalCount,
                TotalPages = totalPages,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections by library {LibraryId}", libraryId);
            throw;
        }
    }

    public async Task<CollectionPageResult> GetCollectionsByTypeAsync(
        int collectionType,
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSecondaryIndexKey("by_type", collectionType.ToString(), sortBy, sortDirection);
            var startRank = (page - 1) * pageSize;
            var endRank = startRank + pageSize - 1;

            // Get collection IDs for this page
            var collectionIds = await _db.SortedSetRangeByRankAsync(
                key,
                startRank,
                endRank,
                sortDirection == "desc" ? Order.Descending : Order.Ascending);

            // Get collection summaries
            var collections = new List<CollectionSummary>();
            foreach (var id in collectionIds)
            {
                var summary = await GetCollectionSummaryAsync(id.ToString());
                if (summary != null)
                {
                    collections.Add(summary);
                }
            }

            // Get total count
            var totalCount = await _db.SortedSetLengthAsync(key);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new CollectionPageResult
            {
                Collections = collections,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = (int)totalCount,
                TotalPages = totalPages,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections by type {Type}", collectionType);
            throw;
        }
    }

    public async Task<int> GetTotalCollectionsCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSortedSetKey("updatedAt", "desc");
            var count = await _db.SortedSetLengthAsync(key);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total collections count");
            throw;
        }
    }

    public async Task<int> GetCollectionsCountByLibraryAsync(ObjectId libraryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSecondaryIndexKey("by_library", libraryId.ToString(), "updatedAt", "desc");
            var count = await _db.SortedSetLengthAsync(key);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections count for library {LibraryId}", libraryId);
            throw;
        }
    }

    public async Task<int> GetCollectionsCountByTypeAsync(int collectionType, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSecondaryIndexKey("by_type", collectionType.ToString(), "updatedAt", "desc");
            var count = await _db.SortedSetLengthAsync(key);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections count for type {Type}", collectionType);
            throw;
        }
    }

    #endregion

    #region Thumbnail Caching

    public async Task<byte[]?> GetCachedThumbnailAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetThumbnailKey(collectionId.ToString());
            var data = await _db.StringGetAsync(key);
            
            if (data.HasValue)
            {
                _logger.LogDebug("✅ Thumbnail cache HIT for collection {CollectionId}", collectionId);
                return (byte[])data;
            }
            
            _logger.LogDebug("❌ Thumbnail cache MISS for collection {CollectionId}", collectionId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cached thumbnail for collection {CollectionId}", collectionId);
            return null; // Fail gracefully
        }
    }

    public async Task SetCachedThumbnailAsync(ObjectId collectionId, byte[] thumbnailData, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetThumbnailKey(collectionId.ToString());
            var expire = expiration ?? TimeSpan.FromDays(30); // 30 days default
            
            await _db.StringSetAsync(key, thumbnailData, expire);
            _logger.LogDebug("💾 Cached thumbnail for collection {CollectionId}, size: {Size} bytes, expiration: {Expiration}", 
                collectionId, thumbnailData.Length, expire);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache thumbnail for collection {CollectionId}", collectionId);
            // Fail gracefully - don't throw
        }
    }

    public async Task BatchCacheThumbnailsAsync(Dictionary<ObjectId, byte[]> thumbnails, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📦 Batch caching {Count} thumbnails...", thumbnails.Count);
            var expire = expiration ?? TimeSpan.FromDays(30);
            
            var batch = _db.CreateBatch();
            var tasks = new List<Task>();
            
            foreach (var kvp in thumbnails)
            {
                var key = GetThumbnailKey(kvp.Key.ToString());
                tasks.Add(batch.StringSetAsync(key, kvp.Value, expire));
            }
            
            batch.Execute();
            await Task.WhenAll(tasks);
            
            var totalSize = thumbnails.Values.Sum(t => t.Length);
            _logger.LogInformation("✅ Batch cached {Count} thumbnails, total size: {Size:N0} bytes ({SizeMB:F2} MB)", 
                thumbnails.Count, totalSize, totalSize / 1024.0 / 1024.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch cache thumbnails");
            // Fail gracefully
        }
    }

    #endregion
}

