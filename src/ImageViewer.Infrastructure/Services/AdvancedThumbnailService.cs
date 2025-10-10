using MongoDB.Bson;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.Services;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Advanced thumbnail service implementation using embedded design
/// Refactored to use ThumbnailEmbedded instead of separate ThumbnailInfo entity
/// </summary>
public class AdvancedThumbnailService : IAdvancedThumbnailService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ILogger<AdvancedThumbnailService> _logger;

    public AdvancedThumbnailService(
        ICollectionRepository collectionRepository,
        IImageProcessingService imageProcessingService,
        ICacheFolderRepository cacheFolderRepository,
        ILogger<AdvancedThumbnailService> logger)
    {
        _collectionRepository = collectionRepository ?? throw new ArgumentNullException(nameof(collectionRepository));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _cacheFolderRepository = cacheFolderRepository ?? throw new ArgumentNullException(nameof(cacheFolderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> GenerateCollectionThumbnailAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating thumbnail for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                _logger.LogWarning("Collection not found: {CollectionId}", collectionId);
                return null;
            }

            var activeImages = collection.GetActiveImages();
            if (!activeImages.Any())
            {
                _logger.LogWarning("No images found in collection: {CollectionId}", collectionId);
                return null;
            }

            // Get the first image or a random one for collection thumbnail
            var sourceImage = activeImages.First();
            
            // Find or create thumbnail for this image
            var existingThumbnail = collection.GetThumbnailForImage(sourceImage.Id);
            if (existingThumbnail != null && File.Exists(existingThumbnail.ThumbnailPath))
            {
                _logger.LogInformation("Using existing thumbnail for collection {CollectionId}: {ThumbnailPath}", 
                    collectionId, existingThumbnail.ThumbnailPath);
                return existingThumbnail.ThumbnailPath;
            }

            // Generate new thumbnail
            var sourcePath = Path.Combine(collection.Path, sourceImage.RelativePath);
            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Source image not found: {SourcePath}", sourcePath);
                return null;
            }

            // Get cache folder for thumbnail storage
            var cacheFolders = await _cacheFolderRepository.GetActiveOrderedByPriorityAsync();
            var cacheFolder = cacheFolders.FirstOrDefault();
            if (cacheFolder == null)
            {
                _logger.LogWarning("No active cache folders available for thumbnail generation");
                return null;
            }

            // Generate thumbnail path
            var thumbnailDir = Path.Combine(cacheFolder.Path, "thumbnails", collectionId.ToString());
            Directory.CreateDirectory(thumbnailDir);
            var thumbnailPath = Path.Combine(thumbnailDir, $"{sourceImage.Id}_300x300.jpg");

            // Generate thumbnail using image processing service
            var thumbnailData = await _imageProcessingService.GenerateThumbnailAsync(
                sourcePath, 300, 300, "jpeg", 95, cancellationToken);

            if (thumbnailData == null || thumbnailData.Length == 0)
            {
                _logger.LogWarning("Failed to generate thumbnail data for {SourcePath}", sourcePath);
                return null;
            }

            // Save thumbnail to disk
            await File.WriteAllBytesAsync(thumbnailPath, thumbnailData, cancellationToken);

            // Create ThumbnailEmbedded and add to collection
            var thumbnail = new ThumbnailEmbedded(
                sourceImage.Id,
                thumbnailPath,
                300,
                300,
                thumbnailData.Length,
                "jpg",
                95);

            collection.AddThumbnail(thumbnail);
            await _collectionRepository.UpdateAsync(collection);

            _logger.LogInformation("Generated thumbnail for collection {CollectionId}: {ThumbnailPath}", 
                collectionId, thumbnailPath);
            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<BatchThumbnailResult> BatchRegenerateThumbnailsAsync(IEnumerable<ObjectId> collectionIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting batch thumbnail regeneration for {Count} collections", collectionIds.Count());

        var result = new BatchThumbnailResult
        {
            Total = collectionIds.Count()
        };

        foreach (var collectionId in collectionIds)
        {
            try
            {
                var thumbnailPath = await GenerateCollectionThumbnailAsync(collectionId, cancellationToken);
                if (thumbnailPath != null)
                {
                    result.Success++;
                    result.SuccessfulCollections.Add(collectionId);
                }
                else
                {
                    result.Failed++;
                    result.FailedCollections.Add(collectionId);
                    result.Errors.Add($"Failed to generate thumbnail for collection {collectionId}");
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.FailedCollections.Add(collectionId);
                result.Errors.Add($"Collection {collectionId}: {ex.Message}");
                _logger.LogError(ex, "Error generating thumbnail for collection {CollectionId}", collectionId);
            }
        }

        _logger.LogInformation("Batch thumbnail regeneration completed. Success: {Success}, Failed: {Failed}", 
            result.Success, result.Failed);
        return result;
    }

    public async Task<byte[]?> GetCollectionThumbnailAsync(ObjectId collectionId, int? width = null, int? height = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting thumbnail for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                _logger.LogWarning("Collection not found: {CollectionId}", collectionId);
                return null;
            }

            // Find any thumbnail for this collection
            var validThumbnails = collection.GetValidThumbnails();
            if (!validThumbnails.Any())
            {
                _logger.LogDebug("No thumbnails found for collection: {CollectionId}", collectionId);
                return null;
            }

            // Get the first valid thumbnail
            var thumbnail = validThumbnails.First();
            if (!File.Exists(thumbnail.ThumbnailPath))
            {
                _logger.LogWarning("Thumbnail file not found: {ThumbnailPath}", thumbnail.ThumbnailPath);
                collection.MarkThumbnailAsInvalid(thumbnail.Id);
                await _collectionRepository.UpdateAsync(collection);
                return null;
            }

            // Read and return thumbnail data
            var thumbnailData = await File.ReadAllBytesAsync(thumbnail.ThumbnailPath, cancellationToken);
            
            // Update access time
            collection.UpdateThumbnailAccess(thumbnail.Id);
            await _collectionRepository.UpdateAsync(collection);

            return thumbnailData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail for collection: {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task DeleteCollectionThumbnailAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting thumbnail for collection: {CollectionId}", collectionId);

        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new KeyNotFoundException($"Collection with ID {collectionId} not found");
            }

            // Delete all thumbnails for this collection
            var thumbnails = collection.Thumbnails.ToList();
            foreach (var thumbnail in thumbnails)
            {
                // Delete physical file
                if (File.Exists(thumbnail.ThumbnailPath))
                {
                    try
                    {
                        File.Delete(thumbnail.ThumbnailPath);
                        _logger.LogDebug("Deleted thumbnail file: {ThumbnailPath}", thumbnail.ThumbnailPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete thumbnail file: {ThumbnailPath}", thumbnail.ThumbnailPath);
                    }
                }

                // Remove from collection
                collection.RemoveThumbnail(thumbnail.Id);
            }

            await _collectionRepository.UpdateAsync(collection);
            _logger.LogInformation("Deleted {Count} thumbnails for collection: {CollectionId}", thumbnails.Count, collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting thumbnail for collection: {CollectionId}", collectionId);
            throw;
        }
    }
}

