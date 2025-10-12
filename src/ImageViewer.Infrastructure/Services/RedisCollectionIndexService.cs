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
    private readonly IThumbnailService? _thumbnailService;

    // Redis key patterns
    private const string SORTED_SET_PREFIX = "collection_index:sorted:";
    private const string HASH_PREFIX = "collection_index:data:";
    private const string STATS_KEY = "collection_index:stats";
    private const string LAST_REBUILD_KEY = "collection_index:last_rebuild";

    public RedisCollectionIndexService(
        IConnectionMultiplexer redis,
        ICollectionRepository collectionRepository,
        ILogger<RedisCollectionIndexService> logger,
        IThumbnailService? thumbnailService = null)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _collectionRepository = collectionRepository;
        _logger = logger;
        _thumbnailService = thumbnailService;
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
                    // Load thumbnail URL if available
                    if (_thumbnailService != null && !string.IsNullOrEmpty(summary.FirstImageId))
                    {
                        try
                        {
                            summary.FirstImageThumbnailUrl = await _thumbnailService.GetThumbnailUrlAsync(
                                ObjectId.Parse(id.ToString()), 
                                ObjectId.Parse(summary.FirstImageId), 
                                200);
                        }
                        catch
                        {
                            // Ignore thumbnail errors
                        }
                    }
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

    private async Task AddToSortedSetsAsync(IDatabaseAsync db, Collection collection)
    {
        var collectionIdStr = collection.Id.ToString();

        // Add to all sort field combinations
        var tasks = new List<Task>();

        // updatedAt
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("updatedAt", "asc"), collectionIdStr, collection.UpdatedAt.Ticks));
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("updatedAt", "desc"), collectionIdStr, -collection.UpdatedAt.Ticks)); // Negative for desc

        // createdAt
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("createdAt", "asc"), collectionIdStr, collection.CreatedAt.Ticks));
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("createdAt", "desc"), collectionIdStr, -collection.CreatedAt.Ticks));

        // name (use hash code for sorting, not perfect but works)
        var nameScore = collection.Name?.GetHashCode() ?? 0;
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("name", "asc"), collectionIdStr, nameScore));
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("name", "desc"), collectionIdStr, -nameScore));

        // imageCount
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("imageCount", "asc"), collectionIdStr, collection.Statistics.TotalItems));
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("imageCount", "desc"), collectionIdStr, -collection.Statistics.TotalItems));

        // totalSize
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("totalSize", "asc"), collectionIdStr, collection.Statistics.TotalSize));
        tasks.Add(db.SortedSetAddAsync(GetSortedSetKey("totalSize", "desc"), collectionIdStr, -collection.Statistics.TotalSize));

        await Task.WhenAll(tasks);
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
            UpdatedAt = collection.UpdatedAt
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
}

