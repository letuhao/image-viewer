# Service Layers - Image Viewer System

## Tổng quan Service Architecture

### Service Layer Structure
```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
├─────────────────────────────────────────────────────────────┤
│  Controllers & API Endpoints                                │
│  - CollectionsController                                    │
│  - ImagesController                                         │
│  - CacheController                                          │
│  - JobsController                                           │
│  - StatisticsController                                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  CQRS + MediatR                                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Commands  │  │   Queries   │  │   Handlers   │        │
│  │             │  │             │  │             │        │
│  │ - Create    │  │ - Get       │  │ - Command    │        │
│  │ - Update    │  │ - Search    │  │   Handlers   │        │
│  │ - Delete    │  │ - List      │  │ - Query      │        │
│  │ - Process   │  │ - Count     │  │   Handlers   │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                            │
├─────────────────────────────────────────────────────────────┤
│  Domain Services & Business Logic                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Collections │  │   Images    │  │    Cache    │        │
│  │   Service   │  │   Service    │  │   Service   │        │
│  │             │  │             │  │             │        │
│  │ - Business  │  │ - Business  │  │ - Business  │        │
│  │   Rules     │  │   Rules     │  │   Rules     │        │
│  │ - Domain    │  │ - Domain    │  │ - Domain    │        │
│  │   Events    │  │   Events    │  │   Events    │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                     │
├─────────────────────────────────────────────────────────────┤
│  Repositories & External Services                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Repositories│  │   Cache     │  │   Storage   │        │
│  │             │  │   Services  │  │   Services  │        │
│  │ - Collection│  │ - Redis     │  │ - File      │        │
│  │ - Image     │  │ - Memory    │  │ - Blob      │        │
│  │ - Cache     │  │ - Distributed│  │ - CDN      │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Application Services

### 1. Collection Application Service

#### Commands
```csharp
// Create Collection Command
public class CreateCollectionCommand : IRequest<CollectionDto>
{
    public string Name { get; set; }
    public string Path { get; set; }
    public CollectionType Type { get; set; }
    public CollectionSettings Settings { get; set; }
    public List<string> Tags { get; set; }
}

// Update Collection Command
public class UpdateCollectionCommand : IRequest<CollectionDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public CollectionSettings Settings { get; set; }
}

// Delete Collection Command
public class DeleteCollectionCommand : IRequest
{
    public Guid Id { get; set; }
    public bool Permanent { get; set; } = false;
}

// Scan Collection Command
public class ScanCollectionCommand : IRequest<JobDto>
{
    public Guid Id { get; set; }
    public bool ForceRescan { get; set; } = false;
    public bool GenerateThumbnails { get; set; } = true;
    public bool GenerateCache { get; set; } = false;
}
```

#### Queries
```csharp
// Get Collections Query
public class GetCollectionsQuery : IRequest<PagedResult<CollectionDto>>
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public string SortBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public string Filter { get; set; }
    public CollectionType? Type { get; set; }
    public bool? HasImages { get; set; }
    public List<string> Tags { get; set; }
}

// Get Collection Query
public class GetCollectionQuery : IRequest<CollectionDto>
{
    public Guid Id { get; set; }
    public bool IncludeImages { get; set; } = false;
    public bool IncludeStatistics { get; set; } = true;
}

// Get Collection Statistics Query
public class GetCollectionStatisticsQuery : IRequest<CollectionStatisticsDto>
{
    public Guid Id { get; set; }
    public string Timeframe { get; set; } = "all"; // day, week, month, year, all
}
```

#### Command Handlers
```csharp
public class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICollectionDomainService _collectionDomainService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<CreateCollectionCommandHandler> _logger;
    
    public async Task<CollectionDto> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        // Validate collection
        var collection = await _collectionDomainService.CreateCollectionAsync(
            request.Name, 
            request.Path, 
            request.Type, 
            request.Settings
        );
        
        // Add tags
        foreach (var tag in request.Tags ?? new List<string>())
        {
            collection.AddTag(tag, "system");
        }
        
        // Save collection
        await _collectionRepository.AddAsync(collection);
        
        // Start background scan
        var jobId = await _backgroundJobService.EnqueueAsync(new ScanCollectionJob(collection.Id));
        
        _logger.LogInformation("Collection {CollectionId} created with job {JobId}", collection.Id, jobId);
        
        return collection.ToDto();
    }
}

public class ScanCollectionCommandHandler : IRequestHandler<ScanCollectionCommand, JobDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<ScanCollectionCommandHandler> _logger;
    
    public async Task<JobDto> Handle(ScanCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionRepository.GetByIdAsync(request.Id);
        if (collection == null)
            throw new NotFoundException($"Collection {request.Id} not found");
        
        var job = new ScanCollectionJob(collection.Id)
        {
            ForceRescan = request.ForceRescan,
            GenerateThumbnails = request.GenerateThumbnails,
            GenerateCache = request.GenerateCache
        };
        
        var jobId = await _backgroundJobService.EnqueueAsync(job);
        
        _logger.LogInformation("Collection scan job {JobId} queued for collection {CollectionId}", jobId, request.Id);
        
        return new JobDto
        {
            JobId = jobId,
            Status = "queued",
            EstimatedDuration = "5 minutes"
        };
    }
}
```

#### Query Handlers
```csharp
public class GetCollectionsQueryHandler : IRequestHandler<GetCollectionsQuery, PagedResult<CollectionDto>>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<GetCollectionsQueryHandler> _logger;
    
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
        
        if (request.HasImages.HasValue)
        {
            if (request.HasImages.Value)
            {
                query = query.Where(c => c.Images.Any());
            }
            else
            {
                query = query.Where(c => !c.Images.Any());
            }
        }
        
        if (request.Tags?.Any() == true)
        {
            query = query.Where(c => c.Tags.Any(t => request.Tags.Contains(t.Name)));
        }
        
        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "name" => request.SortOrder == "desc" ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "createdat" => request.SortOrder == "desc" ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            "updatedat" => request.SortOrder == "desc" ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt),
            "imagecount" => request.SortOrder == "desc" ? query.OrderByDescending(c => c.Images.Count) : query.OrderBy(c => c.Images.Count),
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
                Settings = c.Settings,
                Statistics = c.Statistics,
                Tags = c.Tags.Select(t => new TagDto
                {
                    Tag = t.Name,
                    Count = 1,
                    AddedBy = t.AddedBy,
                    AddedAt = t.AddedAt
                }).ToList(),
                CacheStatus = new CacheStatusDto
                {
                    HasCache = c.CacheInfo.Any(),
                    CachedImages = c.CacheInfo.Count(),
                    TotalImages = c.Images.Count(),
                    CachePercentage = c.Images.Count() > 0 ? (c.CacheInfo.Count() * 100) / c.Images.Count() : 0,
                    LastGenerated = c.CacheInfo.Max(ci => ci.CachedAt)
                },
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        
        return new PagedResult<CollectionDto>(collections, total, request.Page, request.Limit);
    }
}
```

### 2. Image Application Service

#### Commands
```csharp
// Process Image Command
public class ProcessImageCommand : IRequest<ImageDto>
{
    public Guid CollectionId { get; set; }
    public Guid ImageId { get; set; }
    public ImageProcessingOptions Options { get; set; }
}

// Generate Thumbnail Command
public class GenerateThumbnailCommand : IRequest<ThumbnailDto>
{
    public Guid CollectionId { get; set; }
    public Guid ImageId { get; set; }
    public ThumbnailOptions Options { get; set; }
}
```

#### Queries
```csharp
// Get Images Query
public class GetImagesQuery : IRequest<PagedResult<ImageDto>>
{
    public Guid CollectionId { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 50;
    public string SortBy { get; set; } = "filename";
    public string SortOrder { get; set; } = "asc";
    public string Search { get; set; }
    public string Format { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public int? MinHeight { get; set; }
    public int? MaxHeight { get; set; }
}

// Get Image File Query
public class GetImageFileQuery : IRequest<ImageFileDto>
{
    public Guid CollectionId { get; set; }
    public Guid ImageId { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Quality { get; set; } = 90;
    public string Format { get; set; } = "original";
    public string Fit { get; set; } = "contain";
}

// Get Thumbnail Query
public class GetThumbnailQuery : IRequest<ThumbnailDto>
{
    public Guid CollectionId { get; set; }
    public Guid ImageId { get; set; }
    public int? Width { get; set; } = 300;
    public int? Height { get; set; } = 300;
    public int? Quality { get; set; } = 80;
}
```

### 3. Cache Application Service

#### Commands
```csharp
// Generate Cache Command
public class GenerateCacheCommand : IRequest<JobDto>
{
    public List<Guid> CollectionIds { get; set; }
    public CacheOptions Options { get; set; }
}

// Clear Cache Command
public class ClearCacheCommand : IRequest<CacheClearResultDto>
{
    public Guid? CollectionId { get; set; }
    public string Type { get; set; } = "all"; // thumbnails, images, all
}
```

#### Queries
```csharp
// Get Cache Statistics Query
public class GetCacheStatisticsQuery : IRequest<CacheStatisticsDto>
{
    public Guid? CollectionId { get; set; }
    public string Timeframe { get; set; } = "all";
}
```

## Domain Services

### 1. Collection Domain Service
```csharp
public interface ICollectionDomainService
{
    Task<Collection> CreateCollectionAsync(string name, string path, CollectionType type, CollectionSettings settings);
    Task<bool> CanAddImageAsync(Guid collectionId, long imageSize);
    Task<Collection> GetCollectionWithImagesAsync(Guid collectionId);
    Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName);
    Task<CollectionStatistics> CalculateStatisticsAsync(Guid collectionId);
    Task<bool> ValidateCollectionPathAsync(string path, CollectionType type);
}

public class CollectionDomainService : ICollectionDomainService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IFileSystemService _fileSystemService;
    
    public async Task<Collection> CreateCollectionAsync(string name, string path, CollectionType type, CollectionSettings settings)
    {
        // Validate collection name uniqueness
        var existingCollection = await _collectionRepository.GetByNameAsync(name);
        if (existingCollection != null)
            throw new InvalidOperationException($"Collection with name '{name}' already exists");
        
        // Validate path
        if (!await ValidateCollectionPathAsync(path, type))
            throw new DirectoryNotFoundException($"Path '{path}' does not exist or is not accessible");
        
        // Create collection
        var collection = new Collection(name, path, type, settings);
        
        return collection;
    }
    
    public async Task<bool> ValidateCollectionPathAsync(string path, CollectionType type)
    {
        switch (type)
        {
            case CollectionType.Folder:
                return await _fileSystemService.DirectoryExistsAsync(path);
            case CollectionType.Zip:
            case CollectionType.SevenZip:
            case CollectionType.Rar:
            case CollectionType.Tar:
                return await _fileSystemService.FileExistsAsync(path);
            default:
                return false;
        }
    }
    
    public async Task<bool> CanAddImageAsync(Guid collectionId, long imageSize)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
            return false;
        
        // Check if collection has space for new image
        var currentSize = await _imageRepository.GetTotalSizeAsync(collectionId);
        var maxSize = collection.Settings.MaxCacheSize;
        
        return currentSize + imageSize <= maxSize;
    }
    
    public async Task<CollectionStatistics> CalculateStatisticsAsync(Guid collectionId)
    {
        var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
        
        var statistics = new CollectionStatistics
        {
            ImageCount = images.Count(),
            TotalSize = images.Sum(i => i.FileSize),
            ThumbnailCount = images.Count(i => i.CacheInfo?.HasThumbnail == true),
            CacheCount = images.Count(i => i.CacheInfo?.HasCache == true),
            LastScanned = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        return statistics;
    }
}
```

### 2. Image Domain Service
```csharp
public interface IImageDomainService
{
    Task<Image> CreateImageAsync(Guid collectionId, string filePath, ImageMetadata metadata);
    Task<Image> GetImageWithMetadataAsync(Guid imageId);
    Task<IEnumerable<Image>> GetImagesByTagAsync(string tagName);
    Task<ImageMetadata> ExtractMetadataAsync(string filePath);
    Task<bool> IsImageFileAsync(string filePath);
    Task<Image> ProcessImageAsync(Image image, ImageProcessingOptions options);
}

public class ImageDomainService : IImageDomainService
{
    private readonly IImageRepository _imageRepository;
    private readonly IMetadataExtractor _metadataExtractor;
    private readonly IImageProcessor _imageProcessor;
    
    public async Task<Image> CreateImageAsync(Guid collectionId, string filePath, ImageMetadata metadata)
    {
        var fileName = Path.GetFileName(filePath);
        var relativePath = Path.GetRelativePath(Path.GetDirectoryName(filePath), filePath);
        
        var image = new Image(collectionId, fileName, filePath, relativePath, new FileInfo(filePath).Length, metadata);
        
        return image;
    }
    
    public async Task<ImageMetadata> ExtractMetadataAsync(string filePath)
    {
        return await _metadataExtractor.ExtractAsync(filePath);
    }
    
    public async Task<bool> IsImageFileAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
        
        return supportedExtensions.Contains(extension);
    }
    
    public async Task<Image> ProcessImageAsync(Image image, ImageProcessingOptions options)
    {
        // Process image with given options
        var processedImage = await _imageProcessor.ProcessAsync(image, options);
        
        return processedImage;
    }
}
```

### 3. Cache Domain Service
```csharp
public interface ICacheDomainService
{
    Task<CacheInfo> GetCacheAsync(Guid imageId, CacheOptions options);
    Task<CacheInfo> SetCacheAsync(Guid imageId, byte[] imageData, CacheOptions options);
    Task<bool> DeleteCacheAsync(Guid imageId);
    Task<CacheStatistics> GetStatisticsAsync();
    Task CleanupExpiredAsync();
    Task<CacheFolder> GetBestCacheFolderAsync(long requiredSize);
}

public class CacheDomainService : ICacheDomainService
{
    private readonly ICacheInfoRepository _cacheInfoRepository;
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly IFileSystemService _fileSystemService;
    
    public async Task<CacheInfo> GetCacheAsync(Guid imageId, CacheOptions options)
    {
        var cacheInfo = await _cacheInfoRepository.GetByImageIdAsync(imageId);
        
        if (cacheInfo == null || !cacheInfo.IsValid() || cacheInfo.IsExpired())
            return null;
        
        // Check if cache file exists
        if (!await _fileSystemService.FileExistsAsync(cacheInfo.CachePath))
        {
            // Mark as invalid
            cacheInfo.MarkAsInvalid();
            await _cacheInfoRepository.UpdateAsync(cacheInfo);
            return null;
        }
        
        return cacheInfo;
    }
    
    public async Task<CacheInfo> SetCacheAsync(Guid imageId, byte[] imageData, CacheOptions options)
    {
        // Get best cache folder
        var cacheFolder = await GetBestCacheFolderAsync(imageData.Length);
        if (cacheFolder == null)
            throw new InvalidOperationException("No available cache folder");
        
        // Generate cache path
        var cachePath = Path.Combine(cacheFolder.Path, $"{imageId}_{options.GetHashCode()}.{options.Format}");
        
        // Save cache file
        await _fileSystemService.WriteAllBytesAsync(cachePath, imageData);
        
        // Create cache info
        var cacheInfo = new CacheInfo
        {
            ImageId = imageId,
            CachePath = cachePath,
            CacheSize = imageData.Length,
            Quality = options.Quality,
            Format = options.Format,
            Dimensions = $"{options.Width}x{options.Height}",
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(options.TTL),
            IsValid = true
        };
        
        await _cacheInfoRepository.AddAsync(cacheInfo);
        
        // Update cache folder statistics
        cacheFolder.UpdateUsage(imageData.Length, 1);
        await _cacheFolderRepository.UpdateAsync(cacheFolder);
        
        return cacheInfo;
    }
    
    public async Task<CacheFolder> GetBestCacheFolderAsync(long requiredSize)
    {
        var cacheFolders = await _cacheFolderRepository.GetActiveAsync();
        
        return cacheFolders
            .Where(cf => cf.CanAcceptCollection(requiredSize))
            .OrderBy(cf => cf.Priority)
            .ThenBy(cf => cf.CurrentSize)
            .FirstOrDefault();
    }
}
```

## Infrastructure Services

### 1. Repository Interfaces
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

public interface ICacheInfoRepository
{
    Task<CacheInfo> GetByImageIdAsync(Guid imageId);
    Task<IEnumerable<CacheInfo>> GetExpiredAsync();
    Task AddAsync(CacheInfo cacheInfo);
    Task UpdateAsync(CacheInfo cacheInfo);
    Task DeleteAsync(CacheInfo cacheInfo);
    IQueryable<CacheInfo> GetQueryable();
}
```

### 2. External Services
```csharp
public interface IFileSystemService
{
    Task<bool> DirectoryExistsAsync(string path);
    Task<bool> FileExistsAsync(string filePath);
    Task<byte[]> ReadAllBytesAsync(string filePath);
    Task WriteAllBytesAsync(string filePath, byte[] data);
    Task DeleteFileAsync(string filePath);
    Task DeleteDirectoryAsync(string path);
    Task<IEnumerable<string>> GetFilesAsync(string path, string pattern = "*");
    Task<IEnumerable<string>> GetDirectoriesAsync(string path);
}

public interface IImageProcessor
{
    Task<byte[]> ProcessAsync(Image image, ImageProcessingOptions options);
    Task<byte[]> GenerateThumbnailAsync(Image image, ThumbnailOptions options);
    Task<ImageMetadata> ExtractMetadataAsync(string filePath);
    Task<bool> IsImageFileAsync(string filePath);
}

public interface IMetadataExtractor
{
    Task<ImageMetadata> ExtractAsync(string filePath);
    Task<ExifData> ExtractExifAsync(string filePath);
    Task<IccProfile> ExtractIccProfileAsync(string filePath);
}

public interface IBackgroundJobService
{
    Task<string> EnqueueAsync<T>(T job) where T : IBackgroundJob;
    Task<string> ScheduleAsync<T>(T job, TimeSpan delay) where T : IBackgroundJob;
    Task<string> ScheduleAsync<T>(T job, DateTimeOffset scheduleAt) where T : IBackgroundJob;
    Task<bool> DeleteAsync(string jobId);
    Task<JobStatus> GetStatusAsync(string jobId);
}
```

## Service Registration

### Dependency Injection Configuration
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        
        // Application Services
        services.AddScoped<ICollectionApplicationService, CollectionApplicationService>();
        services.AddScoped<IImageApplicationService, ImageApplicationService>();
        services.AddScoped<ICacheApplicationService, CacheApplicationService>();
        
        // Domain Services
        services.AddScoped<ICollectionDomainService, CollectionDomainService>();
        services.AddScoped<IImageDomainService, ImageDomainService>();
        services.AddScoped<ICacheDomainService, CacheDomainService>();
        
        // Infrastructure Services
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<ICacheInfoRepository, CacheInfoRepository>();
        services.AddScoped<IFileSystemService, FileSystemService>();
        services.AddScoped<IImageProcessor, SkiaSharpImageProcessor>();
        services.AddScoped<IMetadataExtractor, ExifMetadataExtractor>();
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
        
        return services;
    }
}
```

## Conclusion

Service layers được thiết kế để:

1. **Separation of Concerns**: Mỗi layer có responsibility rõ ràng
2. **Testability**: Dễ dàng unit test và integration test
3. **Maintainability**: Code dễ maintain và extend
4. **Scalability**: Có thể scale từng layer độc lập
5. **Flexibility**: Dễ dàng thay đổi implementation

Architecture này đảm bảo hệ thống có thể phát triển và maintain một cách hiệu quả.
