# Migration Guide - ImageViewer Platform

## 📋 Tổng Quan

Document này mô tả comprehensive migration guide từ thiết kế cũ (PostgreSQL + EF Core) sang thiết kế mới (MongoDB + 57 Collections + 56 Feature Categories).

## 🎯 Migration Objectives

### **Primary Objectives**
1. **Zero Data Loss**: Đảm bảo không mất dữ liệu trong quá trình migration
2. **Minimal Downtime**: Minimize downtime during migration
3. **Backward Compatibility**: Maintain compatibility với existing clients
4. **Performance Improvement**: Cải thiện performance sau migration
5. **Feature Enhancement**: Enable new features và capabilities

### **Secondary Objectives**
1. **Automated Migration**: Automated migration processes
2. **Rollback Capability**: Quick rollback khi có issues
3. **Validation**: Comprehensive validation sau migration
4. **Documentation**: Clear migration procedures
5. **Training**: Team training cho new system

## 📊 Migration Scope

### **Data Migration**
- **Collections**: 14 collections → 57 collections
- **Features**: 46 categories → 56 categories
- **Database**: PostgreSQL → MongoDB
- **ORM**: Entity Framework Core → MongoDB Driver
- **Architecture**: Monolithic → Microservices-ready

### **Code Migration**
- **Domain Models**: Update cho MongoDB
- **Repositories**: Replace EF Core repositories
- **Services**: Update business logic
- **APIs**: Add new endpoints
- **Background Jobs**: Implement với RabbitMQ

### **Infrastructure Migration**
- **Database**: PostgreSQL → MongoDB
- **Message Queue**: Add RabbitMQ
- **Cache**: Add Redis
- **Monitoring**: Add Prometheus/Grafana
- **Deployment**: Add containerization

## 🗄️ Database Migration Strategy

### **Migration Phases**

#### **Phase 1: Schema Analysis**
```sql
-- Analyze existing PostgreSQL schema
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'public'
ORDER BY table_name, ordinal_position;

-- Analyze relationships
SELECT 
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY';
```

#### **Phase 2: Data Mapping**
```javascript
// PostgreSQL to MongoDB mapping
const migrationMapping = {
    // Core Collections
    'collections': {
        target: 'collections',
        mapping: {
            'id': '_id',
            'name': 'name',
            'path': 'path',
            'type': 'type',
            'created_at': 'createdAt',
            'updated_at': 'updatedAt'
        },
        transformations: [
            'Convert timestamps to ISODate',
            'Add MongoDB-specific fields',
            'Create embedded documents'
        ]
    },
    
    'images': {
        target: 'mediaItems',
        mapping: {
            'id': '_id',
            'collection_id': 'collectionId',
            'name': 'name',
            'path': 'path',
            'file_size': 'fileSize',
            'created_at': 'createdAt',
            'updated_at': 'updatedAt'
        },
        transformations: [
            'Convert to MediaItem entity',
            'Add media-specific fields',
            'Create metadata embedded document'
        ]
    },
    
    'tags': {
        target: 'tags',
        mapping: {
            'id': '_id',
            'name': 'name',
            'color': 'color',
            'created_at': 'createdAt'
        },
        transformations: [
            'Convert to Tag entity',
            'Add MongoDB-specific fields'
        ]
    },
    
    'cache_folders': {
        target: 'cacheFolders',
        mapping: {
            'id': '_id',
            'name': 'name',
            'path': 'path',
            'max_size': 'maxSize',
            'created_at': 'createdAt'
        },
        transformations: [
            'Convert to CacheFolder entity',
            'Add cache-specific fields'
        ]
    },
    
    'background_jobs': {
        target: 'backgroundJobs',
        mapping: {
            'id': '_id',
            'type': 'type',
            'status': 'status',
            'created_at': 'createdAt',
            'updated_at': 'updatedAt'
        },
        transformations: [
            'Convert to BackgroundJob entity',
            'Add job-specific fields',
            'Create progress embedded document'
        ]
    },
    
    'view_sessions': {
        target: 'viewSessions',
        mapping: {
            'id': '_id',
            'user_id': 'userId',
            'collection_id': 'collectionId',
            'started_at': 'startedAt',
            'ended_at': 'endedAt'
        },
        transformations: [
            'Convert to ViewSession entity',
            'Add session-specific fields',
            'Create settings embedded document'
        ]
    }
};
```

#### **Phase 3: Data Migration Script**
```csharp
public class DatabaseMigrationService
{
    private readonly IPostgreSQLRepository _postgresRepo;
    private readonly IMongoDBRepository _mongoRepo;
    private readonly ILogger<DatabaseMigrationService> _logger;
    
    public async Task<MigrationResult> MigrateAllDataAsync()
    {
        var result = new MigrationResult();
        
        try
        {
            _logger.LogInformation("Starting database migration...");
            
            // Migrate core collections
            await MigrateCollectionsAsync();
            await MigrateMediaItemsAsync();
            await MigrateTagsAsync();
            await MigrateCacheFoldersAsync();
            await MigrateBackgroundJobsAsync();
            await MigrateViewSessionsAsync();
            
            // Migrate new collections (empty initially)
            await CreateNewCollectionsAsync();
            
            // Validate migration
            await ValidateMigrationAsync();
            
            result.Success = true;
            result.Message = "Migration completed successfully";
            
            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Migration failed: {ex.Message}";
            result.Error = ex;
            
            _logger.LogError(ex, "Database migration failed");
        }
        
        return result;
    }
    
    private async Task MigrateCollectionsAsync()
    {
        _logger.LogInformation("Migrating collections...");
        
        var postgresCollections = await _postgresRepo.GetAllCollectionsAsync();
        var migratedCount = 0;
        
        foreach (var postgresCollection in postgresCollections)
        {
            var mongoCollection = new Collection
            {
                Id = ObjectId.GenerateNewId(),
                Name = postgresCollection.Name,
                Path = postgresCollection.Path,
                Type = postgresCollection.Type,
                Settings = new CollectionSettings
                {
                    Enabled = true,
                    AutoScan = true,
                    GenerateThumbnails = true,
                    GenerateCache = true
                },
                Metadata = new CollectionMetadata
                {
                    Description = postgresCollection.Description,
                    Tags = new List<string>(),
                    CustomFields = new Dictionary<string, object>()
                },
                Statistics = new CollectionStatistics
                {
                    TotalItems = 0,
                    TotalSize = 0,
                    LastScanDate = postgresCollection.CreatedAt,
                    ScanCount = 0
                },
                WatchInfo = new WatchInfo
                {
                    IsWatching = false,
                    WatchPath = postgresCollection.Path,
                    WatchFilters = new List<string> { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp" }
                },
                SearchIndex = new SearchIndex
                {
                    Tags = new List<string>(),
                    Metadata = new Dictionary<string, object>(),
                    LastIndexed = DateTime.UtcNow
                },
                CreatedAt = postgresCollection.CreatedAt,
                UpdatedAt = postgresCollection.UpdatedAt
            };
            
            await _mongoRepo.CreateCollectionAsync(mongoCollection);
            migratedCount++;
        }
        
        _logger.LogInformation($"Migrated {migratedCount} collections");
    }
    
    private async Task MigrateMediaItemsAsync()
    {
        _logger.LogInformation("Migrating media items...");
        
        var postgresImages = await _postgresRepo.GetAllImagesAsync();
        var migratedCount = 0;
        
        foreach (var postgresImage in postgresImages)
        {
            var mongoMediaItem = new MediaItem
            {
                Id = ObjectId.GenerateNewId(),
                CollectionId = await GetMongoCollectionIdAsync(postgresImage.CollectionId),
                Name = postgresImage.Name,
                Path = postgresImage.Path,
                Type = DetermineMediaType(postgresImage.Path),
                Settings = new MediaItemSettings
                {
                    Enabled = true,
                    GenerateThumbnail = true,
                    GenerateCache = true
                },
                Metadata = new MediaItemMetadata
                {
                    FileSize = postgresImage.FileSize,
                    Width = postgresImage.Width,
                    Height = postgresImage.Height,
                    Format = GetFileFormat(postgresImage.Path),
                    CreatedDate = postgresImage.CreatedAt,
                    ModifiedDate = postgresImage.UpdatedAt,
                    Tags = new List<string>(),
                    CustomFields = new Dictionary<string, object>()
                },
                Statistics = new MediaItemStatistics
                {
                    ViewCount = 0,
                    DownloadCount = 0,
                    LastViewed = null,
                    LastDownloaded = null
                },
                SearchIndex = new SearchIndex
                {
                    Tags = new List<string>(),
                    Metadata = new Dictionary<string, object>(),
                    LastIndexed = DateTime.UtcNow
                },
                CreatedAt = postgresImage.CreatedAt,
                UpdatedAt = postgresImage.UpdatedAt
            };
            
            await _mongoRepo.CreateMediaItemAsync(mongoMediaItem);
            migratedCount++;
        }
        
        _logger.LogInformation($"Migrated {migratedCount} media items");
    }
    
    private async Task CreateNewCollectionsAsync()
    {
        _logger.LogInformation("Creating new collections...");
        
        var newCollections = new[]
        {
            "libraries", "users", "userSettings", "systemSettings", "favoriteLists",
            "auditLogs", "errorLogs", "backupHistory", "performanceMetrics",
            "userBehaviorEvents", "userAnalytics", "contentPopularity", "searchAnalytics",
            "userCollections", "collectionRatings", "userFollows", "collectionComments",
            "userMessages", "conversations", "torrents", "downloadLinks", "torrentStatistics",
            "linkHealthChecker", "downloadQualityOptions", "distributionNodes", "nodePerformanceMetrics",
            "userRewards", "rewardTransactions", "rewardSettings", "rewardAchievements",
            "rewardBadges", "premiumFeatures", "userPremiumFeatures", "storageLocations",
            "fileStorageMapping", "contentModeration", "copyrightManagement", "searchHistory",
            "contentSimilarity", "mediaProcessingJobs", "customReports", "userSecurity",
            "notificationTemplates", "notificationQueue", "fileVersions", "filePermissions",
            "userGroups", "userActivityLogs", "systemHealth", "systemMaintenance"
        };
        
        foreach (var collectionName in newCollections)
        {
            await _mongoRepo.CreateCollectionAsync(collectionName);
        }
        
        _logger.LogInformation($"Created {newCollections.Length} new collections");
    }
}
```

## 🔄 Code Migration Strategy

### **Repository Migration**

#### **Old Repository (EF Core)**
```csharp
public class CollectionRepository : ICollectionRepository
{
    private readonly ImageViewerDbContext _context;
    
    public CollectionRepository(ImageViewerDbContext context)
    {
        _context = context;
    }
    
    public async Task<Collection> GetByIdAsync(int id)
    {
        return await _context.Collections
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<IEnumerable<Collection>> GetAllAsync()
    {
        return await _context.Collections
            .Include(c => c.Images)
            .ToListAsync();
    }
    
    public async Task<Collection> CreateAsync(Collection collection)
    {
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();
        return collection;
    }
}
```

#### **New Repository (MongoDB)**
```csharp
public class MongoCollectionRepository : ICollectionRepository
{
    private readonly IMongoCollection<Collection> _collections;
    
    public MongoCollectionRepository(MongoDbContext context)
    {
        _collections = context.Collections;
    }
    
    public async Task<Collection> GetByIdAsync(ObjectId id)
    {
        return await _collections
            .Find(c => c.Id == id)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<Collection>> GetAllAsync()
    {
        return await _collections
            .Find(_ => true)
            .ToListAsync();
    }
    
    public async Task<Collection> CreateAsync(Collection collection)
    {
        await _collections.InsertOneAsync(collection);
        return collection;
    }
    
    public async Task<IEnumerable<Collection>> SearchAsync(string query)
    {
        var filter = Builders<Collection>.Filter.Text(query);
        return await _collections
            .Find(filter)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId)
    {
        return await _collections
            .Find(c => c.LibraryId == libraryId)
            .ToListAsync();
    }
}
```

### **Service Migration**

#### **Old Service**
```csharp
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _repository;
    private readonly IImageRepository _imageRepository;
    
    public CollectionService(ICollectionRepository repository, IImageRepository imageRepository)
    {
        _repository = repository;
        _imageRepository = imageRepository;
    }
    
    public async Task<Collection> CreateCollectionAsync(CreateCollectionRequest request)
    {
        var collection = new Collection
        {
            Name = request.Name,
            Path = request.Path,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return await _repository.CreateAsync(collection);
    }
}
```

#### **New Service**
```csharp
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _repository;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly IMessageQueueService _messageQueue;
    private readonly ILogger<CollectionService> _logger;
    
    public CollectionService(
        ICollectionRepository repository,
        IMediaItemRepository mediaItemRepository,
        IMessageQueueService messageQueue,
        ILogger<CollectionService> logger)
    {
        _repository = repository;
        _mediaItemRepository = mediaItemRepository;
        _messageQueue = messageQueue;
        _logger = logger;
    }
    
    public async Task<Collection> CreateCollectionAsync(CreateCollectionRequest request)
    {
        var collection = new Collection
        {
            Id = ObjectId.GenerateNewId(),
            LibraryId = request.LibraryId,
            Name = request.Name,
            Path = request.Path,
            Type = request.Type,
            Settings = new CollectionSettings
            {
                Enabled = true,
                AutoScan = request.AutoScan,
                GenerateThumbnails = request.GenerateThumbnails,
                GenerateCache = request.GenerateCache
            },
            Metadata = new CollectionMetadata
            {
                Description = request.Description,
                Tags = request.Tags ?? new List<string>(),
                CustomFields = request.CustomFields ?? new Dictionary<string, object>()
            },
            Statistics = new CollectionStatistics
            {
                TotalItems = 0,
                TotalSize = 0,
                LastScanDate = DateTime.UtcNow,
                ScanCount = 0
            },
            WatchInfo = new WatchInfo
            {
                IsWatching = request.EnableWatching,
                WatchPath = request.Path,
                WatchFilters = request.WatchFilters ?? new List<string> { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp" }
            },
            SearchIndex = new SearchIndex
            {
                Tags = request.Tags ?? new List<string>(),
                Metadata = request.CustomFields ?? new Dictionary<string, object>(),
                LastIndexed = DateTime.UtcNow
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var createdCollection = await _repository.CreateAsync(collection);
        
        // Queue background job for collection scanning
        await _messageQueue.PublishAsync(new CollectionScanMessage
        {
            CollectionId = createdCollection.Id,
            LibraryId = createdCollection.LibraryId,
            Path = createdCollection.Path,
            ScanType = "full",
            Priority = 1
        });
        
        _logger.LogInformation($"Created collection {createdCollection.Name} with ID {createdCollection.Id}");
        
        return createdCollection;
    }
}
```

## 🚀 Migration Execution Plan

### **Pre-Migration Phase**

#### **1. Backup Current System**
```bash
# Backup PostgreSQL database
pg_dump -h localhost -U postgres -d imageviewer > backup_$(date +%Y%m%d_%H%M%S).sql

# Backup application files
tar -czf application_backup_$(date +%Y%m%d_%H%M%S).tar.gz /opt/imageviewer/

# Backup configuration files
tar -czf config_backup_$(date +%Y%m%d_%H%M%S).tar.gz /etc/imageviewer/
```

#### **2. Setup New Infrastructure**
```bash
# Install MongoDB
sudo apt-get install -y mongodb-org

# Install RabbitMQ
sudo apt-get install -y rabbitmq-server

# Install Redis
sudo apt-get install -y redis-server

# Start services
sudo systemctl start mongod
sudo systemctl start rabbitmq-server
sudo systemctl start redis-server
```

#### **3. Deploy New Application**
```bash
# Build new application
dotnet build -c Release

# Deploy API service
sudo systemctl stop imageviewer-api
sudo cp -r bin/Release/net8.0/* /opt/imageviewer/api/
sudo systemctl start imageviewer-api

# Deploy Worker service
sudo systemctl stop imageviewer-worker
sudo cp -r bin/Release/net8.0/* /opt/imageviewer/worker/
sudo systemctl start imageviewer-worker
```

### **Migration Phase**

#### **1. Run Data Migration**
```bash
# Run migration script
dotnet run --project src/ImageViewer.Migration -- --migrate-data

# Validate migration
dotnet run --project src/ImageViewer.Migration -- --validate-migration
```

#### **2. Update Application Configuration**
```bash
# Update connection strings
sudo nano /etc/imageviewer/environment

# Add new configuration
ConnectionStrings__MongoDB=mongodb://localhost:27017/imageviewer
ConnectionStrings__Redis=localhost:6379
RabbitMQ__HostName=localhost
RabbitMQ__Port=5672
RabbitMQ__UserName=imageviewer
RabbitMQ__Password=app_password
```

#### **3. Restart Services**
```bash
# Restart API service
sudo systemctl restart imageviewer-api

# Restart Worker service
sudo systemctl restart imageviewer-worker

# Check service status
sudo systemctl status imageviewer-api
sudo systemctl status imageviewer-worker
```

### **Post-Migration Phase**

#### **1. Validate Migration**
```bash
# Run validation tests
dotnet test tests/ImageViewer.IntegrationTests --filter "Category=Migration"

# Check data integrity
dotnet run --project src/ImageViewer.Migration -- --validate-data-integrity

# Check performance
dotnet run --project src/ImageViewer.Migration -- --validate-performance
```

#### **2. Update Monitoring**
```bash
# Update Prometheus configuration
sudo nano /etc/prometheus/prometheus.yml

# Add MongoDB targets
- job_name: 'mongodb'
  static_configs:
    - targets: ['localhost:9216']

# Restart Prometheus
sudo systemctl restart prometheus
```

#### **3. Update Documentation**
```bash
# Update API documentation
dotnet run --project src/ImageViewer.Api -- --generate-swagger

# Update deployment documentation
# Update user documentation
```

## 🔄 Rollback Strategy

### **Rollback Procedures**

#### **1. Application Rollback**
```bash
# Stop new services
sudo systemctl stop imageviewer-api
sudo systemctl stop imageviewer-worker

# Restore old application
sudo cp -r application_backup_20240101_120000/* /opt/imageviewer/

# Restore configuration
sudo cp -r config_backup_20240101_120000/* /etc/imageviewer/

# Start old services
sudo systemctl start imageviewer-api
sudo systemctl start imageviewer-worker
```

#### **2. Database Rollback**
```bash
# Stop new database
sudo systemctl stop mongod

# Restore PostgreSQL
sudo systemctl start postgresql
psql -h localhost -U postgres -d imageviewer < backup_20240101_120000.sql

# Verify data
psql -h localhost -U postgres -d imageviewer -c "SELECT COUNT(*) FROM collections;"
```

#### **3. Infrastructure Rollback**
```bash
# Stop new services
sudo systemctl stop rabbitmq-server
sudo systemctl stop redis-server

# Start old services
sudo systemctl start postgresql

# Update configuration
sudo nano /etc/imageviewer/environment
# Restore old connection strings
```

## 📊 Migration Validation

### **Data Validation**
```csharp
public class MigrationValidator
{
    private readonly IPostgreSQLRepository _postgresRepo;
    private readonly IMongoDBRepository _mongoRepo;
    
    public async Task<ValidationResult> ValidateMigrationAsync()
    {
        var result = new ValidationResult();
        
        // Validate collections
        var postgresCollections = await _postgresRepo.GetAllCollectionsAsync();
        var mongoCollections = await _mongoRepo.GetAllCollectionsAsync();
        
        if (postgresCollections.Count() != mongoCollections.Count())
        {
            result.AddError($"Collection count mismatch: PostgreSQL {postgresCollections.Count()}, MongoDB {mongoCollections.Count()}");
        }
        
        // Validate media items
        var postgresImages = await _postgresRepo.GetAllImagesAsync();
        var mongoMediaItems = await _mongoRepo.GetAllMediaItemsAsync();
        
        if (postgresImages.Count() != mongoMediaItems.Count())
        {
            result.AddError($"Media item count mismatch: PostgreSQL {postgresImages.Count()}, MongoDB {mongoMediaItems.Count()}");
        }
        
        // Validate data integrity
        await ValidateDataIntegrityAsync(result);
        
        return result;
    }
    
    private async Task ValidateDataIntegrityAsync(ValidationResult result)
    {
        // Validate collection data
        var postgresCollections = await _postgresRepo.GetAllCollectionsAsync();
        
        foreach (var postgresCollection in postgresCollections)
        {
            var mongoCollection = await _mongoRepo.GetCollectionByNameAsync(postgresCollection.Name);
            
            if (mongoCollection == null)
            {
                result.AddError($"Collection {postgresCollection.Name} not found in MongoDB");
                continue;
            }
            
            if (postgresCollection.Name != mongoCollection.Name)
            {
                result.AddError($"Collection name mismatch: {postgresCollection.Name} vs {mongoCollection.Name}");
            }
            
            if (postgresCollection.Path != mongoCollection.Path)
            {
                result.AddError($"Collection path mismatch: {postgresCollection.Path} vs {mongoCollection.Path}");
            }
        }
    }
}
```

### **Performance Validation**
```csharp
public class PerformanceValidator
{
    public async Task<PerformanceResult> ValidatePerformanceAsync()
    {
        var result = new PerformanceResult();
        
        // Test API response times
        var apiResponseTime = await TestAPIResponseTimeAsync();
        result.APIResponseTime = apiResponseTime;
        
        // Test database query performance
        var dbQueryTime = await TestDatabaseQueryTimeAsync();
        result.DatabaseQueryTime = dbQueryTime;
        
        // Test search performance
        var searchTime = await TestSearchPerformanceAsync();
        result.SearchTime = searchTime;
        
        return result;
    }
    
    private async Task<TimeSpan> TestAPIResponseTimeAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:7001/api/collections");
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
    
    private async Task<TimeSpan> TestDatabaseQueryTimeAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var collections = await _mongoRepo.GetAllCollectionsAsync();
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
    
    private async Task<TimeSpan> TestSearchPerformanceAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var results = await _mongoRepo.SearchCollectionsAsync("test");
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}
```

## 🎯 Success Criteria

### **Migration Success Criteria**
- [ ] All data migrated successfully
- [ ] No data loss or corruption
- [ ] All services running
- [ ] Performance improved or maintained
- [ ] All tests passing
- [ ] Monitoring working
- [ ] Documentation updated

### **Performance Criteria**
- [ ] API response time < 200ms
- [ ] Database query time < 100ms
- [ ] Search performance < 500ms
- [ ] Memory usage < 80%
- [ ] CPU usage < 70%

### **Quality Criteria**
- [ ] Code coverage > 90%
- [ ] All integration tests passing
- [ ] Security scan passed
- [ ] Performance tests passed
- [ ] User acceptance tests passed

## 📝 Conclusion

Migration guide này cung cấp comprehensive approach để migrate ImageViewer Platform từ thiết kế cũ sang thiết kế mới với:

1. **Data Migration**: PostgreSQL → MongoDB với 57 collections
2. **Code Migration**: EF Core → MongoDB Driver
3. **Infrastructure Migration**: Add RabbitMQ, Redis, monitoring
4. **Feature Migration**: Enable 56 feature categories
5. **Validation**: Comprehensive validation và testing
6. **Rollback**: Complete rollback procedures

Migration này đảm bảo platform được upgrade successfully với minimal downtime và maximum data integrity.

---

**Created**: 2025-01-04
**Status**: Ready for Implementation
**Priority**: High
