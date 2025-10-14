using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Events;
using ImageViewer.Application.DTOs.Common;
using ImageViewer.Application.Extensions;
using ImageViewer.Application.Options;
using Microsoft.Extensions.Options;

namespace ImageViewer.Application.Services;

/// <summary>
/// Collection service with message queue integration
/// </summary>
public class QueuedCollectionService : ICollectionService
{
    private readonly ICollectionService _collectionService;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<QueuedCollectionService> _logger;

    public QueuedCollectionService(
        ICollectionService collectionService,
        IMessageQueueService messageQueueService,
        ILogger<QueuedCollectionService> logger)
    {
        _collectionService = collectionService;
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    public async Task<SearchResponseDto<Collection>> SearchCollectionsAsync(SearchRequestDto searchRequest, PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        var collections = await _collectionService.GetCollectionsAsync();
        return new SearchResponseDto<Collection>
        {
            Results = collections,
            TotalResults = collections.Count(),
            Query = searchRequest.Query,
            SearchTime = TimeSpan.Zero
        };
    }

    public async Task<Collection?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetCollectionByIdAsync(id);
    }

    public async Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        // Note: ICollectionService doesn't have GetByNameAsync, using GetCollectionsAsync instead
        var collections = await _collectionService.GetCollectionsAsync();
        return collections.FirstOrDefault(c => c.Name == name);
    }

    public async Task<Collection?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetCollectionByPathAsync(path);
    }

    public async Task<IEnumerable<Collection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetCollectionsAsync();
    }

    public async Task<PaginationResponseDto<Collection>> GetCollectionsAsync(PaginationRequestDto pagination, string? search = null, string? type = null, CancellationToken cancellationToken = default)
    {
        var collections = await _collectionService.GetCollectionsAsync();
        // Note: This is a simplified implementation, proper pagination would be handled by the repository
        return new PaginationResponseDto<Collection>
        {
            Data = collections,
            TotalCount = collections.Count(),
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<IEnumerable<Collection>> GetByTypeAsync(CollectionType type, CancellationToken cancellationToken = default)
    {
        var collections = await _collectionService.GetCollectionsAsync();
        return collections.Where(c => c.Type == type);
    }

    public async Task<IEnumerable<Collection>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var collections = await _collectionService.GetCollectionsAsync();
        return collections.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<Collection>> GetCollectionsWithImagesAsync(CancellationToken cancellationToken = default)
    {
        var collections = await _collectionService.GetCollectionsAsync();
        // Note: This is a simplified implementation, proper filtering would be handled by the repository
        return collections.Where(c => c.Statistics.TotalItems > 0);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        // Note: This is a simplified implementation, proper tag filtering would be handled by the repository
        // For now, return empty list as Collection entity doesn't have Tags property
        return new List<Collection>();
    }

    public async Task<Collection> CreateAsync(string name, string path, CollectionType type, CollectionSettings settings, CancellationToken cancellationToken = default)
    {
        var collection = await _collectionService.CreateCollectionAsync(ObjectId.Empty, name, path, type, createdBy: "QueuedCollectionService", createdBySystem: "ImageViewer.API");
        
        // Queue collection scan if auto-scan is enabled
        if (settings?.AutoGenerateCache == true)
        {
            var scanMessage = new CollectionScanMessage
            {
                CollectionId = collection.Id.ToString(), // Convert ObjectId to string
                CollectionPath = collection.Path,
                CollectionType = collection.Type,
                ForceRescan = false
            };
            
            await _messageQueueService.PublishAsync(scanMessage, cancellationToken: cancellationToken);
            _logger.LogInformation("Queued collection scan for collection {CollectionId}", collection.Id);
        }

        return collection;
    }

    public async Task<Collection> UpdateAsync(ObjectId id, string? name = null, string? path = null, CollectionSettings? settings = null, CancellationToken cancellationToken = default)
    {
        var updateRequest = new UpdateCollectionRequest
        {
            Name = name,
            Path = path
        };
        var collection = await _collectionService.UpdateCollectionAsync(id, updateRequest);
        
        // Queue collection scan if auto-scan is enabled
        if (settings?.AutoGenerateCache == true)
        {
            var scanMessage = new CollectionScanMessage
            {
                CollectionId = collection.Id.ToString(), // Convert ObjectId to string
                CollectionPath = collection.Path,
                CollectionType = collection.Type,
                ForceRescan = true
            };
            
            await _messageQueueService.PublishAsync(scanMessage, cancellationToken: cancellationToken);
            _logger.LogInformation("Queued collection scan for updated collection {CollectionId}", collection.Id);
        }

        return collection;
    }

    public async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        await _collectionService.DeleteCollectionAsync(id);
    }

    public async Task RestoreAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _collectionService.GetCollectionByIdAsync(id);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{id}' not found");
            }

            // Restore the collection by activating it (since there's no IsDeleted property)
            collection.Activate();

            // Create update request to save the changes
            var updateRequest = new UpdateCollectionRequest
            {
                Name = collection.Name,
                Path = collection.Path,
                Type = collection.Type
            };

            await _collectionService.UpdateCollectionAsync(id, updateRequest);

            _logger.LogInformation("Collection {CollectionId} has been restored", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore collection {CollectionId}", id);
            throw;
        }
    }

    public async Task ScanCollectionAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        // Queue the scan operation instead of doing it synchronously
        var collection = await _collectionService.GetCollectionByIdAsync(id);
        if (collection == null)
        {
            throw new InvalidOperationException($"Collection with ID '{id}' not found");
        }

        var scanMessage = new CollectionScanMessage
        {
            CollectionId = collection.Id.ToString(), // Convert ObjectId to string
            CollectionPath = collection.Path,
            CollectionType = collection.Type,
            ForceRescan = true
        };
        
        await _messageQueueService.PublishAsync(scanMessage, cancellationToken: cancellationToken);
        _logger.LogInformation("Queued collection scan for collection {CollectionId}", collection.Id);
    }

    public async Task<ImageViewer.Domain.ValueObjects.CollectionStatistics> GetStatisticsAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _collectionService.GetCollectionByIdAsync(id);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{id}' not found");
            }

            // Return the existing statistics from the collection
            var statistics = collection.Statistics;

            _logger.LogInformation("Retrieved statistics for collection {CollectionId}: {TotalItems} items, {TotalSize} bytes", 
                id, statistics.TotalItems, statistics.TotalSize);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for collection {CollectionId}", id);
            throw;
        }
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _collectionService.GetCollectionsAsync();
            var totalSize = collections
                .Where(c => c.IsActive)
                .Sum(c => c.Statistics.TotalSize);

            _logger.LogInformation("Total size across all collections: {TotalSize} bytes", totalSize);
            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total size across all collections");
            throw;
        }
    }

    public async Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _collectionService.GetCollectionsAsync();
            var totalCount = collections
                .Where(c => c.IsActive)
                .Sum(c => (int)c.Statistics.TotalItems);

            _logger.LogInformation("Total image count across all collections: {TotalCount}", totalCount);
            return totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total image count across all collections");
            throw;
        }
    }

    public async Task AddTagAsync(ObjectId collectionId, string tagName, string? description = null, TagColor? color = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{collectionId}' not found");
            }

            // TODO: Implement when TagRepository is available
            // For now, log the operation and return success
            _logger.LogInformation("Tag '{TagName}' would be added to collection {CollectionId} (TagRepository not yet implemented)", 
                tagName, collectionId);

            // In a real implementation, this would:
            // 1. Check if tag exists, create if not
            // 2. Create CollectionTag relationship
            // 3. Update tag usage count
            // 4. Save changes to database
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tag '{TagName}' to collection {CollectionId}", tagName, collectionId);
            throw;
        }
    }

    public async Task RemoveTagAsync(ObjectId collectionId, string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{collectionId}' not found");
            }

            // TODO: Implement when TagRepository is available
            // For now, log the operation and return success
            _logger.LogInformation("Tag '{TagName}' would be removed from collection {CollectionId} (TagRepository not yet implemented)", 
                tagName, collectionId);

            // In a real implementation, this would:
            // 1. Find the tag by name
            // 2. Remove CollectionTag relationship
            // 3. Update tag usage count
            // 4. Save changes to database
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove tag '{TagName}' from collection {CollectionId}", tagName, collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> GetTagsAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{collectionId}' not found");
            }

            // TODO: Implement when TagRepository is available
            // For now, return empty list and log the operation
            _logger.LogInformation("Retrieving tags for collection {CollectionId} (TagRepository not yet implemented)", collectionId);

            // In a real implementation, this would:
            // 1. Query TagRepository for tags associated with this collection
            // 2. Return the list of tags
            return new List<Tag>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tags for collection {CollectionId}", collectionId);
            throw;
        }
    }

    #region ICollectionService Implementation

    public async Task<Collection> CreateCollectionAsync(ObjectId? libraryId, string name, string path, CollectionType type, string? description = null, string? createdBy = null, string? createdBySystem = null)
    {
        return await _collectionService.CreateCollectionAsync(libraryId, name, path, type, description, createdBy, createdBySystem);
    }

    public async Task<Collection?> GetCollectionByIdAsync(ObjectId id)
    {
        return await _collectionService.GetCollectionByIdAsync(id);
    }

    public async Task<Collection?> GetCollectionByPathAsync(string path)
    {
        return await _collectionService.GetCollectionByPathAsync(path);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByLibraryIdAsync(ObjectId libraryId)
    {
        return await _collectionService.GetCollectionsByLibraryIdAsync(libraryId);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsAsync(int page = 1, int pageSize = 20, string sortBy = "updatedAt", string sortDirection = "desc")
    {
        return await _collectionService.GetCollectionsAsync(page, pageSize, sortBy, sortDirection);
    }

    public async Task<long> GetTotalCollectionsCountAsync()
    {
        return await _collectionService.GetTotalCollectionsCountAsync();
    }

    public async Task<Collection> UpdateCollectionAsync(ObjectId id, UpdateCollectionRequest request)
    {
        return await _collectionService.UpdateCollectionAsync(id, request);
    }

    public async Task DeleteCollectionAsync(ObjectId id)
    {
        await _collectionService.DeleteCollectionAsync(id);
    }

    public async Task<Collection> UpdateSettingsAsync(ObjectId id, UpdateCollectionSettingsRequest request, bool triggerScan = true, bool forceRescan = false)
    {
        return await _collectionService.UpdateSettingsAsync(id, request, triggerScan, forceRescan);
    }

    public async Task<Collection> UpdateMetadataAsync(ObjectId id, UpdateCollectionMetadataRequest request)
    {
        return await _collectionService.UpdateMetadataAsync(id, request);
    }

    public async Task<Collection> UpdateStatisticsAsync(ObjectId id, UpdateCollectionStatisticsRequest request)
    {
        return await _collectionService.UpdateStatisticsAsync(id, request);
    }

    public async Task<Collection> ActivateCollectionAsync(ObjectId id)
    {
        return await _collectionService.ActivateCollectionAsync(id);
    }

    public async Task<Collection> DeactivateCollectionAsync(ObjectId id)
    {
        return await _collectionService.DeactivateCollectionAsync(id);
    }

    public async Task<Collection> EnableWatchingAsync(ObjectId id)
    {
        return await _collectionService.EnableWatchingAsync(id);
    }

    public async Task<Collection> DisableWatchingAsync(ObjectId id)
    {
        return await _collectionService.DisableWatchingAsync(id);
    }

    public async Task<Collection> UpdateWatchSettingsAsync(ObjectId id, UpdateWatchSettingsRequest request)
    {
        return await _collectionService.UpdateWatchSettingsAsync(id, request);
    }

    public async Task<IEnumerable<Collection>> SearchCollectionsAsync(string query, int page, int pageSize)
    {
        return await _collectionService.SearchCollectionsAsync(query, page, pageSize);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilterRequest filter, int page, int pageSize)
    {
        return await _collectionService.GetCollectionsByFilterAsync(filter, page, pageSize);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByLibraryAsync(ObjectId libraryId, int page, int pageSize)
    {
        return await _collectionService.GetCollectionsByLibraryAsync(libraryId, page, pageSize);
    }

    public async Task<ImageViewer.Domain.ValueObjects.CollectionStatistics> GetCollectionStatisticsAsync()
    {
        return await _collectionService.GetCollectionStatisticsAsync();
    }

    public async Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit)
    {
        return await _collectionService.GetTopCollectionsByActivityAsync(limit);
    }

    public async Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit)
    {
        return await _collectionService.GetRecentCollectionsAsync(limit);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type, int page, int pageSize)
    {
        return await _collectionService.GetCollectionsByTypeAsync(type, page, pageSize);
    }

    #endregion

    #region Collection Navigation

    public async Task<DTOs.Collections.CollectionNavigationDto> GetCollectionNavigationAsync(ObjectId collectionId, string sortBy = "updatedAt", string sortDirection = "desc")
    {
        return await _collectionService.GetCollectionNavigationAsync(collectionId, sortBy, sortDirection);
    }

    public async Task<DTOs.Collections.CollectionSiblingsDto> GetCollectionSiblingsAsync(ObjectId collectionId, int page = 1, int pageSize = 20, string sortBy = "updatedAt", string sortDirection = "desc")
    {
        return await _collectionService.GetCollectionSiblingsAsync(collectionId, page, pageSize, sortBy, sortDirection);
    }

    public async Task<IEnumerable<Collection>> GetSortedCollectionsAsync(string sortBy = "updatedAt", string sortDirection = "desc", int? limit = null)
    {
        return await _collectionService.GetSortedCollectionsAsync(sortBy, sortDirection, limit);
    }

    #endregion
    
    #region Collection Cleanup
    
    public async Task<CollectionCleanupResult> CleanupNonExistentCollectionsAsync()
    {
        return await _collectionService.CleanupNonExistentCollectionsAsync();
    }
    
    #endregion
}
