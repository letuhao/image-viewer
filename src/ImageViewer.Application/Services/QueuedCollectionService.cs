using Microsoft.Extensions.Logging;
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
        return await _collectionService.SearchCollectionsAsync(searchRequest, pagination, cancellationToken);
    }

    public async Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetByNameAsync(name, cancellationToken);
    }

    public async Task<Collection?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetByPathAsync(path, cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetAllAsync(cancellationToken);
    }

    public async Task<PaginationResponseDto<Collection>> GetCollectionsAsync(PaginationRequestDto pagination, string? search = null, string? type = null, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetCollectionsAsync(pagination, search, type, cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetByTypeAsync(CollectionType type, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetByTypeAsync(type, cancellationToken);
    }

    public async Task<IEnumerable<Collection>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _collectionService.SearchByNameAsync(searchTerm, cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsWithImagesAsync(CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetCollectionsWithImagesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetCollectionsByTagAsync(tagName, cancellationToken);
    }

    public async Task<Collection> CreateAsync(string name, string path, CollectionType type, CollectionSettings settings, CancellationToken cancellationToken = default)
    {
        var collection = await _collectionService.CreateAsync(name, path, type, settings, cancellationToken);
        
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

    public async Task<Collection> UpdateAsync(Guid id, string? name = null, string? path = null, CollectionSettings? settings = null, CancellationToken cancellationToken = default)
    {
        var collection = await _collectionService.UpdateAsync(id, name, path, settings, cancellationToken);
        
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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _collectionService.DeleteAsync(id, cancellationToken);
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _collectionService.RestoreAsync(id, cancellationToken);
    }

    public async Task ScanCollectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Queue the scan operation instead of doing it synchronously
        var collection = await _collectionService.GetByIdAsync(id, cancellationToken);
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

    public async Task<CollectionStatistics> GetStatisticsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetStatisticsAsync(id, cancellationToken);
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetTotalSizeAsync(cancellationToken);
    }

    public async Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetTotalImageCountAsync(cancellationToken);
    }

    public async Task AddTagAsync(Guid collectionId, string tagName, string? description = null, TagColor? color = null, CancellationToken cancellationToken = default)
    {
        await _collectionService.AddTagAsync(collectionId, tagName, description, color, cancellationToken);
    }

    public async Task RemoveTagAsync(Guid collectionId, string tagName, CancellationToken cancellationToken = default)
    {
        await _collectionService.RemoveTagAsync(collectionId, tagName, cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetTagsAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        return await _collectionService.GetTagsAsync(collectionId, cancellationToken);
    }
}
