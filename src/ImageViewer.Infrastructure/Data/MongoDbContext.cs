using MongoDB.Driver;
using Microsoft.Extensions.Options;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB database context
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<Collection> Collections => _database.GetCollection<Collection>("collections");
    public IMongoCollection<Image> Images => _database.GetCollection<Image>("images");
    public IMongoCollection<CacheFolder> CacheFolders => _database.GetCollection<CacheFolder>("cache_folders");
    public IMongoCollection<ImageViewer.Domain.Entities.Tag> Tags => _database.GetCollection<ImageViewer.Domain.Entities.Tag>("tags");
    public IMongoCollection<CollectionTag> CollectionTags => _database.GetCollection<CollectionTag>("collection_tags");
    public IMongoCollection<ImageCacheInfo> ImageCacheInfos => _database.GetCollection<ImageCacheInfo>("image_cache_infos");
    public IMongoCollection<CollectionCacheBinding> CollectionCacheBindings => _database.GetCollection<CollectionCacheBinding>("collection_cache_bindings");
    public IMongoCollection<CollectionStatistics> CollectionStatistics => _database.GetCollection<CollectionStatistics>("collection_statistics");
    public IMongoCollection<ViewSession> ViewSessions => _database.GetCollection<ViewSession>("view_sessions");
    public IMongoCollection<BackgroundJob> BackgroundJobs => _database.GetCollection<BackgroundJob>("background_jobs");
    public IMongoCollection<CollectionSettingsEntity> CollectionSettings => _database.GetCollection<CollectionSettingsEntity>("collection_settings");
    public IMongoCollection<ImageMetadataEntity> ImageMetadata => _database.GetCollection<ImageMetadataEntity>("image_metadata");
}

/// <summary>
/// MongoDB configuration options
/// </summary>
public class MongoDbOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "image_viewer";
}
