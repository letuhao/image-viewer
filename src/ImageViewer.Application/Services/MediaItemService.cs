using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for MediaItem operations
/// </summary>
public class MediaItemService : IMediaItemService
{
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly ILogger<MediaItemService> _logger;

    public MediaItemService(IMediaItemRepository mediaItemRepository, ILogger<MediaItemService> logger)
    {
        _mediaItemRepository = mediaItemRepository ?? throw new ArgumentNullException(nameof(mediaItemRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MediaItem> CreateMediaItemAsync(ObjectId collectionId, string name, string filename, string path, 
        string type, string format, long fileSize, int width, int height, TimeSpan? duration = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Media item name cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(filename))
                throw new ValidationException("Media item filename cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(path))
                throw new ValidationException("Media item path cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(type))
                throw new ValidationException("Media item type cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(format))
                throw new ValidationException("Media item format cannot be null or empty");
            
            if (fileSize <= 0)
                throw new ValidationException("File size must be greater than 0");
            
            if (width <= 0)
                throw new ValidationException("Width must be greater than 0");
            
            if (height <= 0)
                throw new ValidationException("Height must be greater than 0");

            // Check if media item already exists at this path
            var existingMediaItem = await _mediaItemRepository.GetByPathAsync(path);
            if (existingMediaItem != null)
                throw new DuplicateEntityException($"Media item at path '{path}' already exists");

            // Create new media item
            var mediaItem = new MediaItem(collectionId, name, filename, path, type, format, fileSize, width, height, duration);
            return await _mediaItemRepository.CreateAsync(mediaItem);
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to create media item with name {Name} at path {Path}", name, path);
            throw new BusinessRuleException($"Failed to create media item with name '{name}' at path '{path}'", ex);
        }
    }

    public async Task<MediaItem> GetMediaItemByIdAsync(ObjectId mediaItemId)
    {
        try
        {
            var mediaItem = await _mediaItemRepository.GetByIdAsync(mediaItemId);
            if (mediaItem == null)
                throw new EntityNotFoundException($"Media item with ID '{mediaItemId}' not found");
            
            return mediaItem;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to get media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task<MediaItem> GetMediaItemByPathAsync(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ValidationException("Media item path cannot be null or empty");

            var mediaItem = await _mediaItemRepository.GetByPathAsync(path);
            if (mediaItem == null)
                throw new EntityNotFoundException($"Media item at path '{path}' not found");
            
            return mediaItem;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get media item at path {Path}", path);
            throw new BusinessRuleException($"Failed to get media item at path '{path}'", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByCollectionIdAsync(ObjectId collectionId)
    {
        try
        {
            return await _mediaItemRepository.GetByCollectionIdAsync(collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media items for collection {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to get media items for collection '{collectionId}'", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _mediaItemRepository.FindAsync(
                Builders<MediaItem>.Filter.Empty,
                Builders<MediaItem>.Sort.Descending(m => m.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get media items for page {Page} with page size {PageSize}", page, pageSize);
            throw new BusinessRuleException($"Failed to get media items for page {page}", ex);
        }
    }

    public async Task<MediaItem> UpdateMediaItemAsync(ObjectId mediaItemId, UpdateMediaItemRequest request)
    {
        try
        {
            var mediaItem = await GetMediaItemByIdAsync(mediaItemId);
            
            if (request.Name != null)
            {
                mediaItem.UpdateName(request.Name);
            }
            
            if (request.Filename != null)
            {
                mediaItem.UpdateFilename(request.Filename);
            }
            
            if (request.Path != null)
            {
                // Check if path is already taken by another media item
                var existingMediaItem = await _mediaItemRepository.GetByPathAsync(request.Path);
                if (existingMediaItem != null && existingMediaItem.Id != mediaItemId)
                    throw new DuplicateEntityException($"Media item at path '{request.Path}' already exists");
                
                mediaItem.UpdatePath(request.Path);
            }
            
            if (request.Width.HasValue && request.Height.HasValue)
            {
                mediaItem.UpdateDimensions(request.Width.Value, request.Height.Value);
            }
            
            if (request.Duration.HasValue)
            {
                mediaItem.UpdateDuration(request.Duration.Value);
            }
            
            return await _mediaItemRepository.UpdateAsync(mediaItem);
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to update media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to update media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task DeleteMediaItemAsync(ObjectId mediaItemId)
    {
        try
        {
            var mediaItem = await GetMediaItemByIdAsync(mediaItemId);
            await _mediaItemRepository.DeleteAsync(mediaItemId);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to delete media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to delete media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task<MediaItem> UpdateMetadataAsync(ObjectId mediaItemId, UpdateMediaItemMetadataRequest request)
    {
        try
        {
            var mediaItem = await GetMediaItemByIdAsync(mediaItemId);
            
            var newMetadata = new MediaMetadata();
            
            if (request.Title != null)
                newMetadata.UpdateTitle(request.Title);
            
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
            
            if (request.ExifData != null)
            {
                foreach (var exif in request.ExifData)
                {
                    newMetadata.AddExifData(exif.Key, exif.Value);
                }
            }
            
            if (request.ColorProfile != null)
                newMetadata.UpdateColorProfile(request.ColorProfile);
            
            if (request.BitDepth.HasValue)
                newMetadata.UpdateBitDepth(request.BitDepth.Value);
            
            if (request.Compression != null)
                newMetadata.UpdateCompression(request.Compression);
            
            if (request.CreatedDate.HasValue)
                newMetadata.UpdateCreatedDate(request.CreatedDate.Value);
            
            if (request.ModifiedDate.HasValue)
                newMetadata.UpdateModifiedDate(request.ModifiedDate.Value);
            
            if (request.CameraInfo != null)
                newMetadata.UpdateCameraInfo(request.CameraInfo);
            
            mediaItem.UpdateMetadata(newMetadata);
            return await _mediaItemRepository.UpdateAsync(mediaItem);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update metadata for media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to update metadata for media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task<MediaItem> UpdateCacheInfoAsync(ObjectId mediaItemId, UpdateCacheInfoRequest request)
    {
        try
        {
            var mediaItem = await GetMediaItemByIdAsync(mediaItemId);
            
            var newCacheInfo = new CacheInfo();
            
            if (request.IsCached.HasValue && request.IsCached.Value)
            {
                if (string.IsNullOrWhiteSpace(request.CachePath))
                    throw new ValidationException("Cache path is required when setting cached to true");
                
                if (!request.CacheSize.HasValue)
                    throw new ValidationException("Cache size is required when setting cached to true");
                
                if (string.IsNullOrWhiteSpace(request.CacheFormat))
                    throw new ValidationException("Cache format is required when setting cached to true");
                
                if (!request.CacheQuality.HasValue)
                    throw new ValidationException("Cache quality is required when setting cached to true");
                
                if (!request.CacheWidth.HasValue)
                    throw new ValidationException("Cache width is required when setting cached to true");
                
                if (!request.CacheHeight.HasValue)
                    throw new ValidationException("Cache height is required when setting cached to true");
                
                newCacheInfo.SetCached(
                    request.CachePath,
                    request.CacheSize.Value,
                    request.CacheFormat,
                    request.CacheQuality.Value,
                    request.CacheWidth.Value,
                    request.CacheHeight.Value,
                    request.CompressionLevel ?? 6
                );
            }
            else if (request.IsCached.HasValue && !request.IsCached.Value)
            {
                newCacheInfo.ClearCache();
            }
            
            mediaItem.UpdateCacheInfo(newCacheInfo);
            return await _mediaItemRepository.UpdateAsync(mediaItem);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException || ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to update cache info for media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to update cache info for media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task<MediaItem> UpdateStatisticsAsync(ObjectId mediaItemId, UpdateMediaItemStatisticsRequest request)
    {
        try
        {
            var mediaItem = await GetMediaItemByIdAsync(mediaItemId);
            
            var newStatistics = new MediaStatistics();
            
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
            
            if (request.TotalRatings.HasValue && request.AverageRating.HasValue)
            {
                // This would need to be calculated based on the rating system
                // For now, we'll just update the values directly
                newStatistics.IncrementViews(0); // Trigger update
            }
            
            if (request.LastViewed.HasValue)
                newStatistics.UpdateLastViewed();
            
            if (request.LastDownloaded.HasValue)
                newStatistics.UpdateLastDownloaded();
            
            if (request.LastShared.HasValue)
                newStatistics.UpdateLastShared();
            
            if (request.LastLiked.HasValue)
                newStatistics.UpdateLastLiked();
            
            if (request.LastCommented.HasValue)
                newStatistics.UpdateLastCommented();
            
            if (request.LastRated.HasValue)
                newStatistics.UpdateLastRated();
            
            mediaItem.UpdateStatistics(newStatistics);
            return await _mediaItemRepository.UpdateAsync(mediaItem);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update statistics for media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to update statistics for media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task<MediaItem> ActivateMediaItemAsync(ObjectId mediaItemId)
    {
        try
        {
            var mediaItem = await GetMediaItemByIdAsync(mediaItemId);
            mediaItem.Activate();
            return await _mediaItemRepository.UpdateAsync(mediaItem);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to activate media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to activate media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task<MediaItem> DeactivateMediaItemAsync(ObjectId mediaItemId)
    {
        try
        {
            var mediaItem = await GetMediaItemByIdAsync(mediaItemId);
            mediaItem.Deactivate();
            return await _mediaItemRepository.UpdateAsync(mediaItem);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to deactivate media item with ID {MediaItemId}", mediaItemId);
            throw new BusinessRuleException($"Failed to deactivate media item with ID '{mediaItemId}'", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> SearchMediaItemsAsync(string query, int page = 1, int pageSize = 20)
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
            return await _mediaItemRepository.FindAsync(
                Builders<MediaItem>.Filter.Or(
                    Builders<MediaItem>.Filter.Regex(m => m.Name, new BsonRegularExpression(query, "i")),
                    Builders<MediaItem>.Filter.Regex(m => m.Filename, new BsonRegularExpression(query, "i")),
                    Builders<MediaItem>.Filter.Regex(m => m.Path, new BsonRegularExpression(query, "i"))
                ),
                Builders<MediaItem>.Sort.Descending(m => m.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to search media items with query {Query}", query);
            throw new BusinessRuleException($"Failed to search media items with query '{query}'", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByFilterAsync(MediaItemFilterRequest filter, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var mediaItemFilter = new MediaItemFilter
            {
                CollectionId = filter.CollectionId,
                Type = filter.Type,
                Format = filter.Format,
                IsActive = filter.IsActive,
                CreatedAfter = filter.CreatedAfter,
                CreatedBefore = filter.CreatedBefore,
                Path = filter.Path,
                Tags = filter.Tags,
                Categories = filter.Categories,
                MinWidth = filter.MinWidth,
                MaxWidth = filter.MaxWidth,
                MinHeight = filter.MinHeight,
                MaxHeight = filter.MaxHeight,
                MinFileSize = filter.MinFileSize,
                MaxFileSize = filter.MaxFileSize
            };

            var skip = (page - 1) * pageSize;
            return await _mediaItemRepository.FindAsync(
                Builders<MediaItem>.Filter.Empty,
                Builders<MediaItem>.Sort.Descending(m => m.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get media items by filter");
            throw new BusinessRuleException("Failed to get media items by filter", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByCollectionAsync(ObjectId collectionId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _mediaItemRepository.FindAsync(
                Builders<MediaItem>.Filter.Eq(m => m.CollectionId, collectionId),
                Builders<MediaItem>.Sort.Descending(m => m.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get media items for collection {CollectionId}", collectionId);
            throw new BusinessRuleException($"Failed to get media items for collection '{collectionId}'", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByTypeAsync(string type, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _mediaItemRepository.FindAsync(
                Builders<MediaItem>.Filter.Eq(m => m.Type, type),
                Builders<MediaItem>.Sort.Descending(m => m.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get media items by type {Type}", type);
            throw new BusinessRuleException($"Failed to get media items by type '{type}'", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByFormatAsync(string format, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _mediaItemRepository.FindAsync(
                Builders<MediaItem>.Filter.Eq(m => m.Format, format),
                Builders<MediaItem>.Sort.Descending(m => m.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get media items by format {Format}", format);
            throw new BusinessRuleException($"Failed to get media items by format '{format}'", ex);
        }
    }

    public async Task<MediaItemStatistics> GetMediaItemStatisticsAsync()
    {
        try
        {
            return await _mediaItemRepository.GetMediaItemStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media item statistics");
            throw new BusinessRuleException("Failed to get media item statistics", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetTopMediaItemsByActivityAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _mediaItemRepository.GetTopMediaItemsByActivityAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get top media items by activity");
            throw new BusinessRuleException("Failed to get top media items by activity", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetRecentMediaItemsAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _mediaItemRepository.GetRecentMediaItemsAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get recent media items");
            throw new BusinessRuleException("Failed to get recent media items", ex);
        }
    }
}
