using ImageViewer.Application.DTOs.Tags;

namespace ImageViewer.Application.Services;

/// <summary>
/// Tag service interface for managing tags operations
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Get collection tags
    /// </summary>
    Task<IEnumerable<CollectionTagDto>> GetCollectionTagsAsync(Guid collectionId);

    /// <summary>
    /// Add tag to collection
    /// </summary>
    Task<CollectionTagDto> AddTagToCollectionAsync(Guid collectionId, AddTagToCollectionDto dto);

    /// <summary>
    /// Remove tag from collection
    /// </summary>
    Task RemoveTagFromCollectionAsync(Guid collectionId, string tagName);

    /// <summary>
    /// Get all tags
    /// </summary>
    Task<IEnumerable<TagDto>> GetAllTagsAsync();

    /// <summary>
    /// Get tag by ID
    /// </summary>
    Task<TagDto> GetTagAsync(Guid tagId);

    /// <summary>
    /// Create new tag
    /// </summary>
    Task<TagDto> CreateTagAsync(CreateTagDto dto);

    /// <summary>
    /// Update tag
    /// </summary>
    Task<TagDto> UpdateTagAsync(Guid tagId, UpdateTagDto dto);

    /// <summary>
    /// Delete tag
    /// </summary>
    Task DeleteTagAsync(Guid tagId);

    /// <summary>
    /// Get tag statistics
    /// </summary>
    Task<TagStatisticsDto> GetTagStatisticsAsync();

    /// <summary>
    /// Search tags
    /// </summary>
    Task<IEnumerable<TagDto>> SearchTagsAsync(string query, int limit = 20);

    /// <summary>
    /// Get popular tags
    /// </summary>
    Task<IEnumerable<PopularTagDto>> GetPopularTagsAsync(int limit = 20);

    /// <summary>
    /// Get tag suggestions for collection
    /// </summary>
    Task<IEnumerable<TagSuggestionDto>> GetTagSuggestionsAsync(Guid collectionId, int limit = 10);

    /// <summary>
    /// Bulk add tags to collection
    /// </summary>
    Task<IEnumerable<CollectionTagDto>> BulkAddTagsToCollectionAsync(Guid collectionId, IEnumerable<string> tagNames);

    /// <summary>
    /// Bulk remove tags from collection
    /// </summary>
    Task BulkRemoveTagsFromCollectionAsync(Guid collectionId, IEnumerable<string> tagNames);
}
