# Image Viewer System - .NET 8 Architecture Design

## Tổng quan kiến trúc mới

### Kiến trúc tổng thể
```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
├─────────────────────────────────────────────────────────────┤
│  Blazor Server/WebAssembly  │  Progressive Web App        │
│  - Image Grid Component     │  - Offline Support          │
│  - Image Viewer Component   │  - Push Notifications       │
│  - Collection Management    │  - Mobile Optimization      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway Layer                       │
├─────────────────────────────────────────────────────────────┤
│  ASP.NET Core Web API                                      │
│  - Authentication/Authorization                            │
│  - Rate Limiting                                           │
│  - Request/Response Logging                                │
│  - API Versioning                                          │
│  - Swagger/OpenAPI Documentation                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                       │
├─────────────────────────────────────────────────────────────┤
│  CQRS + MediatR                                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Commands  │  │   Queries   │  │   Events    │        │
│  │             │  │             │  │             │        │
│  │ - Create    │  │ - Get       │  │ - Created   │        │
│  │ - Update    │  │ - Search    │  │ - Updated   │        │
│  │ - Delete    │  │ - List      │  │ - Deleted   │        │
│  │ - Process   │  │ - Count     │  │ - Processed │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                            │
├─────────────────────────────────────────────────────────────┤
│  Domain Models & Business Logic                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Collections │  │   Images    │  │    Cache    │        │
│  │             │  │             │  │             │        │
│  │ - Entity    │  │ - Entity    │  │ - Entity    │        │
│  │ - Services  │  │ - Services  │  │ - Services  │        │
│  │ - Rules     │  │ - Rules     │  │ - Rules     │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                     │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Database  │  │    Cache    │  │   Storage   │        │
│  │             │  │             │  │             │        │
│  │ - EF Core   │  │ - Redis     │  │ - File      │        │
│  │ - SQL Server│  │ - Memory    │  │ - Blob      │        │
│  │ - Migrations│  │ - Distributed│  │ - CDN      │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Background│  │   External │  │   Logging   │        │
│  │   Services  │  │   Services  │  │             │        │
│  │             │  │             │  │             │        │
│  │ - Hangfire  │  │ - Image     │  │ - Serilog   │        │
│  │ - SignalR   │  │   Processing│  │ - ELK Stack │        │
│  │ - Health    │  │ - AI/ML     │  │ - Metrics   │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Domain Models

### Collection Entity
```csharp
public class Collection : BaseEntity
{
    public string Name { get; set; }
    public string Path { get; set; }
    public CollectionType Type { get; set; }
    public CollectionSettings Settings { get; set; }
    public CollectionStatistics Statistics { get; set; }
    public List<Image> Images { get; set; }
    public List<CollectionTag> Tags { get; set; }
    public CacheFolder CacheFolder { get; set; }
    
    // Domain methods
    public bool NeedsScan() => Images.Count == 0 || !Settings.LastScanned.HasValue;
    public void UpdateStatistics(CollectionStatistics stats) => Statistics = stats;
    public void AddTag(string tag, string addedBy) => Tags.Add(new CollectionTag(tag, addedBy));
}

public enum CollectionType
{
    Folder,
    Zip,
    SevenZip,
    Rar,
    Tar
}
```

### Image Entity
```csharp
public class Image : BaseEntity
{
    public string CollectionId { get; set; }
    public string Filename { get; set; }
    public string RelativePath { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public ImageMetadata Metadata { get; set; }
    public CacheInfo CacheInfo { get; set; }
    public Collection Collection { get; set; }
    
    // Domain methods
    public bool IsCached() => CacheInfo != null && CacheInfo.IsValid();
    public string GetThumbnailPath() => CacheInfo?.ThumbnailPath;
    public void UpdateCache(CacheInfo cacheInfo) => CacheInfo = cacheInfo;
}

public class ImageMetadata
{
    public string Format { get; set; }
    public int Quality { get; set; }
    public string ColorSpace { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
```

### Cache Entity
```csharp
public class CacheInfo : BaseEntity
{
    public string ImageId { get; set; }
    public string CachePath { get; set; }
    public string ThumbnailPath { get; set; }
    public long CacheSize { get; set; }
    public int Quality { get; set; }
    public string Format { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    
    // Domain methods
    public bool IsValid() => DateTime.UtcNow < ExpiresAt;
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
    public void ExtendExpiry(TimeSpan duration) => ExpiresAt = DateTime.UtcNow.Add(duration);
}

public class CacheFolder : BaseEntity
{
    public string Name { get; set; }
    public string Path { get; set; }
    public int Priority { get; set; }
    public long MaxSize { get; set; }
    public long CurrentSize { get; set; }
    public int FileCount { get; set; }
    public bool IsActive { get; set; }
    public List<Collection> Collections { get; set; }
    
    // Domain methods
    public bool HasSpace(long requiredSize) => CurrentSize + requiredSize <= MaxSize;
    public void UpdateUsage(long sizeDelta, int fileCountDelta)
    {
        CurrentSize += sizeDelta;
        FileCount += fileCountDelta;
    }
}
```

## Application Services

### Collection Service
```csharp
public interface ICollectionService
{
    Task<Collection> GetByIdAsync(string id);
    Task<PagedResult<Collection>> GetPagedAsync(GetCollectionsQuery query);
    Task<Collection> CreateAsync(CreateCollectionCommand command);
    Task<Collection> UpdateAsync(UpdateCollectionCommand command);
    Task DeleteAsync(DeleteCollectionCommand command);
    Task<Collection> ScanAsync(ScanCollectionCommand command);
    Task<CollectionStatistics> GetStatisticsAsync(string id);
}

public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _repository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<CollectionService> _logger;
    
    public async Task<Collection> CreateAsync(CreateCollectionCommand command)
    {
        var collection = new Collection(command.Name, command.Path, command.Type);
        
        // Validate collection path
        if (!await _imageProcessingService.ValidatePathAsync(command.Path, command.Type))
            throw new InvalidOperationException("Invalid collection path");
        
        // Create collection
        await _repository.AddAsync(collection);
        
        // Start background scan
        await _backgroundJobService.EnqueueAsync(new ScanCollectionJob(collection.Id));
        
        return collection;
    }
}
```

### Image Processing Service
```csharp
public interface IImageProcessingService
{
    Task<bool> ValidatePathAsync(string path, CollectionType type);
    Task<List<ImageInfo>> ScanCollectionAsync(string collectionId, string path, CollectionType type);
    Task<byte[]> ProcessImageAsync(ImageProcessingRequest request);
    Task<string> GenerateThumbnailAsync(ThumbnailRequest request);
    Task<ImageMetadata> GetMetadataAsync(string imagePath);
}

public class ImageProcessingService : IImageProcessingService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IImageCacheService _cacheService;
    private readonly ILogger<ImageProcessingService> _logger;
    
    public async Task<byte[]> ProcessImageAsync(ImageProcessingRequest request)
    {
        using var image = SKImage.FromEncodedData(request.ImageData);
        using var bitmap = SKBitmap.FromImage(image);
        
        // Apply transformations
        var processedBitmap = ApplyTransformations(bitmap, request.Transformations);
        
        // Encode to requested format
        var encodedData = EncodeImage(processedBitmap, request.OutputFormat, request.Quality);
        
        return encodedData;
    }
    
    private SKBitmap ApplyTransformations(SKBitmap bitmap, List<ImageTransformation> transformations)
    {
        // Apply resize, crop, filters, etc.
        return bitmap;
    }
}
```

### Cache Service
```csharp
public interface IImageCacheService
{
    Task<CacheInfo> GetCacheAsync(string imageId, CacheOptions options);
    Task<CacheInfo> SetCacheAsync(string imageId, byte[] imageData, CacheOptions options);
    Task<bool> DeleteCacheAsync(string imageId);
    Task<CacheStatistics> GetStatisticsAsync();
    Task CleanupExpiredAsync();
}

public class ImageCacheService : IImageCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IRedisCache _redisCache;
    private readonly IFileSystemService _fileSystemService;
    private readonly ICacheFolderService _cacheFolderService;
    
    public async Task<CacheInfo> GetCacheAsync(string imageId, CacheOptions options)
    {
        // Try memory cache first
        var memoryKey = $"image:{imageId}:{options.GetHashCode()}";
        if (_memoryCache.TryGetValue(memoryKey, out byte[] cachedData))
            return new CacheInfo { Data = cachedData, Source = CacheSource.Memory };
        
        // Try Redis cache
        var redisKey = $"image:{imageId}:{options.GetHashCode()}";
        var redisData = await _redisCache.GetAsync(redisKey);
        if (redisData != null)
        {
            _memoryCache.Set(memoryKey, redisData, TimeSpan.FromMinutes(5));
            return new CacheInfo { Data = redisData, Source = CacheSource.Redis };
        }
        
        // Try file cache
        var cacheFolder = await _cacheFolderService.GetCacheFolderAsync(imageId);
        var filePath = Path.Combine(cacheFolder.Path, $"{imageId}_{options.GetHashCode()}.{options.Format}");
        
        if (await _fileSystemService.ExistsAsync(filePath))
        {
            var fileData = await _fileSystemService.ReadAllBytesAsync(filePath);
            await _redisCache.SetAsync(redisKey, fileData, TimeSpan.FromHours(24));
            _memoryCache.Set(memoryKey, fileData, TimeSpan.FromMinutes(5));
            return new CacheInfo { Data = fileData, Source = CacheSource.File };
        }
        
        return null;
    }
}
```

## Infrastructure Services

### Database Context
```csharp
public class ImageViewerDbContext : DbContext
{
    public DbSet<Collection> Collections { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<CacheInfo> CacheInfos { get; set; }
    public DbSet<CacheFolder> CacheFolders { get; set; }
    public DbSet<CollectionTag> CollectionTags { get; set; }
    public DbSet<CollectionStatistics> CollectionStatistics { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasOne(e => e.CacheFolder).WithMany(cf => cf.Collections);
        });
        
        // Image configuration
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Filename).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RelativePath).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => new { e.CollectionId, e.Filename }).IsUnique();
            entity.HasOne(e => e.Collection).WithMany(c => c.Images);
        });
        
        // Cache configuration
        modelBuilder.Entity<CacheInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CachePath).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.ImageId);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
```

### Background Job Service
```csharp
public interface IBackgroundJobService
{
    Task<string> EnqueueAsync<T>(T job) where T : IBackgroundJob;
    Task<string> ScheduleAsync<T>(T job, TimeSpan delay) where T : IBackgroundJob;
    Task<string> ScheduleAsync<T>(T job, DateTimeOffset scheduleAt) where T : IBackgroundJob;
    Task<bool> DeleteAsync(string jobId);
    Task<JobStatus> GetStatusAsync(string jobId);
}

public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    
    public async Task<string> EnqueueAsync<T>(T job) where T : IBackgroundJob
    {
        return _backgroundJobClient.Enqueue<IBackgroundJobProcessor<T>>(processor => processor.ProcessAsync(job));
    }
}

// Background job processors
public class ScanCollectionJobProcessor : IBackgroundJobProcessor<ScanCollectionJob>
{
    private readonly ICollectionService _collectionService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<ScanCollectionJobProcessor> _logger;
    
    public async Task ProcessAsync(ScanCollectionJob job)
    {
        _logger.LogInformation("Starting collection scan for {CollectionId}", job.CollectionId);
        
        var collection = await _collectionService.GetByIdAsync(job.CollectionId);
        var images = await _imageProcessingService.ScanCollectionAsync(
            job.CollectionId, 
            collection.Path, 
            collection.Type
        );
        
        // Process images in batches
        var batchSize = 10;
        for (int i = 0; i < images.Count; i += batchSize)
        {
            var batch = images.Skip(i).Take(batchSize);
            await ProcessBatchAsync(batch);
        }
        
        _logger.LogInformation("Completed collection scan for {CollectionId}", job.CollectionId);
    }
}
```

## API Controllers

### Collections Controller
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CollectionsController> _logger;
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollections(
        [FromQuery] GetCollectionsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionDto>> GetCollection(string id)
    {
        var query = new GetCollectionQuery { Id = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<ActionResult<CollectionDto>> CreateCollection(
        [FromBody] CreateCollectionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCollection), new { id = result.Id }, result);
    }
    
    [HttpPost("{id}/scan")]
    public async Task<ActionResult> ScanCollection(string id)
    {
        var command = new ScanCollectionCommand { Id = id };
        await _mediator.Send(command);
        return Accepted();
    }
}
```

### Images Controller
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IImageProcessingService _imageProcessingService;
    
    [HttpGet("{collectionId}")]
    public async Task<ActionResult<PagedResult<ImageDto>>> GetImages(
        string collectionId, [FromQuery] GetImagesQuery query)
    {
        query.CollectionId = collectionId;
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpGet("{collectionId}/{imageId}/file")]
    public async Task<ActionResult> GetImageFile(
        string collectionId, string imageId, [FromQuery] GetImageFileQuery query)
    {
        query.CollectionId = collectionId;
        query.ImageId = imageId;
        var result = await _mediator.Send(query);
        
        return File(result.Data, result.ContentType, result.Filename);
    }
    
    [HttpGet("{collectionId}/{imageId}/thumbnail")]
    public async Task<ActionResult> GetThumbnail(
        string collectionId, string imageId, [FromQuery] GetThumbnailQuery query)
    {
        query.CollectionId = collectionId;
        query.ImageId = imageId;
        var result = await _mediator.Send(query);
        
        return File(result.Data, "image/jpeg", $"{imageId}_thumb.jpg");
    }
}
```

## Configuration & Startup

### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ImageViewerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddMemoryCache();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Add custom services
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
builder.Services.AddScoped<IImageCacheService, ImageCacheService>();
builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Performance Optimizations

### 1. Database Optimizations
- **Connection Pooling**: Configure EF Core connection pooling
- **Query Optimization**: Use compiled queries for frequently used queries
- **Indexing Strategy**: Create appropriate indexes for common query patterns
- **Bulk Operations**: Use bulk insert/update for large datasets

### 2. Caching Strategy
- **Multi-level Caching**: Memory → Redis → File system
- **Cache Invalidation**: Smart cache invalidation based on data changes
- **Cache Warming**: Pre-populate cache for frequently accessed data
- **Cache Compression**: Compress cached data to reduce memory usage

### 3. Image Processing Optimizations
- **Parallel Processing**: Process multiple images concurrently
- **Memory Management**: Use object pooling for image processing
- **Format Optimization**: Choose optimal image formats for different use cases
- **Progressive Loading**: Implement progressive image loading

### 4. API Optimizations
- **Response Compression**: Compress API responses
- **Pagination**: Implement efficient pagination
- **Field Selection**: Allow clients to select specific fields
- **Rate Limiting**: Implement rate limiting to prevent abuse

## Security Considerations

### 1. Authentication & Authorization
- **JWT Tokens**: Use JWT for stateless authentication
- **Role-based Access**: Implement role-based access control
- **API Keys**: Support API key authentication for external services

### 2. Data Protection
- **Input Validation**: Validate all inputs
- **SQL Injection Prevention**: Use parameterized queries
- **File Upload Security**: Validate file types and sizes
- **Path Traversal Prevention**: Prevent directory traversal attacks

### 3. Infrastructure Security
- **HTTPS**: Enforce HTTPS for all communications
- **CORS**: Configure CORS properly
- **Security Headers**: Add security headers
- **Logging**: Implement comprehensive security logging

## Monitoring & Observability

### 1. Logging
- **Structured Logging**: Use structured logging with Serilog
- **Log Levels**: Appropriate log levels for different scenarios
- **Correlation IDs**: Use correlation IDs for request tracing
- **Sensitive Data**: Avoid logging sensitive data

### 2. Metrics
- **Application Metrics**: Track application performance metrics
- **Business Metrics**: Track business-specific metrics
- **Infrastructure Metrics**: Monitor infrastructure health
- **Custom Metrics**: Define custom metrics for specific use cases

### 3. Health Checks
- **Database Health**: Check database connectivity
- **Cache Health**: Check cache connectivity
- **External Services**: Check external service health
- **Custom Health Checks**: Implement custom health checks

## Deployment Strategy

### 1. Containerization
- **Docker**: Containerize the application
- **Multi-stage Builds**: Optimize Docker images
- **Health Checks**: Implement Docker health checks
- **Resource Limits**: Set appropriate resource limits

### 2. Orchestration
- **Kubernetes**: Use Kubernetes for orchestration
- **Helm Charts**: Use Helm for deployment management
- **Service Mesh**: Consider service mesh for microservices
- **Auto-scaling**: Implement horizontal pod autoscaling

### 3. CI/CD
- **GitHub Actions**: Use GitHub Actions for CI/CD
- **Automated Testing**: Implement comprehensive automated testing
- **Deployment Pipelines**: Create deployment pipelines
- **Rollback Strategy**: Implement rollback strategies

## Conclusion

Kiến trúc mới này sẽ giải quyết các vấn đề performance và logic không nhất quán trong hệ thống hiện tại. Việc sử dụng .NET 8 với Clean Architecture, CQRS, và các best practices sẽ giúp:

1. **Cải thiện Performance**: Async programming, efficient caching, optimized database queries
2. **Tăng Reliability**: Better error handling, comprehensive logging, health checks
3. **Dễ Maintain**: Clean architecture, separation of concerns, testable code
4. **Scalability**: Horizontal scaling, microservices architecture, cloud-native design
5. **Developer Experience**: Better tooling, debugging, and development experience

Kiến trúc này được thiết kế để có thể scale từ single instance đến distributed microservices architecture tùy theo nhu cầu.
