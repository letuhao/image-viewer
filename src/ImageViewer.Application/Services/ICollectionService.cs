using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.DTOs.Common;
using ImageViewer.Application.Extensions;

namespace ImageViewer.Application.Services;

/// <summary>
/// Collection service interface
/// </summary>
public interface ICollectionService
{
    Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Collection?> GetByPathAsync(string path, CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginationResponseDto<Collection>> GetCollectionsAsync(PaginationRequestDto pagination, string? search = null, string? type = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetByTypeAsync(CollectionType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetCollectionsWithImagesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName, CancellationToken cancellationToken = default);
    Task<SearchResponseDto<Collection>> SearchCollectionsAsync(SearchRequestDto searchRequest, PaginationRequestDto pagination, CancellationToken cancellationToken = default);
    
    Task<Collection> CreateAsync(string name, string path, CollectionType type, CollectionSettings settings, CancellationToken cancellationToken = default);
    Task<Collection> UpdateAsync(Guid id, string? name = null, string? path = null, CollectionSettings? settings = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task ScanCollectionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CollectionStatistics> GetStatisticsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default);
    
    Task AddTagAsync(Guid collectionId, string tagName, string? description = null, TagColor? color = null, CancellationToken cancellationToken = default);
    Task RemoveTagAsync(Guid collectionId, string tagName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetTagsAsync(Guid collectionId, CancellationToken cancellationToken = default);
}

