using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;
using SkiaSharp;
using ImageViewer.Application.Options;
using Microsoft.Extensions.Options;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Advanced thumbnail service implementation
/// </summary>
public class AdvancedThumbnailService : IAdvancedThumbnailService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<AdvancedThumbnailService> _logger;
    private readonly string _thumbnailBasePath;
    private readonly ImageSizeOptions _sizeOptions;

    public AdvancedThumbnailService(
        IUnitOfWork unitOfWork,
        IImageProcessingService imageProcessingService,
        ILogger<AdvancedThumbnailService> logger,
        IOptions<ImageSizeOptions> sizeOptions)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sizeOptions = sizeOptions?.Value ?? new ImageSizeOptions();
        _thumbnailBasePath = Path.Combine(Directory.GetCurrentDirectory(), "thumbnails");
        
        // Ensure thumbnail directory exists
        if (!Directory.Exists(_thumbnailBasePath))
        {
            Directory.CreateDirectory(_thumbnailBasePath);
        }
    }

    public async Task<string?> GenerateCollectionThumbnailAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating thumbnail for collection {CollectionId}", collectionId);

            var collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
            if (collection == null)
            {
                _logger.LogWarning("Collection {CollectionId} not found", collectionId);
                return null;
            }

            var images = await _unitOfWork.Images.GetByCollectionIdAsync(collectionId, cancellationToken);
            if (!images.Any())
            {
                _logger.LogWarning("No images found for collection {CollectionId}", collectionId);
                return null;
            }

            // Select best image for thumbnail
            var bestImage = SelectBestImageForThumbnail(images);
            if (bestImage == null)
            {
                _logger.LogWarning("No suitable image found for thumbnail in collection {CollectionId}", collectionId);
                return null;
            }

            // Generate thumbnail
            var thumbnailPath = GetThumbnailPath(collectionId);
            var thumbnailData = await _imageProcessingService.GenerateThumbnailAsync(
                bestImage.RelativePath, _sizeOptions.ThumbnailWidth, _sizeOptions.ThumbnailHeight, cancellationToken);

            if (thumbnailData == null || thumbnailData.Length == 0)
            {
                _logger.LogWarning("Failed to generate thumbnail for image {ImageId}", bestImage.Id);
                return null;
            }

            // Save thumbnail
            await File.WriteAllBytesAsync(thumbnailPath, thumbnailData, cancellationToken);
            
            _logger.LogInformation("Generated thumbnail for collection {CollectionId} at {ThumbnailPath}", 
                collectionId, thumbnailPath);
            
            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for collection {CollectionId}", collectionId);
            return null;
        }
    }

    public async Task<BatchThumbnailResult> BatchRegenerateThumbnailsAsync(IEnumerable<Guid> collectionIds, CancellationToken cancellationToken = default)
    {
        if (collectionIds == null)
        {
            return new BatchThumbnailResult
            {
                Total = 0,
                Success = 0,
                Failed = 0,
                FailedCollections = new List<Guid>(),
                Errors = new List<string>()
            };
        }

        var result = new BatchThumbnailResult
        {
            Total = collectionIds.Count()
        };

        _logger.LogInformation("Starting batch thumbnail regeneration for {Count} collections", result.Total);

        foreach (var collectionId in collectionIds)
        {
            try
            {
                var thumbnailPath = await GenerateCollectionThumbnailAsync(collectionId, cancellationToken);
                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    result.SuccessfulCollections.Add(collectionId);
                    result.Success++;
                }
                else
                {
                    result.FailedCollections.Add(collectionId);
                    result.Failed++;
                    result.Errors.Add($"Failed to generate thumbnail for collection {collectionId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail for collection {CollectionId}", collectionId);
                result.FailedCollections.Add(collectionId);
                result.Failed++;
                result.Errors.Add($"Error generating thumbnail for collection {collectionId}: {ex.Message}");
            }
        }

        _logger.LogInformation("Batch thumbnail regeneration completed: {Success} success, {Failed} failed", 
            result.Success, result.Failed);

        return result;
    }

    public async Task<byte[]?> GetCollectionThumbnailAsync(Guid collectionId, int? width = null, int? height = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var thumbnailPath = GetThumbnailPath(collectionId);
            
            if (!File.Exists(thumbnailPath))
            {
                _logger.LogInformation("Thumbnail not found for collection {CollectionId}, generating new one", collectionId);
                await GenerateCollectionThumbnailAsync(collectionId, cancellationToken);
                
                if (!File.Exists(thumbnailPath))
                {
                    _logger.LogWarning("Failed to generate thumbnail for collection {CollectionId}", collectionId);
                    return null;
                }
            }

            var thumbnailData = await File.ReadAllBytesAsync(thumbnailPath, cancellationToken);
            
            // Resize if requested
            if (width.HasValue || height.HasValue)
            {
                var targetWidth = width ?? _sizeOptions.ThumbnailWidth;
                var targetHeight = height ?? _sizeOptions.ThumbnailHeight;
                thumbnailData = await _imageProcessingService.ResizeImageAsync(
                    thumbnailPath, targetWidth, targetHeight, 95, cancellationToken);
            }

            return thumbnailData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail for collection {CollectionId}", collectionId);
            return null;
        }
    }

    public async Task DeleteCollectionThumbnailAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var thumbnailPath = GetThumbnailPath(collectionId);
            
            if (File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
                _logger.LogInformation("Deleted thumbnail for collection {CollectionId}", collectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting thumbnail for collection {CollectionId}", collectionId);
        }
    }

    private string GetThumbnailPath(Guid collectionId)
    {
        return Path.Combine(_thumbnailBasePath, $"{collectionId}.jpg");
    }

    private Image? SelectBestImageForThumbnail(IEnumerable<Image> images)
    {
        return images
            .OrderByDescending(img => CalculateImageScore(img))
            .FirstOrDefault();
    }

    private int CalculateImageScore(Image image)
    {
        var score = 0;
        
        // Prefer images with good aspect ratio (close to square)
        var aspectRatio = (double)image.Width / image.Height;
        if (aspectRatio >= 0.8 && aspectRatio <= 1.2)
        {
            score += 50;
        }
        
        // Prefer medium-sized images (not too small, not too large)
        var totalPixels = image.Width * image.Height;
        if (totalPixels >= 100000 && totalPixels <= 2000000) // 100K to 2M pixels
        {
            score += 30;
        }
        
        // Prefer common formats
        var commonFormats = new[] { "jpg", "jpeg", "png", "webp" };
        if (commonFormats.Contains(image.Format.ToLower()))
        {
            score += 20;
        }
        
        return score;
    }
}
