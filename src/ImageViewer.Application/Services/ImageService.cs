using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.Services;

/// <summary>
/// Image service implementation
/// </summary>
public class ImageService : IImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        IUnitOfWork unitOfWork,
        IImageProcessingService imageProcessingService,
        ICacheService cacheService,
        ILogger<ImageService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting image by ID {ImageId}", id);
            return await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image by ID {ImageId}", id);
            throw;
        }
    }

    public async Task<Image?> GetByCollectionIdAndFilenameAsync(Guid collectionId, string filename, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting image by collection {CollectionId} and filename {Filename}", collectionId, filename);
            return await _unitOfWork.Images.GetByCollectionIdAndFilenameAsync(collectionId, filename, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image by collection {CollectionId} and filename {Filename}", collectionId, filename);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetByCollectionIdAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting images by collection {CollectionId}", collectionId);
            return await _unitOfWork.Images.GetByCollectionIdAsync(collectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images by collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetByFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting images by format {Format}", format);
            return await _unitOfWork.Images.GetByFormatAsync(format, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images by format {Format}", format);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting images by size range {MinWidth}x{MinHeight}", minWidth, minHeight);
            return await _unitOfWork.Images.GetBySizeRangeAsync(minWidth, minHeight, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images by size range {MinWidth}x{MinHeight}", minWidth, minHeight);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting large images with minimum size {MinSizeBytes}", minSizeBytes);
            return await _unitOfWork.Images.GetLargeImagesAsync(minSizeBytes, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting large images with minimum size {MinSizeBytes}", minSizeBytes);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting high resolution images {MinWidth}x{MinHeight}", minWidth, minHeight);
            return await _unitOfWork.Images.GetHighResolutionImagesAsync(minWidth, minHeight, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting high resolution images {MinWidth}x{MinHeight}", minWidth, minHeight);
            throw;
        }
    }

    public async Task<Image?> GetRandomImageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting random image");
            return await _unitOfWork.Images.GetRandomImageAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random image");
            throw;
        }
    }

    public async Task<Image?> GetRandomImageByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting random image by collection {CollectionId}", collectionId);
            return await _unitOfWork.Images.GetRandomImageByCollectionAsync(collectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random image by collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<Image?> GetNextImageAsync(Guid currentImageId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting next image for {CurrentImageId}", currentImageId);
            return await _unitOfWork.Images.GetNextImageAsync(currentImageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next image for {CurrentImageId}", currentImageId);
            throw;
        }
    }

    public async Task<Image?> GetPreviousImageAsync(Guid currentImageId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting previous image for {CurrentImageId}", currentImageId);
            return await _unitOfWork.Images.GetPreviousImageAsync(currentImageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting previous image for {CurrentImageId}", currentImageId);
            throw;
        }
    }

    public async Task<byte[]?> GetImageFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting image file for {ImageId}", id);

            var image = await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                _logger.LogWarning("Image {ImageId} not found", id);
                return null;
            }

            var collection = await _unitOfWork.Collections.GetByIdAsync(image.CollectionId, cancellationToken);
            if (collection == null)
            {
                _logger.LogWarning("Collection for image {ImageId} not found", id);
                return null;
            }

            var fullPath = Path.Combine(collection.Path, image.RelativePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Image file not found at path {FilePath}", fullPath);
                return null;
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image file for {ImageId}", id);
            throw;
        }
    }

    public async Task<byte[]?> GetThumbnailAsync(Guid id, int? width = null, int? height = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting thumbnail for {ImageId} with size {Width}x{Height}", id, width, height);

            var image = await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                _logger.LogWarning("Image {ImageId} not found", id);
                return null;
            }

            var thumbnailWidth = width ?? 300;
            var thumbnailHeight = height ?? 300;
            var dimensions = $"{thumbnailWidth}x{thumbnailHeight}";

            // Check cache first
            var cachedThumbnail = await _cacheService.GetCachedImageAsync(id, dimensions, cancellationToken);
            if (cachedThumbnail != null)
            {
                _logger.LogDebug("Returning cached thumbnail for {ImageId}", id);
                return cachedThumbnail;
            }

            // Generate thumbnail
            var collection = await _unitOfWork.Collections.GetByIdAsync(image.CollectionId, cancellationToken);
            if (collection == null)
            {
                _logger.LogWarning("Collection for image {ImageId} not found", id);
                return null;
            }

            var fullPath = Path.Combine(collection.Path, image.RelativePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Image file not found at path {FilePath}", fullPath);
                return null;
            }

            var thumbnailData = await _imageProcessingService.GenerateThumbnailAsync(fullPath, thumbnailWidth, thumbnailHeight, cancellationToken);
            
            // Cache the thumbnail
            await _cacheService.SaveCachedImageAsync(id, dimensions, thumbnailData, cancellationToken);

            _logger.LogDebug("Generated and cached thumbnail for {ImageId}", id);
            return thumbnailData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail for {ImageId}", id);
            throw;
        }
    }

    public async Task<byte[]?> GetCachedImageAsync(Guid id, int? width = null, int? height = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting cached image for {ImageId} with size {Width}x{Height}", id, width, height);

            var image = await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                _logger.LogWarning("Image {ImageId} not found", id);
                return null;
            }

            var cacheWidth = width ?? 1920;
            var cacheHeight = height ?? 1080;
            var dimensions = $"{cacheWidth}x{cacheHeight}";

            // Check cache first
            var cachedImage = await _cacheService.GetCachedImageAsync(id, dimensions, cancellationToken);
            if (cachedImage != null)
            {
                _logger.LogDebug("Returning cached image for {ImageId}", id);
                return cachedImage;
            }

            // Generate cached image
            var collection = await _unitOfWork.Collections.GetByIdAsync(image.CollectionId, cancellationToken);
            if (collection == null)
            {
                _logger.LogWarning("Collection for image {ImageId} not found", id);
                return null;
            }

            var fullPath = Path.Combine(collection.Path, image.RelativePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Image file not found at path {FilePath}", fullPath);
                return null;
            }

            var cachedImageData = await _imageProcessingService.ResizeImageAsync(fullPath, cacheWidth, cacheHeight, 95, cancellationToken);
            
            // Cache the image
            await _cacheService.SaveCachedImageAsync(id, dimensions, cachedImageData, cancellationToken);

            _logger.LogDebug("Generated and cached image for {ImageId}", id);
            return cachedImageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached image for {ImageId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting image {ImageId}", id);

            var image = await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                throw new InvalidOperationException($"Image with ID '{id}' not found");
            }

            image.SoftDelete();
            await _unitOfWork.Images.UpdateAsync(image, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted image {ImageId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId}", id);
            throw;
        }
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring image {ImageId}", id);

            var image = await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                throw new InvalidOperationException($"Image with ID '{id}' not found");
            }

            image.Restore();
            await _unitOfWork.Images.UpdateAsync(image, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully restored image {ImageId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring image {ImageId}", id);
            throw;
        }
    }

    public async Task<long> GetTotalSizeByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting total size for collection {CollectionId}", collectionId);
            return await _unitOfWork.Images.GetTotalSizeByCollectionAsync(collectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total size for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<int> GetCountByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting count for collection {CollectionId}", collectionId);
            return await _unitOfWork.Images.GetCountByCollectionAsync(collectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting count for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task GenerateThumbnailAsync(Guid id, int width, int height, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating thumbnail for image {ImageId} with size {Width}x{Height}", id, width, height);

            var image = await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                throw new InvalidOperationException($"Image with ID '{id}' not found");
            }

            var collection = await _unitOfWork.Collections.GetByIdAsync(image.CollectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection for image {id} not found");
            }

            var fullPath = Path.Combine(collection.Path, image.RelativePath);
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Image file not found at path {fullPath}");
            }

            var thumbnailData = await _imageProcessingService.GenerateThumbnailAsync(fullPath, width, height, cancellationToken);
            var dimensions = $"{width}x{height}";

            await _cacheService.SaveCachedImageAsync(id, dimensions, thumbnailData, cancellationToken);

            _logger.LogInformation("Successfully generated thumbnail for image {ImageId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for image {ImageId}", id);
            throw;
        }
    }

    public async Task GenerateCacheAsync(Guid id, int width, int height, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating cache for image {ImageId} with size {Width}x{Height}", id, width, height);

            var image = await _unitOfWork.Images.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                throw new InvalidOperationException($"Image with ID '{id}' not found");
            }

            var collection = await _unitOfWork.Collections.GetByIdAsync(image.CollectionId, cancellationToken);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection for image {id} not found");
            }

            var fullPath = Path.Combine(collection.Path, image.RelativePath);
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Image file not found at path {fullPath}");
            }

            var cachedImageData = await _imageProcessingService.ResizeImageAsync(fullPath, width, height, 95, cancellationToken);
            var dimensions = $"{width}x{height}";

            await _cacheService.SaveCachedImageAsync(id, dimensions, cachedImageData, cancellationToken);

            _logger.LogInformation("Successfully generated cache for image {ImageId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cache for image {ImageId}", id);
            throw;
        }
    }
}

