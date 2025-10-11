using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Service to initialize MongoDB indexes on application startup
/// Ensures all required indexes exist for optimal query performance
/// </summary>
public class MongoDbInitializationService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbInitializationService> _logger;

    public MongoDbInitializationService(
        IMongoDatabase database,
        ILogger<MongoDbInitializationService> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initialize all MongoDB indexes
    /// Safe to call multiple times - MongoDB will skip existing indexes
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔧 Initializing MongoDB indexes...");
        
        try
        {
            await CreateCollectionIndexesAsync(cancellationToken);
            await CreateUserIndexesAsync(cancellationToken);
            await CreateLibraryIndexesAsync(cancellationToken);
            await CreateCacheFolderIndexesAsync(cancellationToken);
            await CreateScheduledJobIndexesAsync(cancellationToken);
            await CreateBackgroundJobIndexesAsync(cancellationToken);
            await CreateRefreshTokenIndexesAsync(cancellationToken);
            await CreateSystemSettingIndexesAsync(cancellationToken);
            
            _logger.LogInformation("✅ MongoDB indexes initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize MongoDB indexes");
            throw;
        }
    }

    private async Task CreateCollectionIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<Collection>("collections");
        var indexKeys = Builders<Collection>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<Collection>>
        {
            // Library queries - filter by libraryId
            new CreateIndexModel<Collection>(
                indexKeys.Combine(
                    indexKeys.Ascending(c => c.LibraryId),
                    indexKeys.Ascending(c => c.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_libraryId_isDeleted", Background = true }
            ),
            
            // Path lookup - unique constraint
            new CreateIndexModel<Collection>(
                indexKeys.Combine(
                    indexKeys.Ascending(c => c.Path),
                    indexKeys.Ascending(c => c.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_path_isDeleted", Unique = true, Background = true }
            ),
            
            // Active collections filter
            new CreateIndexModel<Collection>(
                indexKeys.Combine(
                    indexKeys.Ascending(c => c.IsActive),
                    indexKeys.Ascending(c => c.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_isActive_isDeleted", Background = true }
            ),
            
            // Collection type filter
            new CreateIndexModel<Collection>(
                indexKeys.Combine(
                    indexKeys.Ascending(c => c.Type),
                    indexKeys.Ascending(c => c.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_type_isDeleted", Background = true }
            ),
            
            // Text search index - name, description, tags, keywords
            new CreateIndexModel<Collection>(
                indexKeys.Combine(
                    indexKeys.Text(c => c.Name),
                    indexKeys.Text(c => c.Description),
                    indexKeys.Text("metadata.tags"),
                    indexKeys.Text("searchIndex.keywords")
                ),
                new CreateIndexOptions 
                { 
                    Name = "idx_text_search", 
                    Background = true,
                    Weights = new MongoDB.Bson.BsonDocument
                    {
                        { "name", 10 },
                        { "metadata.tags", 5 },
                        { "searchIndex.keywords", 3 },
                        { "description", 1 }
                    }
                }
            ),
            
            // Sort by creation date (newest)
            new CreateIndexModel<Collection>(
                indexKeys.Combine(
                    indexKeys.Descending(c => c.CreatedAt),
                    indexKeys.Ascending(c => c.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_createdAt_desc_isDeleted", Background = true }
            ),
            
            // Sort by update date (recently updated)
            new CreateIndexModel<Collection>(
                indexKeys.Combine(
                    indexKeys.Descending(c => c.UpdatedAt),
                    indexKeys.Ascending(c => c.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_updatedAt_desc_isDeleted", Background = true }
            ),
            
            // Embedded images path lookup
            new CreateIndexModel<Collection>(
                indexKeys.Ascending("images.path"),
                new CreateIndexOptions { Name = "idx_images_path", Background = true, Sparse = true }
            ),
            
            // Cache images path lookup
            new CreateIndexModel<Collection>(
                indexKeys.Ascending("cacheImages.cachePath"),
                new CreateIndexOptions { Name = "idx_cacheImages_cachePath", Background = true, Sparse = true }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 9 indexes for collections");
    }

    private async Task CreateUserIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<User>("users");
        var indexKeys = Builders<User>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<User>>
        {
            // Username lookup - login authentication
            new CreateIndexModel<User>(
                indexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Name = "idx_username", Unique = true, Background = true }
            ),
            
            // Email lookup - login/password reset
            new CreateIndexModel<User>(
                indexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Name = "idx_email", Unique = true, Background = true }
            ),
            
            // Active users filter
            new CreateIndexModel<User>(
                indexKeys.Combine(
                    indexKeys.Ascending(u => u.IsActive),
                    indexKeys.Ascending(u => u.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_isActive_isDeleted_users", Background = true }
            ),
            
            // Role-based access control
            new CreateIndexModel<User>(
                indexKeys.Combine(
                    indexKeys.Ascending(u => u.Role),
                    indexKeys.Ascending(u => u.IsActive)
                ),
                new CreateIndexOptions { Name = "idx_role_isActive", Background = true }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 4 indexes for users");
    }

    private async Task CreateLibraryIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<Library>("libraries");
        var indexKeys = Builders<Library>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<Library>>
        {
            // Owner queries - get user's libraries
            new CreateIndexModel<Library>(
                indexKeys.Combine(
                    indexKeys.Ascending(l => l.OwnerId),
                    indexKeys.Ascending(l => l.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_ownerId_isDeleted", Background = true }
            ),
            
            // Path lookup - unique constraint
            new CreateIndexModel<Library>(
                indexKeys.Combine(
                    indexKeys.Ascending(l => l.Path),
                    indexKeys.Ascending(l => l.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_path_isDeleted_libraries", Unique = true, Background = true }
            ),
            
            // Active libraries filter
            new CreateIndexModel<Library>(
                indexKeys.Combine(
                    indexKeys.Ascending(l => l.IsActive),
                    indexKeys.Ascending(l => l.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_isActive_isDeleted_libraries", Background = true }
            ),
            
            // Public libraries filter
            new CreateIndexModel<Library>(
                indexKeys.Combine(
                    indexKeys.Ascending(l => l.IsPublic),
                    indexKeys.Ascending(l => l.IsActive),
                    indexKeys.Ascending(l => l.IsDeleted)
                ),
                new CreateIndexOptions { Name = "idx_isPublic_isActive_isDeleted", Background = true }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 4 indexes for libraries");
    }

    private async Task CreateCacheFolderIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<CacheFolder>("cache_folders");
        var indexKeys = Builders<CacheFolder>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<CacheFolder>>
        {
            // Path lookup - unique constraint
            new CreateIndexModel<CacheFolder>(
                indexKeys.Ascending(cf => cf.Path),
                new CreateIndexOptions { Name = "idx_path_cache_folders", Unique = true, Background = true }
            ),
            
            // Active cache folders ordered by priority (HOT PATH!)
            new CreateIndexModel<CacheFolder>(
                indexKeys.Combine(
                    indexKeys.Ascending(cf => cf.IsActive),
                    indexKeys.Ascending(cf => cf.Priority)
                ),
                new CreateIndexOptions { Name = "idx_isActive_priority", Background = true }
            ),
            
            // Cached collection IDs lookup
            new CreateIndexModel<CacheFolder>(
                indexKeys.Ascending(cf => cf.CachedCollectionIds),
                new CreateIndexOptions { Name = "idx_cachedCollectionIds", Background = true, Sparse = true }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 3 indexes for cache_folders");
    }

    private async Task CreateScheduledJobIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<ScheduledJob>("scheduled_jobs");
        var indexKeys = Builders<ScheduledJob>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<ScheduledJob>>
        {
            // Job type and enabled status
            new CreateIndexModel<ScheduledJob>(
                indexKeys.Combine(
                    indexKeys.Ascending(j => j.JobType),
                    indexKeys.Ascending(j => j.IsEnabled)
                ),
                new CreateIndexOptions { Name = "idx_jobType_isEnabled", Background = true }
            ),
            
            // Library-specific jobs
            new CreateIndexModel<ScheduledJob>(
                indexKeys.Combine(
                    indexKeys.Ascending("libraryId"),
                    indexKeys.Ascending(j => j.IsEnabled)
                ),
                new CreateIndexOptions { Name = "idx_libraryId_isEnabled", Background = true, Sparse = true }
            ),
            
            // Next run time (for job scheduler)
            new CreateIndexModel<ScheduledJob>(
                indexKeys.Combine(
                    indexKeys.Ascending(j => j.NextRunAt),
                    indexKeys.Ascending(j => j.IsEnabled)
                ),
                new CreateIndexOptions { Name = "idx_nextRunAt_isEnabled", Background = true, Sparse = true }
            ),
            
            // Hangfire job ID lookup
            new CreateIndexModel<ScheduledJob>(
                indexKeys.Ascending(j => j.HangfireJobId),
                new CreateIndexOptions { Name = "idx_hangfireJobId", Background = true, Sparse = true }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 4 indexes for scheduled_jobs");
    }

    private async Task CreateBackgroundJobIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<BackgroundJob>("background_jobs");
        var indexKeys = Builders<BackgroundJob>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<BackgroundJob>>
        {
            // Job status and type
            new CreateIndexModel<BackgroundJob>(
                indexKeys.Combine(
                    indexKeys.Ascending(j => j.Status),
                    indexKeys.Ascending(j => j.JobType)
                ),
                new CreateIndexOptions { Name = "idx_status_jobType", Background = true }
            ),
            
            // Created date (for cleanup and history)
            new CreateIndexModel<BackgroundJob>(
                indexKeys.Descending(j => j.CreatedAt),
                new CreateIndexOptions { Name = "idx_createdAt_desc_background_jobs", Background = true }
            ),
            
            // Started date (for monitoring active jobs)
            new CreateIndexModel<BackgroundJob>(
                indexKeys.Descending(j => j.StartedAt),
                new CreateIndexOptions { Name = "idx_startedAt_desc", Background = true, Sparse = true }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 3 indexes for background_jobs");
    }

    private async Task CreateRefreshTokenIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<RefreshToken>("refresh_tokens");
        var indexKeys = Builders<RefreshToken>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<RefreshToken>>
        {
            // Token lookup
            new CreateIndexModel<RefreshToken>(
                indexKeys.Ascending(rt => rt.Token),
                new CreateIndexOptions { Name = "idx_token", Unique = true, Background = true }
            ),
            
            // User sessions
            new CreateIndexModel<RefreshToken>(
                indexKeys.Combine(
                    indexKeys.Ascending(rt => rt.UserId),
                    indexKeys.Ascending(rt => rt.ExpiresAt)
                ),
                new CreateIndexOptions { Name = "idx_userId_expiresAt", Background = true }
            ),
            
            // TTL index - auto-delete expired tokens
            new CreateIndexModel<RefreshToken>(
                indexKeys.Ascending(rt => rt.ExpiresAt),
                new CreateIndexOptions 
                { 
                    Name = "idx_expiresAt", 
                    Background = true,
                    ExpireAfter = TimeSpan.Zero // Delete immediately after expiration
                }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 3 indexes for refresh_tokens (including TTL)");
    }

    private async Task CreateSystemSettingIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<SystemSetting>("system_settings");
        var indexKeys = Builders<SystemSetting>.IndexKeys;
        
        var indexes = new List<CreateIndexModel<SystemSetting>>
        {
            // SettingKey lookup - unique constraint
            new CreateIndexModel<SystemSetting>(
                indexKeys.Ascending(s => s.SettingKey),
                new CreateIndexOptions { Name = "idx_settingKey_system_settings", Unique = true, Background = true }
            ),
            
            // Category filter - for grouping settings by category
            new CreateIndexModel<SystemSetting>(
                indexKeys.Ascending(s => s.Category),
                new CreateIndexOptions { Name = "idx_category_system_settings", Background = true }
            )
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        _logger.LogDebug("✓ Created 2 indexes for system_settings");
    }
}

