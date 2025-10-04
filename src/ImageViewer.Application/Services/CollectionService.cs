using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for Collection operations
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(ICollectionRepository collectionRepository, ILogger<CollectionService> logger)
    {
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string name, string path, CollectionType type)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Collection name cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(path))
                throw new ValidationException("Collection path cannot be null or empty");

            // Check if collection already exists at this path
            var existingCollection = await _collectionRepository.GetByPathAsync(path);
            if (existingCollection != null)
                throw new DuplicateEntityException($"Collection at path '{path}' already exists");

            // Create new collection
            var collection = new Collection(libraryId, name, path, type);
            return await _collectionRepository.CreateAsync(collection);
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

    public async Task<IEnumerable<Collection>> GetCollectionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

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
            
            return await _collectionRepository.UpdateAsync(collection);
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
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to delete collection with ID {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to delete collection with ID '{collectionId}'", ex);
        }
    }

    public async Task<Collection> UpdateSettingsAsync(ObjectId collectionId, UpdateCollectionSettingsRequest request)
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
            return await _collectionRepository.UpdateAsync(collection);
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
}