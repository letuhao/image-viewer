using System.Text.Json;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
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
    private const string DASHBOARD_STATS_KEY = "dashboard:statistics";
    private const string DASHBOARD_METADATA_KEY = "dashboard:metadata";

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
            _logger.LogInformation("📊 Redis connection status: IsConnected={IsConnected}", _redis.IsConnected);
            
            // CRITICAL: Wait for Redis to be ready before attempting rebuild
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("⚠️ Redis not connected yet, waiting up to 10s...");
                var waited = 0;
                while (!_redis.IsConnected && waited < 10000)
                {
                    _logger.LogDebug("⏳ Still waiting for Redis... ({Waited}ms)", waited);
                    await Task.Delay(500, cancellationToken);
                    waited += 500;
                }
                
                if (!_redis.IsConnected)
                {
                    _logger.LogError("❌ Redis connection timeout after {Waited}ms, skipping index rebuild", waited);
                    _logger.LogError("📊 Final connection status: {Status}", _redis.GetStatus());
                    return; // Skip rebuild, will retry on next startup
                }
                
                _logger.LogInformation("✅ Redis connected after waiting {Waited}ms", waited);
            }
            else
            {
                _logger.LogInformation("✅ Redis already connected, proceeding with rebuild");
            }
            
            var startTime = DateTime.UtcNow;

            // Get all non-deleted collections
            var collections = await _collectionRepository.FindAsync(
                MongoDB.Driver.Builders<Collection>.Filter.Eq(c => c.IsDeleted, false),
                MongoDB.Driver.Builders<Collection>.Sort.Ascending(c => c.Id),
                0, // 0 = no limit, get all collections
                0  // 0 = no skip
            );

            var collectionList = collections.ToList();
            _logger.LogInformation("📊 Found {Count} collections to index from MongoDB", collectionList.Count);

            // Smart detection: If MongoDB has few collections but Redis has many keys, do a fast FLUSHDB
            // Only if Redis connection is ready (avoid timeout if connection still establishing)
            _logger.LogInformation("🔍 Checking Redis connection before clearing old data...");
            if (_redis.IsConnected)
            {
                _logger.LogInformation("✅ Redis connected, checking for stale data...");
                var redisKeyCount = await GetRedisKeyCountAsync();
                var mongoCollectionCount = collectionList.Count;
                _logger.LogInformation("📊 Redis keys: {RedisKeys}, MongoDB collections: {MongoCollections}", 
                    redisKeyCount, mongoCollectionCount);
                
                if (mongoCollectionCount < 100 && redisKeyCount > mongoCollectionCount * 10)
                {
                    _logger.LogWarning("⚠️ Detected stale Redis data: {RedisKeys} keys but only {MongoCollections} collections. Using FLUSHDB for fast cleanup.", 
                        redisKeyCount, mongoCollectionCount);
                    await FlushRedisAsync();
                    _logger.LogInformation("✅ Redis flushed successfully");
                }
                else
                {
                    // Normal clear: scan and delete specific keys
                    _logger.LogInformation("🧹 Clearing old Redis index data...");
                    await ClearIndexAsync();
                    _logger.LogInformation("✅ Old Redis index cleared");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Redis connection not ready yet, using FLUSHDB as safe fallback");
                await FlushRedisAsync();
                _logger.LogInformation("✅ Redis flushed successfully");
            }

            // Build sorted sets and hash entries
            _logger.LogInformation("🔨 Building Redis index for {Count} collections...", collectionList.Count);
            var batch = _db.CreateBatch();
            var tasks = new List<Task>();

            var processedCount = 0;
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
                
                processedCount++;
                if (processedCount % 1000 == 0)
                {
                    _logger.LogInformation("📊 Processed {Count}/{Total} collections...", processedCount, collectionList.Count);
                }
            }

            // Execute batch
            _logger.LogInformation("💾 Executing Redis batch write for {Count} collections...", collectionList.Count);
            batch.Execute();
            await Task.WhenAll(tasks);
            _logger.LogInformation("✅ Batch write completed successfully");

            // Update statistics
            _logger.LogInformation("📊 Updating index statistics...");
            await _db.StringSetAsync(LAST_REBUILD_KEY, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await _db.StringSetAsync(STATS_KEY + ":total", collectionList.Count);
            _logger.LogInformation("✅ Index statistics updated");

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
            
            // First, get the collection summary to know which secondary indexes to clean
            var summary = await GetCollectionSummaryAsync(collectionIdStr);

            var sortFields = new[] { "updatedAt", "createdAt", "name", "imageCount", "totalSize" };
            var sortDirections = new[] { "asc", "desc" };
            var tasks = new List<Task>();

            // Remove from primary indexes
            foreach (var field in sortFields)
            {
                foreach (var direction in sortDirections)
                {
                    var key = GetSortedSetKey(field, direction);
                    tasks.Add(_db.SortedSetRemoveAsync(key, collectionIdStr));
                }
            }
            
            // Remove from secondary indexes (if summary found)
            if (summary != null)
            {
                // Remove from by_library indexes
                if (!string.IsNullOrEmpty(summary.LibraryId))
                {
                    foreach (var field in sortFields)
                    {
                        foreach (var direction in sortDirections)
                        {
                            var key = GetSecondaryIndexKey("by_library", summary.LibraryId, field, direction);
                            tasks.Add(_db.SortedSetRemoveAsync(key, collectionIdStr));
                        }
                    }
                }
                
                // Remove from by_type indexes
                foreach (var field in sortFields)
                {
                    foreach (var direction in sortDirections)
                    {
                        var key = GetSecondaryIndexKey("by_type", summary.Type.ToString(), field, direction);
                        tasks.Add(_db.SortedSetRemoveAsync(key, collectionIdStr));
                    }
                }
                
                _logger.LogDebug("Removing collection from {PrimaryCount} primary + {SecondaryCount} secondary indexes", 
                    sortFields.Length * sortDirections.Length, 
                    (sortFields.Length * sortDirections.Length * 2)); // library + type
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
            // Note: ZRANK always returns ascending rank (0 = lowest score), Order parameter is ignored
            var rank = await _db.SortedSetRankAsync(key, collectionIdStr);
            
            if (!rank.HasValue)
            {
                _logger.LogWarning("Collection {CollectionId} not found in index", collectionId);
                // Fallback: try to get from database
                var collection = await _collectionRepository.GetByIdAsync(collectionId);
                if (collection != null && !collection.IsDeleted)
                {
                    await AddOrUpdateCollectionAsync(collection);
                    rank = await _db.SortedSetRankAsync(key, collectionIdStr);
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
                // ZRANGE by rank should always use Order.Ascending (rank 0, 1, 2...)
                if (rank.Value > 0)
                {
                    var prevEntries = await _db.SortedSetRangeByRankAsync(key, rank.Value - 1, rank.Value - 1, Order.Ascending);
                    previousId = prevEntries.FirstOrDefault().ToString();
                }

                // Get next (rank + 1)
                if (rank.Value < totalCount - 1)
                {
                    var nextEntries = await _db.SortedSetRangeByRankAsync(key, rank.Value + 1, rank.Value + 1, Order.Ascending);
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
            // Note: ZRANK always returns ascending rank (0 = lowest score)
            var rank = await _db.SortedSetRankAsync(key, collectionIdStr);
            
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

            // ABSOLUTE PAGINATION WITH CURRENT POSITION AWARENESS
            // Calculate which absolute page the current collection is on
            // Then use standard pagination from there
            // 
            // Example: current at rank 24,423, pageSize 20
            //   currentPage = floor(24,423 / 20) + 1 = 1221 + 1 = 1222
            //   Page 1222: ranks 24,420-24,439 (but clamped to 24,423)
            //   Page 1221: ranks 24,400-24,419
            //   Page 1223: ranks 24,440+ (empty if beyond total)
            
            // Calculate which page contains the current collection
            var currentPageNumber = (currentPosition / pageSize) + 1;
            
            // Use the requested page parameter, but default to current page if page=1
            // This allows frontend to request page 1 and get the "current" page
            int actualPage = (page == 1) ? currentPageNumber : page;
            
            // Standard absolute pagination
            var startRank = (actualPage - 1) * pageSize;
            var endRank = Math.Min((int)totalCount - 1, startRank + pageSize - 1);
            
            // Ensure valid range
            startRank = Math.Max(0, startRank);
            if (startRank >= totalCount)
            {
                startRank = Math.Max(0, (int)totalCount - pageSize);
                endRank = (int)totalCount - 1;
            }

            // Get collection IDs in range (O(log N + M))
            // ZRANGE by rank always uses ascending order (rank 0, 1, 2...)
            var collectionIds = await _db.SortedSetRangeByRankAsync(key, startRank, endRank, Order.Ascending);

            // Get collection summaries using batch MGET (10-20x faster!)
            var siblings = await BatchGetCollectionSummariesAsync(collectionIds);

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new CollectionSiblingsResult
            {
                Siblings = siblings,
                CurrentPosition = currentPosition + 1, // 1-based
                CurrentPage = actualPage,
                TotalCount = (int)totalCount,
                TotalPages = totalPages
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
            _logger.LogDebug("🔍 Checking if Redis index is valid...");
            _logger.LogDebug("📊 Redis connection status: IsConnected={IsConnected}", _redis.IsConnected);
            
            // Check if at least one sorted set exists and has entries
            var key = GetSortedSetKey("updatedAt", "desc");
            _logger.LogDebug("🔍 Checking sorted set key: {Key}", key);
            var count = await _db.SortedSetLengthAsync(key);
            _logger.LogDebug("📊 Sorted set count: {Count}", count);
            
            if (count == 0)
            {
                _logger.LogInformation("⚠️ Index invalid: Sorted set is empty");
                return false;
            }

            // Check if last rebuild time exists
            _logger.LogDebug("🔍 Checking last rebuild key: {Key}", LAST_REBUILD_KEY);
            var lastRebuildExists = await _db.KeyExistsAsync(LAST_REBUILD_KEY);
            _logger.LogDebug("📊 Last rebuild key exists: {Exists}", lastRebuildExists);
            
            if (!lastRebuildExists)
            {
                _logger.LogInformation("⚠️ Index invalid: Last rebuild key missing");
                return false;
            }
            
            _logger.LogInformation("✅ Index is valid: {Count} entries found", count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error checking index validity");
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
        var libraryId = collection.LibraryId?.ToString() ?? "null";
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
            LibraryId = collection.LibraryId?.ToString() ?? string.Empty,
            Description = collection.Description,
            Type = (int)collection.Type,
            Tags = new List<string>(), // TODO: Add tags support when Collection entity has it
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

        try
        {
            return JsonSerializer.Deserialize<CollectionSummary>(json.ToString());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize collection summary for {CollectionId}", collectionId);
            return null;
        }
    }

    /// <summary>
    /// Batch get multiple collection summaries using MGET (10-20x faster than sequential GET)
    /// </summary>
    private async Task<List<CollectionSummary>> BatchGetCollectionSummariesAsync(RedisValue[] collectionIds)
    {
        if (collectionIds.Length == 0)
            return new List<CollectionSummary>();

        try
        {
            // Build hash keys for batch retrieval
            var hashKeys = collectionIds.Select(id => (RedisKey)GetHashKey(id.ToString())).ToArray();
            
            // Single MGET call instead of N GET calls (10-20x faster!)
            var jsonValues = await _db.StringGetAsync(hashKeys);
            
            // Deserialize all summaries
            var summaries = new List<CollectionSummary>();
            for (int i = 0; i < jsonValues.Length; i++)
            {
                if (jsonValues[i].HasValue)
                {
                    try
                    {
                        var summary = JsonSerializer.Deserialize<CollectionSummary>(jsonValues[i].ToString());
                        if (summary != null)
                        {
                            summaries.Add(summary);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize collection {Id}", collectionIds[i]);
                    }
                }
            }
            
            _logger.LogDebug("Batch retrieved {Found}/{Total} collection summaries", summaries.Count, collectionIds.Length);
            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch get collection summaries");
            return new List<CollectionSummary>();
        }
    }

    private async Task ClearIndexAsync()
    {
        _logger.LogDebug("Clearing existing index...");

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var tasks = new List<Task>();
            
            // Find and delete all sorted set indexes (primary + secondary)
            // Use async enumeration to avoid timeout on large key sets
            var sortedSetCount = 0;
            await foreach (var key in server.KeysAsync(pattern: $"{SORTED_SET_PREFIX}*", pageSize: 1000))
            {
                tasks.Add(_db.KeyDeleteAsync(key));
                sortedSetCount++;
            }
            _logger.LogDebug("Found {Count} sorted set keys to delete", sortedSetCount);
            
            // Find and delete all collection hashes
            var hashCount = 0;
            await foreach (var key in server.KeysAsync(pattern: $"{HASH_PREFIX}*", pageSize: 1000))
            {
                tasks.Add(_db.KeyDeleteAsync(key));
                hashCount++;
            }
            _logger.LogDebug("Found {Count} collection hashes to delete", hashCount);
            
            // Note: Don't clear thumbnails - they can persist for performance
            // Thumbnails have 30-day expiration anyway
            
            await Task.WhenAll(tasks);
            
            _logger.LogInformation("✅ Cleared {SortedSets} sorted sets and {Hashes} hashes", 
                sortedSetCount, hashCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear index");
            throw;
        }
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
            // ZRANGE by rank always uses ascending order (rank 0, 1, 2...)
            var collectionIds = await _db.SortedSetRangeByRankAsync(key, startRank, endRank, Order.Ascending);

            // Get collection summaries using batch MGET (10-20x faster than sequential GET!)
            var collections = await BatchGetCollectionSummariesAsync(collectionIds);

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
            // ZRANGE by rank always uses ascending order (rank 0, 1, 2...)
            var collectionIds = await _db.SortedSetRangeByRankAsync(key, startRank, endRank, Order.Ascending);

            // Get collection summaries using batch MGET (10-20x faster!)
            var collections = await BatchGetCollectionSummariesAsync(collectionIds);

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
            // ZRANGE by rank always uses ascending order (rank 0, 1, 2...)
            var collectionIds = await _db.SortedSetRangeByRankAsync(key, startRank, endRank, Order.Ascending);

            // Get collection summaries using batch MGET (10-20x faster!)
            var collections = await BatchGetCollectionSummariesAsync(collectionIds);

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
    
    /// <summary>
    /// Get total number of keys in Redis (fast O(1) operation)
    /// </summary>
    private async Task<long> GetRedisKeyCountAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            // Use DBSIZE for fast key count (O(1) operation)
            var keyCount = await server.DatabaseSizeAsync();
            return keyCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Redis key count, assuming 0");
            return 0;
        }
    }
    
    /// <summary>
    /// Fast flush of entire Redis database (faster than scanning/deleting individual keys)
    /// Use when MongoDB has been cleared but Redis still has stale data
    /// </summary>
    private async Task FlushRedisAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            _logger.LogInformation("✅ Flushed entire Redis database (fast cleanup for stale data)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush Redis database");
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

    #region Dashboard Statistics

    /// <summary>
    /// Get dashboard statistics from Redis cache (ultra-fast)
    /// </summary>
    public async Task<DashboardStatistics?> GetDashboardStatisticsAsync()
    {
        try
        {
            var statsJson = await _db.StringGetAsync(DASHBOARD_STATS_KEY);
            if (!statsJson.HasValue)
            {
                _logger.LogDebug("No dashboard statistics found in Redis cache");
                return null;
            }

            var stats = JsonSerializer.Deserialize<DashboardStatistics>(statsJson);
            _logger.LogDebug("✅ Retrieved dashboard statistics from Redis cache");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get dashboard statistics from Redis");
            return null;
        }
    }

    /// <summary>
    /// Store dashboard statistics in Redis cache
    /// </summary>
    public async Task StoreDashboardStatisticsAsync(DashboardStatistics stats)
    {
        try
        {
            stats.LastUpdated = DateTime.UtcNow;
            var statsJson = JsonSerializer.Serialize(stats);
            
            // Store with 5-minute expiration for auto-refresh
            await _db.StringSetAsync(DASHBOARD_STATS_KEY, statsJson, TimeSpan.FromMinutes(5));
            
            _logger.LogDebug("✅ Stored dashboard statistics in Redis cache");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store dashboard statistics in Redis");
        }
    }

    /// <summary>
    /// Update dashboard statistics incrementally (for real-time updates)
    /// </summary>
    public async Task UpdateDashboardStatisticsAsync(string updateType, object updateData)
    {
        try
        {
            var metadata = new
            {
                Type = updateType,
                Data = updateData,
                Timestamp = DateTime.UtcNow
            };

            var metadataJson = JsonSerializer.Serialize(metadata);
            
            // Store update metadata
            await _db.ListLeftPushAsync(DASHBOARD_METADATA_KEY, metadataJson);
            
            // Keep only last 100 updates
            await _db.ListTrimAsync(DASHBOARD_METADATA_KEY, 0, 99);
            
            // Invalidate dashboard stats to force refresh
            await _db.KeyDeleteAsync(DASHBOARD_STATS_KEY);
            
            _logger.LogDebug("✅ Updated dashboard metadata: {UpdateType}", updateType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update dashboard metadata");
        }
    }

    /// <summary>
    /// Get recent dashboard activity from Redis
    /// </summary>
    public async Task<List<object>> GetRecentDashboardActivityAsync(int limit = 10)
    {
        try
        {
            var activities = await _db.ListRangeAsync(DASHBOARD_METADATA_KEY, 0, limit - 1);
            var result = new List<object>();

            foreach (var activity in activities)
            {
                if (activity.HasValue)
                {
                    var activityObj = JsonSerializer.Deserialize<object>(activity);
                    result.Add(activityObj);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get recent dashboard activity");
            return new List<object>();
        }
    }

    /// <summary>
    /// Check if dashboard statistics are fresh (less than 1 minute old)
    /// </summary>
    public async Task<bool> IsDashboardStatisticsFreshAsync()
    {
        try
        {
            var stats = await GetDashboardStatisticsAsync();
            if (stats == null) return false;

            var age = DateTime.UtcNow - stats.LastUpdated;
            return age.TotalMinutes < 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check dashboard statistics freshness");
            return false;
        }
    }

    #endregion
}


