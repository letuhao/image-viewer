using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.Events;
using ImageViewer.Application.DTOs.BackgroundJobs;
using ImageViewer.Application.Mappings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for Collection operations
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CollectionService> _logger;
    private readonly IThumbnailCacheService? _thumbnailCacheService;
    private readonly ICollectionIndexService? _collectionIndexService;

    public CollectionService(
        ICollectionRepository collectionRepository, 
        IMessageQueueService messageQueueService, 
        IServiceProvider serviceProvider, 
        ILogger<CollectionService> logger,
        IThumbnailCacheService? thumbnailCacheService = null,
        ICollectionIndexService? collectionIndexService = null)
    {
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _messageQueueService = messageQueueService ?? throw new ArgumentNullException(nameof(messageQueueService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _thumbnailCacheService = thumbnailCacheService; // Optional for unit tests
        _collectionIndexService = collectionIndexService; // Optional, fallback to MongoDB if not available
    }

    public async Task<Collection> CreateCollectionAsync(ObjectId? libraryId, string name, string path, CollectionType type, string? description = null, string? createdBy = null, string? createdBySystem = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Collection name cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(path))
                throw new ValidationException("Collection path cannot be null or empty");

            // Check if collection already exists at this path
            Collection? existingCollection = null;
            try
            {
                existingCollection = await _collectionRepository.GetByPathAsync(path);
            }
            catch (EntityNotFoundException)
            {
                // Collection doesn't exist, which is fine - we'll create a new one
                existingCollection = null;
            }
            
            if (existingCollection != null)
                throw new DuplicateEntityException($"Collection at path '{path}' already exists");

            // Create new collection with creator tracking
            var collection = new Collection(libraryId, name, path, type, description, createdBy, createdBySystem);
            var createdCollection = await _collectionRepository.CreateAsync(collection);
            
            // Sync to Redis index
            if (_collectionIndexService != null)
            {
                try
                {
                    await _collectionIndexService.AddOrUpdateCollectionAsync(createdCollection);
                    _logger.LogDebug("✅ Added collection {CollectionId} to Redis index", createdCollection.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add collection {CollectionId} to Redis index", createdCollection.Id);
                    // Continue even if index update fails
                }
            }
            
            // Trigger collection scan if AutoScan is enabled (default is true)
            if (createdCollection.Settings.AutoScan)
            {
                // Create background job for collection scan tracking - MANDATORY
                var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
                var scanJob = await backgroundJobService.CreateJobAsync(new CreateBackgroundJobDto
                {
                    Type = "collection-scan",
                    Description = $"Collection scan for {createdCollection.Name}",
                    CollectionId = createdCollection.Id // Link job to collection
                });

                var scanMessage = new CollectionScanMessage
                {
                    CollectionId = createdCollection.Id.ToString(), // Convert ObjectId to string
                    CollectionPath = createdCollection.Path,
                    CollectionType = createdCollection.Type,
                    ForceRescan = false,
                    CreatedBy = "CollectionService",
                    CreatedBySystem = "ImageViewer.Application",
                    JobId = scanJob.JobId.ToString() // Link message to job for tracking!
                };
                
                await _messageQueueService.PublishAsync(scanMessage); // Use default routing key
                _logger.LogInformation("✅ Queued collection scan for new collection {CollectionId}: {CollectionName} (Job: {JobId})", 
                    createdCollection.Id, createdCollection.Name, scanJob.JobId);
            }
            
            return createdCollection;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to create collection with name {Name} at path {Path}", name, path);
            throw new BusinessRuleException($"Failed to create collection with name '{name}' at path '{path}'", ex);
        }
    }

    public async Task<Collection?> GetCollectionByIdAsync(ObjectId collectionId)
    {
        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID '{collectionId}' not found");
            
            return collection;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to get collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection?> GetCollectionByPathAsync(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ValidationException("Collection path cannot be null or empty");

            var collection = await _collectionRepository.GetByPathAsync(path);
            if (collection == null)
                throw new EntityNotFoundException($"Collection at path '{path}' not found");
            
            return collection;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get collection at path {Path}", path);
            throw new BusinessRuleException($"Failed to get collection at path '{path}'", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByLibraryIdAsync(ObjectId libraryId)
    {
        try
        {
            return await _collectionRepository.GetByLibraryIdAsync(libraryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections for library {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to get collections for library '{libraryId}'", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsAsync(int page = 1, int pageSize = 20, string sortBy = "updatedAt", string sortDirection = "desc")
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            // Try Redis index first (10-50x faster!)
            if (_collectionIndexService != null)
            {
                try
                {
                    _logger.LogDebug("Using Redis index for GetCollectionsAsync with sort {SortBy} {SortDirection}", sortBy, sortDirection);
                    var result = await _collectionIndexService.GetCollectionPageAsync(
                        page, pageSize, sortBy, sortDirection);
                    
                    // Convert CollectionSummary to full Collection entities
                    // by fetching from MongoDB (batch operation)
                    var collectionIds = result.Collections.Select(s => ObjectId.Parse(s.Id)).ToList();
                    var collections = new List<Collection>();
                    
                    foreach (var id in collectionIds)
                    {
                        var collection = await _collectionRepository.GetByIdAsync(id);
                        if (collection != null)
                        {
                            collections.Add(collection);
                        }
                    }
                    
                    return collections;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis index failed, falling back to MongoDB");
                    // Fall through to MongoDB fallback
                }
            }

            // Fallback to MongoDB (original logic)
            _logger.LogDebug("Using MongoDB for GetCollectionsAsync");
            var skip = (page - 1) * pageSize;
            return await _collectionRepository.FindAsync(
                Builders<Collection>.Filter.Empty,
                Builders<Collection>.Sort.Descending(c => c.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get collections for page {Page} with page size {PageSize}", page, pageSize);
            throw new BusinessRuleException($"Failed to get collections for page {page}", ex);
        }
    }

    public async Task<long> GetTotalCollectionsCountAsync()
    {
        try
        {
            // Try Redis index first (100-200x faster - O(1) operation!)
            if (_collectionIndexService != null)
            {
                try
                {
                    _logger.LogDebug("Using Redis index for GetTotalCollectionsCountAsync");
                    return await _collectionIndexService.GetTotalCollectionsCountAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis index failed, falling back to MongoDB");
                    // Fall through to MongoDB fallback
                }
            }

            // Fallback to MongoDB
            _logger.LogDebug("Using MongoDB for GetTotalCollectionsCountAsync");
            return await _collectionRepository.CountAsync(Builders<Collection>.Filter.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total collections count");
            throw new BusinessRuleException("Failed to get total collections count", ex);
        }
    }

    public async Task<Collection> UpdateCollectionAsync(ObjectId collectionId, UpdateCollectionRequest request)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            
            if (request.Name != null)
            {
                collection.UpdateName(request.Name);
            }
            
            if (request.Description != null)
            {
                collection.UpdateDescription(request.Description);
            }
            
            if (request.Path != null)
            {
                // Check if path is already taken by another collection
                var existingCollection = await _collectionRepository.GetByPathAsync(request.Path);
                if (existingCollection != null && existingCollection.Id != collectionId)
                    throw new DuplicateEntityException($"Collection at path '{request.Path}' already exists");
                
                collection.UpdatePath(request.Path);
            }
            
            if (request.Type.HasValue)
            {
                collection.UpdateType(request.Type.Value);
            }
            
            var updatedCollection = await _collectionRepository.UpdateAsync(collection);
            
            // Sync to Redis index
            if (_collectionIndexService != null)
            {
                try
                {
                    await _collectionIndexService.AddOrUpdateCollectionAsync(updatedCollection);
                    _logger.LogDebug("✅ Updated collection {CollectionId} in Redis index", updatedCollection.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update collection {CollectionId} in Redis index", updatedCollection.Id);
                    // Continue even if index update fails
                }
            }
            
            return updatedCollection;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to update collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to update collection with ID '{collectionId}'", ex);
        }
    }

    public async Task DeleteCollectionAsync(ObjectId collectionId)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            await _collectionRepository.DeleteAsync(collectionId);
            
            // Remove from Redis index
            if (_collectionIndexService != null)
            {
                try
                {
                    await _collectionIndexService.RemoveCollectionAsync(collectionId);
                    _logger.LogDebug("✅ Removed collection {CollectionId} from Redis index", collectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove collection {CollectionId} from Redis index", collectionId);
                    // Continue even if index update fails
                }
            }
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to delete collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to delete collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> UpdateSettingsAsync(ObjectId collectionId, UpdateCollectionSettingsRequest request, bool triggerScan = true)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            
            var newSettings = new CollectionSettings();
            
            if (request.Enabled.HasValue)
            {
                if (request.Enabled.Value)
                    newSettings.Enable();
                else
                    newSettings.Disable();
            }
            
            if (request.AutoScan.HasValue)
                newSettings.UpdateScanSettings(request.AutoScan.Value, 
                    request.GenerateThumbnails ?? newSettings.GenerateThumbnails, 
                    request.GenerateCache ?? newSettings.GenerateCache);
            
            if (request.GenerateThumbnails.HasValue)
                newSettings.UpdateScanSettings(newSettings.AutoScan, 
                    request.GenerateThumbnails.Value, 
                    newSettings.GenerateCache);
            
            if (request.GenerateCache.HasValue)
                newSettings.UpdateScanSettings(newSettings.AutoScan, 
                    newSettings.GenerateThumbnails, 
                    request.GenerateCache.Value);
            
            if (request.EnableWatching.HasValue)
                newSettings.UpdateEnableWatching(request.EnableWatching.Value);
            
            if (request.ScanInterval.HasValue)
                newSettings.UpdateScanInterval(request.ScanInterval.Value);
            
            if (request.MaxFileSize.HasValue)
                newSettings.UpdateMaxFileSize(request.MaxFileSize.Value);
            
            if (request.AllowedFormats != null)
            {
                foreach (var format in request.AllowedFormats)
                {
                    newSettings.AddAllowedFormat(format);
                }
            }
            
            if (request.ExcludedPaths != null)
            {
                foreach (var path in request.ExcludedPaths)
                {
                    newSettings.AddExcludedPath(path);
                }
            }
            
            collection.UpdateSettings(newSettings);
            var updatedCollection = await _collectionRepository.UpdateAsync(collection);
            
        // Trigger collection scan if AutoScan is enabled AND triggerScan parameter is true
        if (newSettings.AutoScan && triggerScan)
        {
            // Create background job for collection scan tracking - MANDATORY
            var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
            var scanJob = await backgroundJobService.CreateJobAsync(new CreateBackgroundJobDto
            {
                Type = "collection-scan",
                Description = $"Collection scan for {collection.Name}",
                CollectionId = null // CollectionId is not used in BackgroundJob entity
            });

            var scanMessage = new CollectionScanMessage
            {
                CollectionId = collection.Id.ToString(), // Convert ObjectId to string
                CollectionPath = collection.Path,
                CollectionType = collection.Type,
                ForceRescan = false,
                CreatedBy = "CollectionService",
                CreatedBySystem = "ImageViewer.Application",
                JobId = scanJob.JobId.ToString() // Link message to job for tracking!
            };
            
            await _messageQueueService.PublishAsync(scanMessage, "collection.scan");
            _logger.LogInformation("✅ Queued collection scan for collection {CollectionId}: {CollectionName} (Job: {JobId})", 
                collection.Id, collection.Name, scanJob.JobId);
        }
            
            return updatedCollection;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update settings for collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to update settings for collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> UpdateMetadataAsync(ObjectId collectionId, UpdateCollectionMetadataRequest request)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            
            var newMetadata = new CollectionMetadata();
            
            if (request.Description != null)
                newMetadata.UpdateDescription(request.Description);
            
            if (request.Tags != null)
            {
                foreach (var tag in request.Tags)
                {
                    newMetadata.AddTag(tag);
                }
            }
            
            if (request.Categories != null)
            {
                foreach (var category in request.Categories)
                {
                    newMetadata.AddCategory(category);
                }
            }
            
            if (request.CustomFields != null)
            {
                foreach (var field in request.CustomFields)
                {
                    newMetadata.AddCustomField(field.Key, field.Value);
                }
            }
            
            if (request.Version != null)
                newMetadata.UpdateVersion(request.Version);
            
            if (request.CreatedBy != null)
                newMetadata.UpdateCreatedBy(request.CreatedBy);
            
            if (request.ModifiedBy != null)
                newMetadata.UpdateModifiedBy(request.ModifiedBy);
            
            collection.UpdateMetadata(newMetadata);
            return await _collectionRepository.UpdateAsync(collection);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update metadata for collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to update metadata for collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> UpdateStatisticsAsync(ObjectId collectionId, UpdateCollectionStatisticsRequest request)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            
            var newStatistics = new ImageViewer.Domain.ValueObjects.CollectionStatistics();
            
            if (request.TotalItems.HasValue)
                newStatistics.UpdateStats(request.TotalItems.Value, request.TotalSize ?? 0);
            
            if (request.TotalSize.HasValue)
                newStatistics.IncrementSize(request.TotalSize.Value);
            
            if (request.TotalViews.HasValue)
                newStatistics.IncrementViews(request.TotalViews.Value);
            
            if (request.TotalDownloads.HasValue)
                newStatistics.IncrementDownloads(request.TotalDownloads.Value);
            
            if (request.TotalShares.HasValue)
                newStatistics.IncrementShares(request.TotalShares.Value);
            
            if (request.TotalLikes.HasValue)
                newStatistics.IncrementLikes(request.TotalLikes.Value);
            
            if (request.TotalComments.HasValue)
                newStatistics.IncrementComments(request.TotalComments.Value);
            
            if (request.LastScanDate.HasValue)
                newStatistics.UpdateLastScanDate(request.LastScanDate.Value);
            
            if (request.LastActivity.HasValue)
                newStatistics.UpdateLastActivity();
            
            collection.UpdateStatistics(newStatistics);
            return await _collectionRepository.UpdateAsync(collection);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update statistics for collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to update statistics for collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> ActivateCollectionAsync(ObjectId collectionId)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            collection.Activate();
            return await _collectionRepository.UpdateAsync(collection);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to activate collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to activate collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> DeactivateCollectionAsync(ObjectId collectionId)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            collection.Deactivate();
            return await _collectionRepository.UpdateAsync(collection);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to deactivate collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to deactivate collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> EnableWatchingAsync(ObjectId collectionId)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            collection.EnableWatching();
            return await _collectionRepository.UpdateAsync(collection);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to enable watching for collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to enable watching for collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> DisableWatchingAsync(ObjectId collectionId)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            collection.DisableWatching();
            return await _collectionRepository.UpdateAsync(collection);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to disable watching for collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to disable watching for collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> UpdateWatchSettingsAsync(ObjectId collectionId, UpdateWatchSettingsRequest request)
    {
        try
        {
            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null)
                throw new EntityNotFoundException($"Collection with ID {collectionId} not found");
            
            if (request.IsWatching.HasValue)
            {
                if (request.IsWatching.Value)
                {
                    collection.EnableWatching();
                }
                else
                {
                    collection.DisableWatching();
                }
            }
            
            if (request.WatchPath != null)
            {
                collection.WatchInfo.UpdateWatchPath(request.WatchPath);
            }
            
            if (request.WatchFilters != null)
            {
                foreach (var filter in request.WatchFilters)
                {
                    collection.WatchInfo.AddWatchFilter(filter);
                }
            }
            
            return await _collectionRepository.UpdateAsync(collection);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update watch settings for collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to update watch settings for collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<IEnumerable<Collection>> SearchCollectionsAsync(string query, int page = 1, int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ValidationException("Search query cannot be null or empty");
            
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _collectionRepository.FindAsync(
                Builders<Collection>.Filter.Or(
                    Builders<Collection>.Filter.Regex(c => c.Name, new BsonRegularExpression(query, "i")),
                    Builders<Collection>.Filter.Regex(c => c.Path, new BsonRegularExpression(query, "i"))
                ),
                Builders<Collection>.Sort.Descending(c => c.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to search collections with query {Query}", query);
            throw new BusinessRuleException($"Failed to search collections with query '{query}'", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilterRequest filter, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var collectionFilter = new CollectionFilter
            {
                LibraryId = filter.LibraryId,
                Type = filter.Type,
                IsActive = filter.IsActive,
                CreatedAfter = filter.CreatedAfter,
                CreatedBefore = filter.CreatedBefore,
                Path = filter.Path,
                Tags = filter.Tags,
                Categories = filter.Categories
            };

            var skip = (page - 1) * pageSize;
            return await _collectionRepository.FindAsync(
                Builders<Collection>.Filter.Empty,
                Builders<Collection>.Sort.Descending(c => c.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get collections by filter");
            throw new BusinessRuleException("Failed to get collections by filter", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByLibraryAsync(ObjectId libraryId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _collectionRepository.FindAsync(
                Builders<Collection>.Filter.Eq(c => c.LibraryId, libraryId),
                Builders<Collection>.Sort.Descending(c => c.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get collections for library {LibraryId}", libraryId);
            throw new BusinessRuleException($"Failed to get collections for library '{libraryId}'", ex);
        }
    }

    public async Task<ImageViewer.Domain.ValueObjects.CollectionStatistics> GetCollectionStatisticsAsync()
    {
        try
        {
            return await _collectionRepository.GetCollectionStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection statistics");
            throw new BusinessRuleException("Failed to get collection statistics", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _collectionRepository.GetTopCollectionsByActivityAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get top collections by activity");
            throw new BusinessRuleException("Failed to get top collections by activity", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _collectionRepository.GetRecentCollectionsAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get recent collections");
            throw new BusinessRuleException("Failed to get recent collections", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _collectionRepository.FindAsync(
                Builders<Collection>.Filter.Eq(c => c.Type, type),
                Builders<Collection>.Sort.Descending(c => c.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get collections by type {Type}", type);
            throw new BusinessRuleException($"Failed to get collections by type '{type}'", ex);
        }
    }

    #region Collection Navigation

    public async Task<DTOs.Collections.CollectionNavigationDto> GetCollectionNavigationAsync(ObjectId collectionId, string sortBy = "updatedAt", string sortDirection = "desc")
    {
        try
        {
            _logger.LogDebug("Getting navigation info for collection {CollectionId} with sort {SortBy} {SortDirection}", collectionId, sortBy, sortDirection);

            // Try Redis index first (70-250x faster - O(log N) operation!)
            if (_collectionIndexService != null)
            {
                try
                {
                    _logger.LogDebug("Using Redis index for GetCollectionNavigationAsync");
                    var result = await _collectionIndexService.GetNavigationAsync(
                        collectionId, sortBy, sortDirection);
                    
                    return new DTOs.Collections.CollectionNavigationDto
                    {
                        PreviousCollectionId = result.PreviousCollectionId,
                        NextCollectionId = result.NextCollectionId,
                        CurrentPosition = result.CurrentPosition,
                        TotalCollections = result.TotalCollections,
                        HasPrevious = result.HasPrevious,
                        HasNext = result.HasNext
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis index failed, falling back to MongoDB");
                    // Fall through to MongoDB fallback
                }
            }

            // Fallback to MongoDB (original logic)
            _logger.LogDebug("Using MongoDB for GetCollectionNavigationAsync");
            
            // Get the current collection
            var currentCollection = await _collectionRepository.GetByIdAsync(collectionId);
            if (currentCollection == null || currentCollection.IsDeleted)
            {
                throw new BusinessRuleException($"Collection {collectionId} not found");
            }

            var currentSortValue = GetSortValue(currentCollection, sortBy);
            var isAscending = sortDirection.ToLower() == "asc";

            // Get previous collection
            var previousFilter = BuildNavigationFilter(sortBy, currentSortValue, currentCollection.Id, !isAscending, true);
            var previousSort = isAscending 
                ? BuildDescendingSortDefinition(sortBy) 
                : BuildAscendingSortDefinition(sortBy);
            
            var previousCollections = await _collectionRepository.FindAsync(
                previousFilter,
                previousSort,
                1,
                0
            );
            var previousCollection = previousCollections.FirstOrDefault();

            // Get next collection
            var nextFilter = BuildNavigationFilter(sortBy, currentSortValue, currentCollection.Id, isAscending, false);
            var nextSort = isAscending 
                ? BuildAscendingSortDefinition(sortBy) 
                : BuildDescendingSortDefinition(sortBy);
            
            var nextCollections = await _collectionRepository.FindAsync(
                nextFilter,
                nextSort,
                1,
                0
            );
            var nextCollection = nextCollections.FirstOrDefault();

            // Get total count
            var totalCount = await _collectionRepository.CountAsync(
                Builders<Collection>.Filter.Eq(c => c.IsDeleted, false)
            );

            // Calculate position
            var positionFilter = BuildNavigationFilter(sortBy, currentSortValue, currentCollection.Id, isAscending, false);
            var collectionsBeforeCurrent = await _collectionRepository.CountAsync(positionFilter);
            var currentPosition = (int)collectionsBeforeCurrent + 1;

            return new DTOs.Collections.CollectionNavigationDto
            {
                PreviousCollectionId = previousCollection?.Id.ToString(),
                NextCollectionId = nextCollection?.Id.ToString(),
                CurrentPosition = currentPosition,
                TotalCollections = (int)totalCount,
                HasPrevious = previousCollection != null,
                HasNext = nextCollection != null
            };
        }
        catch (Exception ex) when (!(ex is BusinessRuleException))
        {
            _logger.LogError(ex, "Failed to get navigation info for collection {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to get navigation info for collection", ex);
        }
    }

    public async Task<DTOs.Collections.CollectionSiblingsDto> GetCollectionSiblingsAsync(ObjectId collectionId, int page = 1, int pageSize = 20, string sortBy = "updatedAt", string sortDirection = "desc")
    {
        try
        {
            _logger.LogDebug("Getting siblings for collection {CollectionId} (page {Page}, size {PageSize})", collectionId, page, pageSize);

            // Try Redis index first (100-250x faster - no memory overhead!)
            if (_collectionIndexService != null)
            {
                try
                {
                    _logger.LogDebug("Using Redis index for GetCollectionSiblingsAsync");
                    var result = await _collectionIndexService.GetSiblingsAsync(
                        collectionId, page, pageSize, sortBy, sortDirection);
                    
                    // Convert CollectionSummary to CollectionOverviewDto
                    var siblings = result.Siblings.Select(s => new DTOs.Collections.CollectionOverviewDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ImageCount = s.ImageCount,
                        ThumbnailCount = s.ThumbnailCount,
                        CacheImageCount = s.CacheCount,
                        TotalSize = s.TotalSize,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        FirstImageId = s.FirstImageId,
                        ThumbnailBase64 = null // Will be populated by controller if needed
                    }).ToList();
                    
                    return new DTOs.Collections.CollectionSiblingsDto
                    {
                        Siblings = siblings,
                        CurrentPosition = result.CurrentPosition,
                        CurrentPage = result.CurrentPage,
                        TotalCount = result.TotalCount,
                        TotalPages = result.TotalPages
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis index failed, falling back to MongoDB");
                    // Fall through to MongoDB fallback
                }
            }

            // Fallback to MongoDB (original logic - loads ALL collections!)
            _logger.LogDebug("Using MongoDB for GetCollectionSiblingsAsync");
            var allCollections = (await GetSortedCollectionsAsync(sortBy, sortDirection)).ToList();
            
            var currentPosition = allCollections.FindIndex(c => c.Id == collectionId);
            
            if (currentPosition == -1)
            {
                throw new BusinessRuleException($"Collection {collectionId} not found in sorted list");
            }

            var skip = (page - 1) * pageSize;
            var paginatedCollections = allCollections
                .Skip(skip)
                .Take(pageSize)
                .ToList();
            
            var siblingDtos = paginatedCollections.Select(c => c.ToOverviewDto()).ToList();

            // Populate firstImageId
            for (int i = 0; i < paginatedCollections.Count; i++)
            {
                if (siblingDtos[i].FirstImageId == null && paginatedCollections[i].Images?.Count > 0)
                {
                    siblingDtos[i].FirstImageId = paginatedCollections[i].Images[0].Id;
                }
            }

            var totalPages = (int)Math.Ceiling((double)allCollections.Count / pageSize);
            var currentPageNumber = (currentPosition / pageSize) + 1;
            
            return new DTOs.Collections.CollectionSiblingsDto
            {
                Siblings = siblingDtos,
                CurrentPosition = currentPosition + 1, // 1-based
                CurrentPage = (page == 1) ? currentPageNumber : page,
                TotalCount = allCollections.Count,
                TotalPages = totalPages
            };
        }
        catch (Exception ex) when (!(ex is BusinessRuleException))
        {
            _logger.LogError(ex, "Failed to get siblings for collection {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to get siblings for collection", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetSortedCollectionsAsync(string sortBy = "updatedAt", string sortDirection = "desc", int? limit = null)
    {
        try
        {
            _logger.LogDebug("Getting sorted collections from database: {SortBy} {SortDirection}, Limit: {Limit}", sortBy, sortDirection, limit);

            // Build sort definition based on sortBy field
            var sortDefinition = sortDirection.ToLower() == "asc" 
                ? BuildAscendingSortDefinition(sortBy)
                : BuildDescendingSortDefinition(sortBy);

            // Get collections with sorting
            // Note: Using MongoDB indexes on sortBy fields for fast sorting
            return await _collectionRepository.FindAsync(
                Builders<Collection>.Filter.Eq(c => c.IsDeleted, false),
                sortDefinition,
                limit ?? int.MaxValue,
                0
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sorted collections");
            throw new BusinessRuleException($"Failed to get sorted collections", ex);
        }
    }

    /// <summary>
    /// Get sort value from collection based on sort field
    /// </summary>
    private object GetSortValue(Collection collection, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "createdat" => collection.CreatedAt,
            "updatedat" => collection.UpdatedAt,
            "name" => collection.Name,
            "imagecount" => collection.Statistics.TotalItems,
            "totalsize" => collection.Statistics.TotalSize,
            _ => collection.UpdatedAt
        };
    }

    /// <summary>
    /// Build MongoDB filter for navigation (find previous/next collection)
    /// </summary>
    private FilterDefinition<Collection> BuildNavigationFilter(string sortBy, object currentValue, ObjectId currentId, bool greater, bool orEqual)
    {
        var baseFilter = Builders<Collection>.Filter.Eq(c => c.IsDeleted, false);
        
        FilterDefinition<Collection> sortFilter = sortBy.ToLower() switch
        {
            "createdat" => greater
                ? (orEqual 
                    ? Builders<Collection>.Filter.Lte(c => c.CreatedAt, (DateTime)currentValue)
                    : Builders<Collection>.Filter.Lt(c => c.CreatedAt, (DateTime)currentValue))
                : (orEqual 
                    ? Builders<Collection>.Filter.Gte(c => c.CreatedAt, (DateTime)currentValue)
                    : Builders<Collection>.Filter.Gt(c => c.CreatedAt, (DateTime)currentValue)),
            
            "updatedat" => greater
                ? (orEqual 
                    ? Builders<Collection>.Filter.Lte(c => c.UpdatedAt, (DateTime)currentValue)
                    : Builders<Collection>.Filter.Lt(c => c.UpdatedAt, (DateTime)currentValue))
                : (orEqual 
                    ? Builders<Collection>.Filter.Gte(c => c.UpdatedAt, (DateTime)currentValue)
                    : Builders<Collection>.Filter.Gt(c => c.UpdatedAt, (DateTime)currentValue)),
            
            "name" => greater
                ? (orEqual 
                    ? Builders<Collection>.Filter.Lte(c => c.Name, (string)currentValue)
                    : Builders<Collection>.Filter.Lt(c => c.Name, (string)currentValue))
                : (orEqual 
                    ? Builders<Collection>.Filter.Gte(c => c.Name, (string)currentValue)
                    : Builders<Collection>.Filter.Gt(c => c.Name, (string)currentValue)),
            
            "imagecount" => greater
                ? (orEqual 
                    ? Builders<Collection>.Filter.Lte(c => c.Statistics.TotalItems, (int)currentValue)
                    : Builders<Collection>.Filter.Lt(c => c.Statistics.TotalItems, (int)currentValue))
                : (orEqual 
                    ? Builders<Collection>.Filter.Gte(c => c.Statistics.TotalItems, (int)currentValue)
                    : Builders<Collection>.Filter.Gt(c => c.Statistics.TotalItems, (int)currentValue)),
            
            "totalsize" => greater
                ? (orEqual 
                    ? Builders<Collection>.Filter.Lte(c => c.Statistics.TotalSize, (long)currentValue)
                    : Builders<Collection>.Filter.Lt(c => c.Statistics.TotalSize, (long)currentValue))
                : (orEqual 
                    ? Builders<Collection>.Filter.Gte(c => c.Statistics.TotalSize, (long)currentValue)
                    : Builders<Collection>.Filter.Gt(c => c.Statistics.TotalSize, (long)currentValue)),
            
            _ => greater
                ? (orEqual 
                    ? Builders<Collection>.Filter.Lte(c => c.UpdatedAt, (DateTime)currentValue)
                    : Builders<Collection>.Filter.Lt(c => c.UpdatedAt, (DateTime)currentValue))
                : (orEqual 
                    ? Builders<Collection>.Filter.Gte(c => c.UpdatedAt, (DateTime)currentValue)
                    : Builders<Collection>.Filter.Gt(c => c.UpdatedAt, (DateTime)currentValue))
        };
        
        // Exclude current collection
        var excludeCurrentFilter = Builders<Collection>.Filter.Ne(c => c.Id, currentId);
        
        return Builders<Collection>.Filter.And(baseFilter, sortFilter, excludeCurrentFilter);
    }

    private SortDefinition<Collection> BuildAscendingSortDefinition(string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "createdat" => Builders<Collection>.Sort.Ascending(c => c.CreatedAt),
            "updatedat" => Builders<Collection>.Sort.Ascending(c => c.UpdatedAt),
            "name" => Builders<Collection>.Sort.Ascending(c => c.Name),
            "imagecount" => Builders<Collection>.Sort.Ascending(c => c.Statistics.TotalItems),
            "totalsize" => Builders<Collection>.Sort.Ascending(c => c.Statistics.TotalSize),
            _ => Builders<Collection>.Sort.Ascending(c => c.UpdatedAt)
        };
    }

    private SortDefinition<Collection> BuildDescendingSortDefinition(string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "createdat" => Builders<Collection>.Sort.Descending(c => c.CreatedAt),
            "updatedat" => Builders<Collection>.Sort.Descending(c => c.UpdatedAt),
            "name" => Builders<Collection>.Sort.Descending(c => c.Name),
            "imagecount" => Builders<Collection>.Sort.Descending(c => c.Statistics.TotalItems),
            "totalsize" => Builders<Collection>.Sort.Descending(c => c.Statistics.TotalSize),
            _ => Builders<Collection>.Sort.Descending(c => c.UpdatedAt)
        };
    }

    #endregion
}