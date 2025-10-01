using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.DTOs.Common;
using ImageViewer.Application.Extensions;

namespace ImageViewer.Application.Services;

/// <summary>
/// Collection service implementation
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileScannerService _fileScannerService;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(
        IUnitOfWork unitOfWork,
        IFileScannerService fileScannerService,
        ILogger<CollectionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileScannerService = fileScannerService ?? throw new ArgumentNullException(nameof(fileScannerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collection by ID {CollectionId}", id);
            return await _unitOfWork.Collections.GetByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by ID {CollectionId}", id);
            throw;
        }
    }

    public async Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collection by name {CollectionName}", name);
            return await _unitOfWork.Collections.GetByNameAsync(name, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by name {CollectionName}", name);
            throw;
        }
    }

    public async Task<Collection?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collection by path {CollectionPath}", path);
            return await _unitOfWork.Collections.GetByPathAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by path {CollectionPath}", path);
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all collections");
            return await _unitOfWork.Collections.GetActiveCollectionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all collections");
            throw;
        }
    }

    public async Task<PaginationResponseDto<Collection>> GetCollectionsAsync(PaginationRequestDto pagination, string? search = null, string? type = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collections with pagination. Page: {Page}, PageSize: {PageSize}, Search: {Search}, Type: {Type}", 
                pagination.Page, pagination.PageSize, search, type);

            var collections = await _unitOfWork.Collections.GetActiveCollectionsQueryableAsync(cancellationToken);
            var query = collections.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Path.Contains(search));
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<CollectionType>(type, true, out var collectionType))
            {
                query = query.Where(c => c.Type == collectionType);
            }

            // Apply sorting
            query = query.ApplySorting(pagination.SortBy, pagination.SortDirection);

            // Get total count before pagination
            var totalCount = query.Count();
            
            // Apply pagination
            var paginatedData = query.ApplyPagination(pagination);
            
            // Create response
            var result = paginatedData.ToPaginationResponse(totalCount, pagination);
            
            _logger.LogDebug("Retrieved {Count} collections out of {Total} total", 
                result.Data.Count(), result.TotalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections with pagination");
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetByTypeAsync(CollectionType type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collections by type {CollectionType}", type);
            return await _unitOfWork.Collections.GetByTypeAsync(type, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections by type {CollectionType}", type);
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Searching collections by name {SearchTerm}", searchTerm);
            return await _unitOfWork.Collections.SearchByNameAsync(searchTerm, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching collections by name {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsWithImagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collections with images");
            return await _unitOfWork.Collections.GetCollectionsWithImagesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections with images");
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collections by tag {TagName}", tagName);
            return await _unitOfWork.Collections.GetCollectionsByTagAsync(tagName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections by tag {TagName}", tagName);
            throw;
        }
    }

    public async Task<Collection> CreateAsync(string name, string path, CollectionType type, CollectionSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating collection {CollectionName} at path {CollectionPath}", name, path);

            // Check if collection already exists
            var existingCollection = await _unitOfWork.Collections.GetByPathAsync(path, cancellationToken);
            if (existingCollection != null)
            {
                throw new InvalidOperationException($"Collection with path '{path}' already exists");
            }

            // Validate path
            var isValidPath = await _fileScannerService.IsValidCollectionPathAsync(path, cancellationToken);
            if (!isValidPath)
            {
                throw new ArgumentException($"Invalid collection path: {path}");
            }

            // Create collection
            var collection = new Collection(name, path, type);
        if (settings != null)
        {
            var settingsEntity = new CollectionSettingsEntity(
                collection.Id,
                settings.TotalImages,
                settings.TotalSizeBytes,
                settings.ThumbnailWidth,
                settings.ThumbnailHeight,
                settings.CacheWidth,
                settings.CacheHeight,
                settings.AutoGenerateThumbnails,
                settings.AutoGenerateCache,
                settings.CacheExpiration,
                settings.AdditionalSettingsJson
            );
            collection.SetSettings(settingsEntity);
        }
            await _unitOfWork.Collections.AddAsync(collection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created collection {CollectionId} with name {CollectionName}", collection.Id, name);
            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection {CollectionName} at path {CollectionPath}", name, path);
            throw;
        }
    }

    public async Task<Collection> UpdateAsync(Guid id, string? name = null, string? path = null, CollectionSettings? settings = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating collection {CollectionId}", id);

            var collection = await _unitOfWork.Collections.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{id}' not found");
            }

            if (name != null)
            {
                collection.UpdateName(name);
            }

            if (path != null)
            {
                collection.UpdatePath(path);
            }

        if (settings != null)
        {
            var settingsEntity = new CollectionSettingsEntity(
                collection.Id,
                settings.TotalImages,
                settings.TotalSizeBytes,
                settings.ThumbnailWidth,
                settings.ThumbnailHeight,
                settings.CacheWidth,
                settings.CacheHeight,
                settings.AutoGenerateThumbnails,
                settings.AutoGenerateCache,
                settings.CacheExpiration,
                settings.AdditionalSettingsJson
            );
            collection.SetSettings(settingsEntity);
        }

            await _unitOfWork.Collections.UpdateAsync(collection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated collection {CollectionId}", id);
            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {CollectionId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting collection {CollectionId}", id);

            var collection = await _unitOfWork.Collections.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{id}' not found");
            }

            collection.SoftDelete();
            await _unitOfWork.Collections.UpdateAsync(collection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted collection {CollectionId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {CollectionId}", id);
            throw;
        }
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring collection {CollectionId}", id);

            var collection = await _unitOfWork.Collections.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{id}' not found");
            }

            collection.Restore();
            await _unitOfWork.Collections.UpdateAsync(collection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully restored collection {CollectionId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring collection {CollectionId}", id);
            throw;
        }
    }

    public async Task ScanCollectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scanning collection {CollectionId}", id);

            var collection = await _unitOfWork.Collections.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{id}' not found");
            }

            // Scan for images
            IEnumerable<Image> images;
            if (collection.Type == CollectionType.Folder)
            {
                images = await _fileScannerService.ScanFolderAsync(collection.Path, cancellationToken);
            }
            else
            {
                images = await _fileScannerService.ScanArchiveAsync(collection.Path, collection.Type, cancellationToken);
            }

            // Add images to collection
            foreach (var image in images)
            {
                collection.AddImage(image);
            }

            // Update collection settings with scan results
            var settings = collection.Settings;
            if (settings != null)
            {
                settings.UpdateTotalImages(collection.GetImageCount());
                settings.UpdateTotalSize(collection.GetTotalSize());
            }
            else
            {
                var settingsEntity = new CollectionSettingsEntity(
                    collection.Id,
                    collection.GetImageCount(),
                    collection.GetTotalSize(),
                    300, 300, // thumbnail size
                    1920, 1080, // cache size
                    true, true, // auto generate
                    TimeSpan.FromDays(30),
                    "{}"
                );
                collection.SetSettings(settingsEntity);
            }

            await _unitOfWork.Collections.UpdateAsync(collection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully scanned collection {CollectionId}, found {ImageCount} images", id, collection.GetImageCount());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning collection {CollectionId}", id);
            throw;
        }
    }

    public async Task<CollectionStatistics> GetStatisticsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting statistics for collection {CollectionId}", id);

            var collection = await _unitOfWork.Collections.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{id}' not found");
            }

            var statistics = collection.Statistics ?? new CollectionStatistics(id);
            
            // Update statistics
            statistics.UpdateImageCount(collection.GetImageCount());
            statistics.UpdateTotalSize(collection.GetTotalSize());
            
            var images = collection.Images.ToList();
            if (images.Any())
            {
                var avgWidth = (int)images.Average(i => i.Width);
                var avgHeight = (int)images.Average(i => i.Height);
                statistics.UpdateAverageDimensions(avgWidth, avgHeight);
            }

            await _unitOfWork.CollectionStatistics.UpdateAsync(statistics, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for collection {CollectionId}", id);
            throw;
        }
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting total size of all collections");
            return await _unitOfWork.Collections.GetTotalSizeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total size");
            throw;
        }
    }

    public async Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting total image count");
            return await _unitOfWork.Collections.GetTotalImageCountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total image count");
            throw;
        }
    }

    public async Task AddTagAsync(Guid collectionId, string tagName, string? description = null, TagColor? color = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding tag {TagName} to collection {CollectionId}", tagName, collectionId);

            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection with ID '{collectionId}' not found");
            }

            // Find or create tag
            var tag = await _unitOfWork.Tags.FirstOrDefaultAsync(t => t.Name == tagName, cancellationToken);
            if (tag == null)
            {
                tag = new Tag(tagName, description ?? "", color);
                await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
            }

            // Create collection tag relationship
            var collectionTag = new CollectionTag(collectionId, tag.Id);
            await _unitOfWork.CollectionTags.AddAsync(collectionTag, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added tag {TagName} to collection {CollectionId}", tagName, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag {TagName} to collection {CollectionId}", tagName, collectionId);
            throw;
        }
    }

    public async Task RemoveTagAsync(Guid collectionId, string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing tag {TagName} from collection {CollectionId}", tagName, collectionId);

            var tag = await _unitOfWork.Tags.FirstOrDefaultAsync(t => t.Name == tagName, cancellationToken);
            if (tag == null)
            {
                throw new InvalidOperationException($"Tag '{tagName}' not found");
            }

            var collectionTag = await _unitOfWork.CollectionTags.FirstOrDefaultAsync(
                ct => ct.CollectionId == collectionId && ct.TagId == tag.Id, cancellationToken);
            
            if (collectionTag == null)
            {
                throw new InvalidOperationException($"Collection '{collectionId}' does not have tag '{tagName}'");
            }

            await _unitOfWork.CollectionTags.DeleteAsync(collectionTag, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully removed tag {TagName} from collection {CollectionId}", tagName, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag {TagName} from collection {CollectionId}", tagName, collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> GetTagsAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting tags for collection {CollectionId}", collectionId);

            var collectionTags = await _unitOfWork.CollectionTags.FindAsync(
                ct => ct.CollectionId == collectionId, cancellationToken);

            var tagIds = collectionTags.Select(ct => ct.TagId).ToList();
            var tags = await _unitOfWork.Tags.FindAsync(t => tagIds.Contains(t.Id), cancellationToken);

            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags for collection {CollectionId}", collectionId);
            throw;
        }
    }
}

