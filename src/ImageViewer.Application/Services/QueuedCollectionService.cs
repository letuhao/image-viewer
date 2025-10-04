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
        var collection = await _collectionService.CreateCollectionAsync(ObjectId.Empty, name, path, type);
        
        // Queue collection scan if auto-scan is enabled
        if (settings?.AutoGenerateCache == true)
        {
            var scanMessage = new CollectionScanMessage
            {
                CollectionId = collection.Id,
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
                CollectionId = collection.Id,
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
        // TODO: Implement restore functionality
        throw new NotImplementedException("Restore functionality not yet implemented");
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
            CollectionId = collection.Id,
            CollectionPath = collection.Path,
            CollectionType = collection.Type,
            ForceRescan = true
        };
        
        await _messageQueueService.PublishAsync(scanMessage, cancellationToken: cancellationToken);
        _logger.LogInformation("Queued collection scan for collection {CollectionId}", collection.Id);
    }

    public async Task<ImageViewer.Domain.ValueObjects.CollectionStatistics> GetStatisticsAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetStatisticsAsync
        throw new NotImplementedException("GetStatisticsAsync not yet implemented");
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetTotalSizeAsync
        throw new NotImplementedException("GetTotalSizeAsync not yet implemented");
    }

    public async Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetTotalImageCountAsync
        throw new NotImplementedException("GetTotalImageCountAsync not yet implemented");
    }

    public async Task AddTagAsync(ObjectId collectionId, string tagName, string? description = null, TagColor? color = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement AddTagAsync
        throw new NotImplementedException("AddTagAsync not yet implemented");
    }

    public async Task RemoveTagAsync(ObjectId collectionId, string tagName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement RemoveTagAsync
        throw new NotImplementedException("RemoveTagAsync not yet implemented");
    }

    public async Task<IEnumerable<Tag>> GetTagsAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetTagsAsync
        throw new NotImplementedException("GetTagsAsync not yet implemented");
    }

    #region ICollectionService Implementation

    public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string name, string path, CollectionType type)
    {
        return await _collectionService.CreateCollectionAsync(libraryId, name, path, type);
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

    public async Task<IEnumerable<Collection>> GetCollectionsAsync(int page, int pageSize)
    {
        return await _collectionService.GetCollectionsAsync(page, pageSize);
    }

    public async Task<Collection> UpdateCollectionAsync(ObjectId id, UpdateCollectionRequest request)
    {
        return await _collectionService.UpdateCollectionAsync(id, request);
    }

    public async Task DeleteCollectionAsync(ObjectId id)
    {
        await _collectionService.DeleteCollectionAsync(id);
    }

    public async Task<Collection> UpdateSettingsAsync(ObjectId id, UpdateCollectionSettingsRequest request)
    {
        return await _collectionService.UpdateSettingsAsync(id, request);
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
}
