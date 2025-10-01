# Design Patterns - Image Viewer System

## Tổng quan Design Patterns

### Patterns được sử dụng trong hệ thống
1. **Clean Architecture** - Tổng thể architecture
2. **CQRS (Command Query Responsibility Segregation)** - Tách biệt commands và queries
3. **Repository Pattern** - Data access abstraction
4. **Unit of Work** - Transaction management
5. **Domain Events** - Loose coupling giữa aggregates
6. **Factory Pattern** - Object creation
7. **Strategy Pattern** - Algorithm selection
8. **Observer Pattern** - Event handling
9. **Singleton Pattern** - Service instances
10. **Builder Pattern** - Complex object construction

## 1. Clean Architecture

### Architecture Layers
```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
├─────────────────────────────────────────────────────────────┤
│  Controllers, API Endpoints, DTOs                          │
│  - Input validation                                        │
│  - Request/Response mapping                                 │
│  - Authentication/Authorization                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Use Cases, Application Services, CQRS                     │
│  - Business workflows                                       │
│  - Command/Query handlers                                   │
│  - Application-specific business rules                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                            │
├─────────────────────────────────────────────────────────────┤
│  Entities, Value Objects, Domain Services                  │
│  - Core business logic                                      │
│  - Domain rules and constraints                             │
│  - Domain events                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                     │
├─────────────────────────────────────────────────────────────┤
│  Repositories, External Services, Data Access             │
│  - Database implementations                                 │
│  - File system operations                                   │
│  - External API integrations                                │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Direction
- **Inward Dependencies**: Outer layers depend on inner layers
- **No Outward Dependencies**: Inner layers don't know about outer layers
- **Interface Segregation**: Use interfaces for dependencies

### Example Implementation
```csharp
// Domain Layer - Core business logic
public class Collection : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Path { get; private set; }
    public CollectionType Type { get; private set; }
    
    // Domain methods
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name cannot be empty");
        
        Name = name;
        AddDomainEvent(new CollectionNameUpdatedEvent(Id, Name));
    }
}

// Application Layer - Use cases
public class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICollectionDomainService _collectionDomainService;
    
    public async Task<CollectionDto> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionDomainService.CreateCollectionAsync(
            request.Name, request.Path, request.Type, request.Settings);
        
        await _collectionRepository.AddAsync(collection);
        
        return collection.ToDto();
    }
}

// Infrastructure Layer - Data access
public class CollectionRepository : ICollectionRepository
{
    private readonly ImageViewerDbContext _context;
    
    public async Task<Collection> GetByIdAsync(Guid id)
    {
        return await _context.Collections
            .Include(c => c.Images)
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
```

## 2. CQRS (Command Query Responsibility Segregation)

### Command Side
```csharp
// Commands - Write operations
public class CreateCollectionCommand : IRequest<CollectionDto>
{
    public string Name { get; set; }
    public string Path { get; set; }
    public CollectionType Type { get; set; }
    public CollectionSettings Settings { get; set; }
}

public class UpdateCollectionCommand : IRequest<CollectionDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public CollectionSettings Settings { get; set; }
}

public class DeleteCollectionCommand : IRequest
{
    public Guid Id { get; set; }
    public bool Permanent { get; set; }
}

// Command Handlers
public class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICollectionDomainService _collectionDomainService;
    private readonly IBackgroundJobService _backgroundJobService;
    
    public async Task<CollectionDto> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        // Validate and create collection
        var collection = await _collectionDomainService.CreateCollectionAsync(
            request.Name, request.Path, request.Type, request.Settings);
        
        // Save to database
        await _collectionRepository.AddAsync(collection);
        
        // Start background scan
        await _backgroundJobService.EnqueueAsync(new ScanCollectionJob(collection.Id));
        
        return collection.ToDto();
    }
}
```

### Query Side
```csharp
// Queries - Read operations
public class GetCollectionsQuery : IRequest<PagedResult<CollectionDto>>
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public string SortBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public string Filter { get; set; }
    public CollectionType? Type { get; set; }
}

public class GetCollectionQuery : IRequest<CollectionDto>
{
    public Guid Id { get; set; }
    public bool IncludeImages { get; set; } = false;
    public bool IncludeStatistics { get; set; } = true;
}

// Query Handlers
public class GetCollectionsQueryHandler : IRequestHandler<GetCollectionsQuery, PagedResult<CollectionDto>>
{
    private readonly ICollectionRepository _collectionRepository;
    
    public async Task<PagedResult<CollectionDto>> Handle(GetCollectionsQuery request, CancellationToken cancellationToken)
    {
        var query = _collectionRepository.GetQueryable()
            .Where(c => !c.IsDeleted);
        
        // Apply filters
        if (!string.IsNullOrEmpty(request.Filter))
        {
            query = query.Where(c => c.Name.Contains(request.Filter));
        }
        
        if (request.Type.HasValue)
        {
            query = query.Where(c => c.Type == request.Type.Value);
        }
        
        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "name" => request.SortOrder == "desc" ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "createdat" => request.SortOrder == "desc" ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Name)
        };
        
        // Get total count
        var total = await query.CountAsync(cancellationToken);
        
        // Apply pagination
        var collections = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(c => new CollectionDto
            {
                Id = c.Id,
                Name = c.Name,
                Path = c.Path,
                Type = c.Type.ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        
        return new PagedResult<CollectionDto>(collections, total, request.Page, request.Limit);
    }
}
```

## 3. Repository Pattern

### Repository Interface
```csharp
public interface ICollectionRepository
{
    Task<Collection> GetByIdAsync(Guid id);
    Task<Collection> GetByNameAsync(string name);
    Task<Collection> GetWithImagesAsync(Guid id);
    Task<PagedResult<Collection>> GetPagedAsync(GetCollectionsQuery query);
    Task<IEnumerable<Collection>> GetByTagAsync(string tagName);
    Task AddAsync(Collection collection);
    Task UpdateAsync(Collection collection);
    Task DeleteAsync(Collection collection);
    IQueryable<Collection> GetQueryable();
}

public interface IImageRepository
{
    Task<Image> GetByIdAsync(Guid id);
    Task<Image> GetWithMetadataAsync(Guid id);
    Task<PagedResult<Image>> GetPagedAsync(GetImagesQuery query);
    Task<IEnumerable<Image>> GetByCollectionIdAsync(Guid collectionId);
    Task<IEnumerable<Image>> GetByTagAsync(string tagName);
    Task<long> GetTotalSizeAsync(Guid collectionId);
    Task AddAsync(Image image);
    Task UpdateAsync(Image image);
    Task DeleteAsync(Image image);
    IQueryable<Image> GetQueryable();
}
```

### Repository Implementation
```csharp
public class CollectionRepository : ICollectionRepository
{
    private readonly ImageViewerDbContext _context;
    private readonly ILogger<CollectionRepository> _logger;
    
    public CollectionRepository(ImageViewerDbContext context, ILogger<CollectionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<Collection> GetByIdAsync(Guid id)
    {
        return await _context.Collections
            .Include(c => c.Images)
            .Include(c => c.Tags)
            .Include(c => c.Statistics)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }
    
    public async Task<Collection> GetByNameAsync(string name)
    {
        return await _context.Collections
            .FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted);
    }
    
    public async Task<Collection> GetWithImagesAsync(Guid id)
    {
        return await _context.Collections
            .Include(c => c.Images.Where(i => !i.IsDeleted))
            .Include(c => c.Tags)
            .Include(c => c.Statistics)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }
    
    public async Task<PagedResult<Collection>> GetPagedAsync(GetCollectionsQuery query)
    {
        var queryable = _context.Collections
            .Where(c => !c.IsDeleted);
        
        // Apply filters
        if (!string.IsNullOrEmpty(query.Filter))
        {
            queryable = queryable.Where(c => c.Name.Contains(query.Filter));
        }
        
        if (query.Type.HasValue)
        {
            queryable = queryable.Where(c => c.Type == query.Type.Value);
        }
        
        // Apply sorting
        queryable = query.SortBy.ToLower() switch
        {
            "name" => query.SortOrder == "desc" ? queryable.OrderByDescending(c => c.Name) : queryable.OrderBy(c => c.Name),
            "createdat" => query.SortOrder == "desc" ? queryable.OrderByDescending(c => c.CreatedAt) : queryable.OrderBy(c => c.CreatedAt),
            _ => queryable.OrderBy(c => c.Name)
        };
        
        var total = await queryable.CountAsync();
        var collections = await queryable
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync();
        
        return new PagedResult<Collection>(collections, total, query.Page, query.Limit);
    }
    
    public async Task AddAsync(Collection collection)
    {
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Collection {CollectionId} added", collection.Id);
    }
    
    public async Task UpdateAsync(Collection collection)
    {
        _context.Collections.Update(collection);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Collection {CollectionId} updated", collection.Id);
    }
    
    public async Task DeleteAsync(Collection collection)
    {
        if (collection.IsDeleted)
            return;
        
        collection.MarkAsDeleted();
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Collection {CollectionId} deleted", collection.Id);
    }
    
    public IQueryable<Collection> GetQueryable()
    {
        return _context.Collections.AsQueryable();
    }
}
```

## 4. Unit of Work Pattern

### Unit of Work Interface
```csharp
public interface IUnitOfWork : IDisposable
{
    ICollectionRepository Collections { get; }
    IImageRepository Images { get; }
    ICacheInfoRepository CacheInfos { get; }
    ICollectionTagRepository CollectionTags { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### Unit of Work Implementation
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ImageViewerDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction _transaction;
    
    public UnitOfWork(ImageViewerDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
        
        Collections = new CollectionRepository(_context, _logger);
        Images = new ImageRepository(_context, _logger);
        CacheInfos = new CacheInfoRepository(_context, _logger);
        CollectionTags = new CollectionTagRepository(_context, _logger);
    }
    
    public ICollectionRepository Collections { get; }
    public IImageRepository Images { get; }
    public ICacheInfoRepository CacheInfos { get; }
    public ICollectionTagRepository CollectionTags { get; }
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction already started");
        
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Transaction started");
    }
    
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to commit");
        
        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to rollback");
        
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

## 5. Domain Events Pattern

### Domain Event Interface
```csharp
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}
```

### Domain Event Base Class
```csharp
public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public string EventType { get; private set; }
    
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name;
    }
}
```

### Domain Events
```csharp
public class CollectionCreatedEvent : DomainEvent
{
    public Guid CollectionId { get; private set; }
    public string Name { get; private set; }
    public string Path { get; private set; }
    public CollectionType Type { get; private set; }
    
    public CollectionCreatedEvent(Guid collectionId, string name, string path, CollectionType type)
    {
        CollectionId = collectionId;
        Name = name;
        Path = path;
        Type = type;
    }
}

public class ImageAddedToCollectionEvent : DomainEvent
{
    public Guid CollectionId { get; private set; }
    public Guid ImageId { get; private set; }
    public string FileName { get; private set; }
    
    public ImageAddedToCollectionEvent(Guid collectionId, Guid imageId, string fileName)
    {
        CollectionId = collectionId;
        ImageId = imageId;
        FileName = fileName;
    }
}

public class CacheGeneratedEvent : DomainEvent
{
    public Guid ImageId { get; private set; }
    public string CachePath { get; private set; }
    public long CacheSize { get; private set; }
    
    public CacheGeneratedEvent(Guid imageId, string cachePath, long cacheSize)
    {
        ImageId = imageId;
        CachePath = cachePath;
        CacheSize = cacheSize;
    }
}
```

### Domain Event Handlers
```csharp
public class CollectionCreatedEventHandler : INotificationHandler<CollectionCreatedEvent>
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<CollectionCreatedEventHandler> _logger;
    
    public CollectionCreatedEventHandler(
        IBackgroundJobService backgroundJobService,
        ILogger<CollectionCreatedEventHandler> logger)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }
    
    public async Task Handle(CollectionCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Collection {CollectionId} created, starting background scan", notification.CollectionId);
        
        // Start background scan
        await _backgroundJobService.EnqueueAsync(new ScanCollectionJob(notification.CollectionId));
    }
}

public class ImageAddedToCollectionEventHandler : INotificationHandler<ImageAddedToCollectionEvent>
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<ImageAddedToCollectionEventHandler> _logger;
    
    public ImageAddedToCollectionEventHandler(
        IBackgroundJobService backgroundJobService,
        ILogger<ImageAddedToCollectionEventHandler> logger)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }
    
    public async Task Handle(ImageAddedToCollectionEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Image {ImageId} added to collection {CollectionId}", 
            notification.ImageId, notification.CollectionId);
        
        // Start thumbnail generation
        await _backgroundJobService.EnqueueAsync(new GenerateThumbnailJob(notification.ImageId));
    }
}
```

## 6. Factory Pattern

### Image Processor Factory
```csharp
public interface IImageProcessorFactory
{
    IImageProcessor CreateProcessor(ImageFormat format);
    IImageProcessor CreateProcessor(string fileExtension);
}

public class ImageProcessorFactory : IImageProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageProcessorFactory> _logger;
    
    public ImageProcessorFactory(IServiceProvider serviceProvider, ILogger<ImageProcessorFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public IImageProcessor CreateProcessor(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Jpeg => _serviceProvider.GetRequiredService<JpegImageProcessor>(),
            ImageFormat.Png => _serviceProvider.GetRequiredService<PngImageProcessor>(),
            ImageFormat.WebP => _serviceProvider.GetRequiredService<WebPImageProcessor>(),
            ImageFormat.Gif => _serviceProvider.GetRequiredService<GifImageProcessor>(),
            _ => _serviceProvider.GetRequiredService<DefaultImageProcessor>()
        };
    }
    
    public IImageProcessor CreateProcessor(string fileExtension)
    {
        var format = fileExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".webp" => ImageFormat.WebP,
            ".gif" => ImageFormat.Gif,
            _ => ImageFormat.Unknown
        };
        
        return CreateProcessor(format);
    }
}
```

### Cache Strategy Factory
```csharp
public interface ICacheStrategyFactory
{
    ICacheStrategy CreateStrategy(CacheType type);
}

public class CacheStrategyFactory : ICacheStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public CacheStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public ICacheStrategy CreateStrategy(CacheType type)
    {
        return type switch
        {
            CacheType.Memory => _serviceProvider.GetRequiredService<MemoryCacheStrategy>(),
            CacheType.Redis => _serviceProvider.GetRequiredService<RedisCacheStrategy>(),
            CacheType.File => _serviceProvider.GetRequiredService<FileCacheStrategy>(),
            CacheType.Distributed => _serviceProvider.GetRequiredService<DistributedCacheStrategy>(),
            _ => _serviceProvider.GetRequiredService<DefaultCacheStrategy>()
        };
    }
}
```

## 7. Strategy Pattern

### Image Processing Strategy
```csharp
public interface IImageProcessingStrategy
{
    Task<byte[]> ProcessAsync(byte[] imageData, ImageProcessingOptions options);
    Task<ImageMetadata> ExtractMetadataAsync(byte[] imageData);
    bool CanProcess(string fileExtension);
}

public class JpegProcessingStrategy : IImageProcessingStrategy
{
    private readonly ILogger<JpegProcessingStrategy> _logger;
    
    public JpegProcessingStrategy(ILogger<JpegProcessingStrategy> logger)
    {
        _logger = logger;
    }
    
    public async Task<byte[]> ProcessAsync(byte[] imageData, ImageProcessingOptions options)
    {
        using var image = SKImage.FromEncodedData(imageData);
        using var bitmap = SKBitmap.FromImage(image);
        
        // Apply JPEG-specific processing
        var processedBitmap = ApplyJpegProcessing(bitmap, options);
        
        // Encode as JPEG
        using var encodedImage = processedBitmap.Encode(SKEncodedImageFormat.Jpeg, options.Quality);
        return encodedImage.ToArray();
    }
    
    public async Task<ImageMetadata> ExtractMetadataAsync(byte[] imageData)
    {
        using var image = SKImage.FromEncodedData(imageData);
        
        return new ImageMetadata
        {
            Width = image.Width,
            Height = image.Height,
            Format = "jpeg",
            ColorSpace = image.ColorSpace?.ToString(),
            BitDepth = 8,
            HasTransparency = false
        };
    }
    
    public bool CanProcess(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() is ".jpg" or ".jpeg";
    }
    
    private SKBitmap ApplyJpegProcessing(SKBitmap bitmap, ImageProcessingOptions options)
    {
        // Apply JPEG-specific processing logic
        return bitmap;
    }
}
```

### Cache Strategy
```csharp
public interface ICacheStrategy
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}

public class MemoryCacheStrategy : ICacheStrategy
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheStrategy> _logger;
    
    public MemoryCacheStrategy(IMemoryCache memoryCache, ILogger<MemoryCacheStrategy> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    public Task<T> GetAsync<T>(string key)
    {
        _memoryCache.TryGetValue(key, out T value);
        return Task.FromResult(value);
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }
        
        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }
    
    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
    
    public Task<bool> ExistsAsync(string key)
    {
        var exists = _memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }
}
```

## 8. Observer Pattern

### Event Bus
```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IDomainEvent;
    void Subscribe<T>(Func<T, Task> handler) where T : IDomainEvent;
    void Unsubscribe<T>(Func<T, Task> handler) where T : IDomainEvent;
}

public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers;
    
    public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _handlers = new ConcurrentDictionary<Type, List<Func<object, Task>>>();
    }
    
    public async Task PublishAsync<T>(T @event) where T : IDomainEvent
    {
        var eventType = typeof(T);
        
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            var tasks = handlers.Select(handler => handler(@event));
            await Task.WhenAll(tasks);
        }
        
        _logger.LogDebug("Published event {EventType} with ID {EventId}", eventType.Name, @event.Id);
    }
    
    public void Subscribe<T>(Func<T, Task> handler) where T : IDomainEvent
    {
        var eventType = typeof(T);
        var handlers = _handlers.GetOrAdd(eventType, _ => new List<Func<object, Task>>());
        
        handlers.Add(@event => handler((T)@event));
        
        _logger.LogDebug("Subscribed handler for event {EventType}", eventType.Name);
    }
    
    public void Unsubscribe<T>(Func<T, Task> handler) where T : IDomainEvent
    {
        var eventType = typeof(T);
        
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(@event => handler((T)@event));
        }
        
        _logger.LogDebug("Unsubscribed handler for event {EventType}", eventType.Name);
    }
}
```

## 9. Singleton Pattern

### Configuration Service
```csharp
public interface IConfigurationService
{
    string GetValue(string key);
    T GetValue<T>(string key);
    void SetValue(string key, object value);
}

public class ConfigurationService : IConfigurationService
{
    private static readonly Lazy<ConfigurationService> _instance = new(() => new ConfigurationService());
    private readonly ConcurrentDictionary<string, object> _configurations;
    
    private ConfigurationService()
    {
        _configurations = new ConcurrentDictionary<string, object>();
    }
    
    public static ConfigurationService Instance => _instance.Value;
    
    public string GetValue(string key)
    {
        return _configurations.TryGetValue(key, out var value) ? value.ToString() : null;
    }
    
    public T GetValue<T>(string key)
    {
        if (_configurations.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        
        return default(T);
    }
    
    public void SetValue(string key, object value)
    {
        _configurations.AddOrUpdate(key, value, (_, _) => value);
    }
}
```

## 10. Builder Pattern

### Image Processing Options Builder
```csharp
public class ImageProcessingOptionsBuilder
{
    private ImageProcessingOptions _options;
    
    public ImageProcessingOptionsBuilder()
    {
        _options = new ImageProcessingOptions();
    }
    
    public ImageProcessingOptionsBuilder WithSize(int width, int height)
    {
        _options.Width = width;
        _options.Height = height;
        return this;
    }
    
    public ImageProcessingOptionsBuilder WithQuality(int quality)
    {
        _options.Quality = Math.Max(1, Math.Min(100, quality));
        return this;
    }
    
    public ImageProcessingOptionsBuilder WithFormat(ImageFormat format)
    {
        _options.Format = format;
        return this;
    }
    
    public ImageProcessingOptionsBuilder WithFitMode(FitMode fitMode)
    {
        _options.FitMode = fitMode;
        return this;
    }
    
    public ImageProcessingOptionsBuilder WithWatermark(string watermarkText)
    {
        _options.WatermarkText = watermarkText;
        return this;
    }
    
    public ImageProcessingOptionsBuilder WithFilters(List<ImageFilter> filters)
    {
        _options.Filters = filters ?? new List<ImageFilter>();
        return this;
    }
    
    public ImageProcessingOptionsBuilder WithCrop(CropOptions cropOptions)
    {
        _options.CropOptions = cropOptions;
        return this;
    }
    
    public ImageProcessingOptions Build()
    {
        return _options;
    }
}

// Usage
var options = new ImageProcessingOptionsBuilder()
    .WithSize(1920, 1080)
    .WithQuality(90)
    .WithFormat(ImageFormat.Jpeg)
    .WithFitMode(FitMode.Contain)
    .WithWatermark("Sample Watermark")
    .WithFilters(new List<ImageFilter> { ImageFilter.Sharpen, ImageFilter.Contrast })
    .Build();
```

### Collection Builder
```csharp
public class CollectionBuilder
{
    private Collection _collection;
    
    public CollectionBuilder()
    {
        _collection = new Collection();
    }
    
    public CollectionBuilder WithName(string name)
    {
        _collection.UpdateName(name);
        return this;
    }
    
    public CollectionBuilder WithPath(string path)
    {
        _collection.UpdatePath(path);
        return this;
    }
    
    public CollectionBuilder WithType(CollectionType type)
    {
        _collection.UpdateType(type);
        return this;
    }
    
    public CollectionBuilder WithSettings(CollectionSettings settings)
    {
        _collection.UpdateSettings(settings);
        return this;
    }
    
    public CollectionBuilder WithTag(string tagName)
    {
        _collection.AddTag(tagName, "system");
        return this;
    }
    
    public CollectionBuilder WithTags(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            _collection.AddTag(tag, "system");
        }
        return this;
    }
    
    public CollectionBuilder WithAutoScan(bool autoScan = true)
    {
        var settings = _collection.Settings ?? new CollectionSettings();
        settings.AutoScan = autoScan;
        _collection.UpdateSettings(settings);
        return this;
    }
    
    public CollectionBuilder WithCacheEnabled(bool cacheEnabled = true)
    {
        var settings = _collection.Settings ?? new CollectionSettings();
        settings.CacheEnabled = cacheEnabled;
        _collection.UpdateSettings(settings);
        return this;
    }
    
    public Collection Build()
    {
        return _collection;
    }
}

// Usage
var collection = new CollectionBuilder()
    .WithName("My Manga Collection")
    .WithPath("D:\\Manga\\Collection1")
    .WithType(CollectionType.Folder)
    .WithTags(new[] { "manga", "comics", "japanese" })
    .WithAutoScan(true)
    .WithCacheEnabled(true)
    .Build();
```

## Conclusion

Design patterns được sử dụng trong hệ thống để:

1. **Maintainability**: Code dễ maintain và extend
2. **Testability**: Dễ dàng unit test và integration test
3. **Scalability**: Có thể scale từng component độc lập
4. **Flexibility**: Dễ dàng thay đổi implementation
5. **Reusability**: Code có thể reuse được
6. **Separation of Concerns**: Mỗi pattern có responsibility rõ ràng

Việc sử dụng các patterns này đảm bảo hệ thống có architecture tốt và dễ phát triển.
