using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of CollectionArchiveRepository
/// </summary>
public class CollectionArchiveRepository : ICollectionArchiveRepository
{
    private readonly IMongoCollection<CollectionArchive> _collection;
    private readonly ILogger<CollectionArchiveRepository> _logger;

    public CollectionArchiveRepository(IMongoDatabase database, ILogger<CollectionArchiveRepository> logger)
    {
        _collection = database.GetCollection<CollectionArchive>("collection_archives");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create indexes
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        try
        {
            // Index on originalId for fast lookups
            var originalIdIndex = Builders<CollectionArchive>.IndexKeys.Ascending(c => c.OriginalId);
            _collection.Indexes.CreateOne(new CreateIndexModel<CollectionArchive>(originalIdIndex, new CreateIndexOptions { Unique = true }));

            // Index on archivedAt for sorting
            var archivedAtIndex = Builders<CollectionArchive>.IndexKeys.Descending(c => c.ArchivedAt);
            _collection.Indexes.CreateOne(new CreateIndexModel<CollectionArchive>(archivedAtIndex));

            // Index on archiveReason for filtering
            var archiveReasonIndex = Builders<CollectionArchive>.IndexKeys.Ascending(c => c.ArchiveReason);
            _collection.Indexes.CreateOne(new CreateIndexModel<CollectionArchive>(archiveReasonIndex));

            // Index on name and path for searching
            var nameIndex = Builders<CollectionArchive>.IndexKeys.Text(c => c.Name).Text(c => c.Path);
            _collection.Indexes.CreateOne(new CreateIndexModel<CollectionArchive>(nameIndex));

            _logger.LogDebug("Created indexes for collection_archives collection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create indexes for collection_archives collection");
        }
    }

    public async Task<CollectionArchive> CreateAsync(CollectionArchive collectionArchive)
    {
        try
        {
            await _collection.InsertOneAsync(collectionArchive);
            _logger.LogDebug("Created archived collection {ArchiveId} for original collection {OriginalId}", 
                collectionArchive.Id, collectionArchive.OriginalId);
            return collectionArchive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create archived collection for original collection {OriginalId}", 
                collectionArchive.OriginalId);
            throw;
        }
    }

    public async Task<CollectionArchive?> GetByIdAsync(ObjectId archiveId)
    {
        try
        {
            var filter = Builders<CollectionArchive>.Filter.Eq(c => c.Id, archiveId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get archived collection by ID {ArchiveId}", archiveId);
            throw;
        }
    }

    public async Task<CollectionArchive?> GetByOriginalIdAsync(ObjectId originalCollectionId)
    {
        try
        {
            var filter = Builders<CollectionArchive>.Filter.Eq(c => c.OriginalId, originalCollectionId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get archived collection by original ID {OriginalId}", originalCollectionId);
            throw;
        }
    }

    public async Task<IEnumerable<CollectionArchive>> GetArchivedCollectionsAsync(int page = 1, int pageSize = 20, string sortBy = "archivedAt", string sortDirection = "desc")
    {
        try
        {
            var sortDefinition = BuildSortDefinition(sortBy, sortDirection);
            var skip = (page - 1) * pageSize;

            return await _collection
                .Find(Builders<CollectionArchive>.Filter.Empty)
                .Sort(sortDefinition)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get archived collections");
            throw;
        }
    }

    public async Task<long> GetTotalArchivedCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(Builders<CollectionArchive>.Filter.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total archived collections count");
            throw;
        }
    }

    public async Task<IEnumerable<CollectionArchive>> SearchArchivedCollectionsAsync(string query, int page = 1, int pageSize = 20)
    {
        try
        {
            var filter = Builders<CollectionArchive>.Filter.Text(query);
            var skip = (page - 1) * pageSize;

            return await _collection
                .Find(filter)
                .Sort(Builders<CollectionArchive>.Sort.Descending(c => c.ArchivedAt))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search archived collections with query: {Query}", query);
            throw;
        }
    }

    public async Task<IEnumerable<CollectionArchive>> GetArchivedCollectionsByReasonAsync(string archiveReason, int page = 1, int pageSize = 20)
    {
        try
        {
            var filter = Builders<CollectionArchive>.Filter.Eq(c => c.ArchiveReason, archiveReason);
            var skip = (page - 1) * pageSize;

            return await _collection
                .Find(filter)
                .Sort(Builders<CollectionArchive>.Sort.Descending(c => c.ArchivedAt))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get archived collections by reason: {Reason}", archiveReason);
            throw;
        }
    }

    public async Task DeleteAsync(ObjectId archiveId)
    {
        try
        {
            var filter = Builders<CollectionArchive>.Filter.Eq(c => c.Id, archiveId);
            var result = await _collection.DeleteOneAsync(filter);
            
            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("No archived collection found with ID {ArchiveId} to delete", archiveId);
            }
            else
            {
                _logger.LogDebug("Deleted archived collection {ArchiveId}", archiveId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete archived collection {ArchiveId}", archiveId);
            throw;
        }
    }

    public async Task<ArchiveStatistics> GetArchiveStatisticsAsync()
    {
        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "totalCollections", new BsonDocument("$sum", 1) },
                    { "totalImages", new BsonDocument("$sum", "$statistics.totalItems") },
                    { "totalSize", new BsonDocument("$sum", "$statistics.totalSize") },
                    { "oldestArchive", new BsonDocument("$min", "$archivedAt") },
                    { "newestArchive", new BsonDocument("$max", "$archivedAt") }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "totalCollections", 1 },
                    { "totalImages", 1 },
                    { "totalSize", 1 },
                    { "oldestArchive", 1 },
                    { "newestArchive", 1 }
                })
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            
            var statistics = new ArchiveStatistics();
            
            if (result != null)
            {
                statistics.TotalArchivedCollections = result.GetValue("totalCollections", 0).ToInt64();
                statistics.TotalArchivedImages = result.GetValue("totalImages", 0).ToInt64();
                statistics.TotalArchivedSize = result.GetValue("totalSize", 0).ToInt64();
                
                if (result.Contains("oldestArchive") && !result["oldestArchive"].IsBsonNull)
                    statistics.OldestArchive = result["oldestArchive"].ToUniversalTime();
                
                if (result.Contains("newestArchive") && !result["newestArchive"].IsBsonNull)
                    statistics.NewestArchive = result["newestArchive"].ToUniversalTime();
            }

            // Get collections by reason
            var reasonPipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$archiveReason" },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };

            var reasonResults = await _collection.Aggregate<BsonDocument>(reasonPipeline).ToListAsync();
            foreach (var reasonResult in reasonResults)
            {
                var reason = reasonResult["_id"].AsString;
                var count = reasonResult["count"].ToInt64();
                statistics.CollectionsByReason[reason] = count;
            }

            // Get collections by type
            var typePipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$type" },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };

            var typeResults = await _collection.Aggregate<BsonDocument>(typePipeline).ToListAsync();
            foreach (var typeResult in typeResults)
            {
                var type = typeResult["_id"].AsString;
                var count = typeResult["count"].ToInt64();
                statistics.CollectionsByType[type] = count;
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get archive statistics");
            throw;
        }
    }

    private SortDefinition<CollectionArchive> BuildSortDefinition(string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.ToLower() == "desc";
        
        return sortBy.ToLower() switch
        {
            "archivedat" => isDescending 
                ? Builders<CollectionArchive>.Sort.Descending(c => c.ArchivedAt)
                : Builders<CollectionArchive>.Sort.Ascending(c => c.ArchivedAt),
            "name" => isDescending 
                ? Builders<CollectionArchive>.Sort.Descending(c => c.Name)
                : Builders<CollectionArchive>.Sort.Ascending(c => c.Name),
            "type" => isDescending 
                ? Builders<CollectionArchive>.Sort.Descending(c => c.Type)
                : Builders<CollectionArchive>.Sort.Ascending(c => c.Type),
            "originalid" => isDescending 
                ? Builders<CollectionArchive>.Sort.Descending(c => c.OriginalId)
                : Builders<CollectionArchive>.Sort.Ascending(c => c.OriginalId),
            _ => Builders<CollectionArchive>.Sort.Descending(c => c.ArchivedAt)
        };
    }
}
