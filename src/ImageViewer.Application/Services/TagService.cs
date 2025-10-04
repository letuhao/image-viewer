using ImageViewer.Application.DTOs.Tags;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Tag service implementation
/// </summary>
public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICollectionTagRepository _collectionTagRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<TagService> _logger;

    public TagService(
        ITagRepository tagRepository,
        ICollectionRepository collectionRepository,
        ICollectionTagRepository collectionTagRepository,
        IUserContextService userContextService,
        ILogger<TagService> logger)
    {
        _tagRepository = tagRepository;
        _collectionRepository = collectionRepository;
        _collectionTagRepository = collectionTagRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<IEnumerable<CollectionTagDto>> GetCollectionTagsAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Getting tags for collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        var collectionTags = await _collectionTagRepository.GetByCollectionIdAsync(collectionId);
        return collectionTags.Select(ct => new CollectionTagDto
        {
            Tag = ct.Tag.Name,
            Count = 1, // Assuming one tag per collection
            AddedBy = _userContextService.GetCurrentUserName(),
            AddedAt = ct.CreatedAt
        });
    }

    public async Task<CollectionTagDto> AddTagToCollectionAsync(ObjectId collectionId, AddTagToCollectionDto dto)
    {
        _logger.LogInformation("Adding tag {TagName} to collection: {CollectionId}", dto.TagName, collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        // Find or create tag
        var tag = await _tagRepository.GetByNameAsync(dto.TagName);
        if (tag == null)
        {
            var tagColor = dto.Color != null 
                ? new Domain.ValueObjects.TagColor(dto.Color.R, dto.Color.G, dto.Color.B)
                : Domain.ValueObjects.TagColor.Default;
            
            tag = new Tag(
                dto.TagName,
                dto.Description ?? string.Empty,
                tagColor
            );
            await _tagRepository.CreateAsync(tag);
        }

        // Create collection tag relationship
        var collectionTag = new CollectionTag(collectionId, tag.Id);
        await _collectionTagRepository.CreateAsync(collectionTag);
        // Repository automatically saves changes in MongoDB

        _logger.LogInformation("Tag {TagName} added to collection: {CollectionId}", dto.TagName, collectionId);

        return new CollectionTagDto
        {
            Tag = tag.Name,
            Count = 1,
            AddedBy = _userContextService.GetCurrentUserName(),
            AddedAt = collectionTag.CreatedAt
        };
    }

    public async Task RemoveTagFromCollectionAsync(ObjectId collectionId, string tagName)
    {
        _logger.LogInformation("Removing tag {TagName} from collection: {CollectionId}", tagName, collectionId);

        var tag = await _tagRepository.GetByNameAsync(tagName);
        if (tag == null)
        {
            throw new ArgumentException($"Tag {tagName} not found");
        }

        var collectionTag = await _collectionTagRepository.GetByCollectionIdAndTagIdAsync(collectionId, tag.Id);
        if (collectionTag == null)
        {
            throw new ArgumentException($"Tag {tagName} not found in collection {collectionId}");
        }

        await _collectionTagRepository.DeleteAsync(collectionTag.Id);
        // Repository automatically saves changes in MongoDB

        _logger.LogInformation("Tag {TagName} removed from collection: {CollectionId}", tagName, collectionId);
    }

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
    {
        _logger.LogInformation("Getting all tags");

        var tags = await _tagRepository.GetAllAsync();
        return tags.Select(t => new TagDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Color = new TagColorDto
            {
                R = t.Color.R,
                G = t.Color.G,
                B = t.Color.B
            },
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });
    }

    public async Task<TagDto> GetTagAsync(ObjectId tagId)
    {
        _logger.LogInformation("Getting tag: {TagId}", tagId);

        var tag = await _tagRepository.GetByIdAsync(tagId);
        if (tag == null)
        {
            throw new ArgumentException($"Tag with ID {tagId} not found");
        }

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            Color = new TagColorDto
            {
                R = tag.Color.R,
                G = tag.Color.G,
                B = tag.Color.B
            },
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt
        };
    }

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto)
    {
        _logger.LogInformation("Creating tag: {TagName}", dto.Name);

        var tag = new Tag(
            dto.Name,
            dto.Description,
            new Domain.ValueObjects.TagColor(dto.Color.R, dto.Color.G, dto.Color.B)
        );

        await _tagRepository.CreateAsync(tag);
        // Repository automatically saves changes in MongoDB

        _logger.LogInformation("Tag created with ID: {TagId}", tag.Id);

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            Color = new TagColorDto
            {
                R = tag.Color.R,
                G = tag.Color.G,
                B = tag.Color.B
            },
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt
        };
    }

    public async Task<TagDto> UpdateTagAsync(ObjectId tagId, UpdateTagDto dto)
    {
        _logger.LogInformation("Updating tag: {TagId}", tagId);

        var tag = await _tagRepository.GetByIdAsync(tagId);
        if (tag == null)
        {
            throw new ArgumentException($"Tag with ID {tagId} not found");
        }

        tag.UpdateName(dto.Name);
        tag.UpdateDescription(dto.Description);
        tag.UpdateColor(new Domain.ValueObjects.TagColor(dto.Color.R, dto.Color.G, dto.Color.B));

        await _tagRepository.UpdateAsync(tag);
        // Repository automatically saves changes in MongoDB

        _logger.LogInformation("Tag updated: {TagId}", tagId);

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            Color = new TagColorDto
            {
                R = tag.Color.R,
                G = tag.Color.G,
                B = tag.Color.B
            },
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt
        };
    }

    public async Task DeleteTagAsync(ObjectId tagId)
    {
        _logger.LogInformation("Deleting tag: {TagId}", tagId);

        var tag = await _tagRepository.GetByIdAsync(tagId);
        if (tag == null)
        {
            throw new ArgumentException($"Tag with ID {tagId} not found");
        }

        // Remove all collection tag relationships
        var collectionTags = await _collectionTagRepository.GetByTagIdAsync(tagId);
        foreach (var collectionTag in collectionTags)
        {
            await _collectionTagRepository.DeleteAsync(collectionTag.Id);
        }

        await _tagRepository.DeleteAsync(tag.Id);
        // Repository automatically saves changes in MongoDB

        _logger.LogInformation("Tag deleted: {TagId}", tagId);
    }

    public async Task<TagStatisticsDto> GetTagStatisticsAsync()
    {
        _logger.LogInformation("Getting tag statistics");

        var tags = await _tagRepository.GetAllAsync();
        var collectionTags = await _collectionTagRepository.GetAllAsync();

        var totalTags = tags.Count();
        var totalTagUsages = collectionTags.Count();
        var averageTagsPerCollection = totalTagUsages > 0 ? (double)totalTagUsages / totalTags : 0;

        var popularTags = collectionTags
            .GroupBy(ct => ct.TagId)
            .Select(g => new PopularTagDto
            {
                TagId = g.Key,
                TagName = g.First().Tag.Name,
                UsageCount = g.Count()
            })
            .OrderByDescending(pt => pt.UsageCount)
            .Take(10);

        return new TagStatisticsDto
        {
            TotalTags = totalTags,
            TotalTagUsages = totalTagUsages,
            AverageTagsPerCollection = averageTagsPerCollection,
            PopularTags = popularTags
        };
    }

    public async Task<IEnumerable<TagDto>> SearchTagsAsync(string query, int limit = 20)
    {
        _logger.LogInformation("Searching tags with query: {Query}", query);

        var tags = await _tagRepository.GetAllAsync();
        return tags
            .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       t.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Color = new TagColorDto
                {
                    R = t.Color.R,
                    G = t.Color.G,
                    B = t.Color.B
                },
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            });
    }

    public async Task<IEnumerable<PopularTagDto>> GetPopularTagsAsync(int limit = 20)
    {
        _logger.LogInformation("Getting popular tags");

        var collectionTags = await _collectionTagRepository.GetAllAsync();
        return collectionTags
            .GroupBy(ct => ct.TagId)
            .Select(g => new PopularTagDto
            {
                TagId = g.Key,
                TagName = g.First().Tag.Name,
                UsageCount = g.Count()
            })
            .OrderByDescending(pt => pt.UsageCount)
            .Take(limit);
    }

    public async Task<IEnumerable<TagSuggestionDto>> GetTagSuggestionsAsync(ObjectId collectionId, int limit = 10)
    {
        _logger.LogInformation("Getting tag suggestions for collection: {CollectionId}", collectionId);

        // Get tags from similar collections (collections with similar names or paths)
        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        var allCollections = await _collectionRepository.GetAllAsync();
        var similarCollections = allCollections
            .Where(c => c.Id != collectionId && 
                       (c.Name.Contains(collection.Name, StringComparison.OrdinalIgnoreCase) ||
                        c.Path.Contains(collection.Path, StringComparison.OrdinalIgnoreCase)))
            .Take(5);

        var suggestedTags = new List<TagSuggestionDto>();
        foreach (var similarCollection in similarCollections)
        {
            var collectionTags = await _collectionTagRepository.GetByCollectionIdAsync(similarCollection.Id);
            suggestedTags.AddRange(collectionTags.Select(ct => new TagSuggestionDto
            {
                TagName = ct.Tag.Name,
                Confidence = 0.8, // Mock confidence score
                Source = "similar_collection"
            }));
        }

        return suggestedTags
            .GroupBy(st => st.TagName)
            .Select(g => new TagSuggestionDto
            {
                TagName = g.Key,
                Confidence = g.Max(st => st.Confidence),
                Source = g.First().Source
            })
            .OrderByDescending(st => st.Confidence)
            .Take(limit);
    }

    public async Task<IEnumerable<CollectionTagDto>> BulkAddTagsToCollectionAsync(ObjectId collectionId, IEnumerable<string> tagNames)
    {
        _logger.LogInformation("Bulk adding tags to collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        var results = new List<CollectionTagDto>();
        foreach (var tagName in tagNames)
        {
            try
            {
                var dto = new AddTagToCollectionDto
                {
                    TagName = tagName,
                    Description = string.Empty,
                    Color = new TagColorDto { R = 128, G = 128, B = 128 } // Default gray
                };
                var result = await AddTagToCollectionAsync(collectionId, dto);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to add tag {TagName} to collection {CollectionId}: {Error}", 
                    tagName, collectionId, ex.Message);
            }
        }

        _logger.LogInformation("Bulk added {Count} tags to collection: {CollectionId}", results.Count, collectionId);
        return results;
    }

    public async Task BulkRemoveTagsFromCollectionAsync(ObjectId collectionId, IEnumerable<string> tagNames)
    {
        _logger.LogInformation("Bulk removing tags from collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        foreach (var tagName in tagNames)
        {
            try
            {
                await RemoveTagFromCollectionAsync(collectionId, tagName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to remove tag {TagName} from collection {CollectionId}: {Error}", 
                    tagName, collectionId, ex.Message);
            }
        }

        _logger.LogInformation("Bulk removed tags from collection: {CollectionId}", collectionId);
    }
}
