using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for repairing corrupted data in the database
/// </summary>
public class DataRepairService : IDataRepairService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<DataRepairService> _logger;

    public DataRepairService(
        ICollectionRepository collectionRepository,
        ILogger<DataRepairService> logger)
    {
        _collectionRepository = collectionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Repair truncated ZIP file paths in collections
    /// This fixes paths that were truncated at the # character during original scans
    /// </summary>
    public async Task<DataRepairResult> RepairTruncatedZipPathsAsync()
    {
        _logger.LogInformation("🔧 Starting ZIP path truncation repair...");

        var result = new DataRepairResult();
        var collections = await _collectionRepository.GetAllAsync();
        
        foreach (var collection in collections)
        {
            if (collection.Images == null || !collection.Images.Any())
                continue;

            var imagesFixed = 0;
            var imagesToUpdate = new List<Domain.ValueObjects.ImageEmbedded>();

            foreach (var image in collection.Images)
            {
                // Check if this looks like a truncated ZIP path
                if (IsTruncatedZipPath(image.LegacyRelativePath))
                {
                    _logger.LogDebug("🔍 Found truncated path: {Path} in collection {CollectionName}", 
                        image.LegacyRelativePath, collection.Name);

                    // Try to find the actual ZIP file
                    var actualZipPath = await FindActualZipFileAsync(collection.Path, image.LegacyRelativePath);
                    
                    if (!string.IsNullOrEmpty(actualZipPath))
                    {
                        // Create a new ImageEmbedded with the correct path
                        var fixedImage = await CreateFixedImageAsync(image, actualZipPath, collection.Path);
                        if (fixedImage != null)
                        {
                            imagesToUpdate.Add(fixedImage);
                            imagesFixed++;
                            
                            _logger.LogInformation("✅ Fixed truncated path: {OldPath} -> {NewPath}", 
                                image.LegacyRelativePath, fixedImage.Filename);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Could not find actual ZIP file for truncated path: {Path}", 
                            image.LegacyRelativePath);
                        result.Warnings.Add($"Could not find ZIP file for: {image.LegacyRelativePath}");
                    }
                }
            }

            // Update the collection with fixed images
            if (imagesToUpdate.Any())
            {
                // Replace the old images with fixed ones
                var updatedImages = collection.Images.ToList();
                foreach (var fixedImage in imagesToUpdate)
                {
                    var index = updatedImages.FindIndex(img => img.Id == fixedImage.Id);
                    if (index >= 0)
                    {
                        updatedImages[index] = fixedImage;
                    }
                }

                collection.Images = updatedImages;
                await _collectionRepository.UpdateAsync(collection);
                
                result.CollectionsFixed++;
                result.ImagesFixed += imagesFixed;
                
                _logger.LogInformation("✅ Fixed {Count} images in collection {Name}", 
                    imagesFixed, collection.Name);
            }
        }

        _logger.LogInformation("🎉 ZIP path repair complete! Fixed {ImagesFixed} images in {CollectionsFixed} collections", 
            result.ImagesFixed, result.CollectionsFixed);

        return result;
    }

    /// <summary>
    /// Check if a path looks like a truncated ZIP path
    /// </summary>
    private static bool IsTruncatedZipPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Look for patterns like "[Folder] Name" without # or ::
        // This suggests the path was truncated at the # character
        return path.StartsWith("[") && 
               path.Contains("] ") && 
               !path.Contains("#") && 
               !path.Contains("::") &&
               !path.EndsWith(".zip");
    }

    /// <summary>
    /// Find the actual ZIP file by scanning the directory
    /// </summary>
    private async Task<string?> FindActualZipFileAsync(string collectionPath, string truncatedPath)
    {
        try
        {
            if (!Directory.Exists(collectionPath))
                return null;

            // Extract the base name from the truncated path
            // e.g., "[Mr. Teardrop] Mash Kyrielight" -> "Mash Kyrielight"
            var baseName = ExtractBaseName(truncatedPath);
            
            // Look for ZIP files that contain this base name
            var zipFiles = Directory.GetFiles(collectionPath, "*.zip", SearchOption.TopDirectoryOnly);
            
            foreach (var zipFile in zipFiles)
            {
                var fileName = Path.GetFileName(zipFile);
                if (fileName.Contains(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("🎯 Found potential ZIP file: {FileName}", fileName);
                    return zipFile;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error finding ZIP file for truncated path: {Path}", truncatedPath);
            return null;
        }
    }

    /// <summary>
    /// Extract the base name from a truncated path
    /// </summary>
    private static string ExtractBaseName(string truncatedPath)
    {
        // Extract the part after "] " from patterns like "[Folder] Name"
        var index = truncatedPath.IndexOf("] ");
        if (index > 0)
        {
            return truncatedPath.Substring(index + 2).Trim();
        }
        
        return truncatedPath;
    }

    /// <summary>
    /// Create a fixed ImageEmbedded with the correct ZIP path
    /// </summary>
    private async Task<Domain.ValueObjects.ImageEmbedded?> CreateFixedImageAsync(
        Domain.ValueObjects.ImageEmbedded originalImage, 
        string actualZipPath, 
        string collectionPath)
    {
        try
        {
            // For now, we'll create a basic fix
            // In a real scenario, you'd want to scan the ZIP file to get the actual entry names
            // and create proper ArchiveEntryInfo objects
            
            var relativeZipPath = Path.GetRelativePath(collectionPath, actualZipPath);
            
            // Create a new ImageEmbedded with the correct path
            var fixedImage = new Domain.ValueObjects.ImageEmbedded(
                originalImage.Filename,
                relativeZipPath, // This will be the ZIP file path
                originalImage.FileSize,
                originalImage.Width,
                originalImage.Height,
                originalImage.Format
            );
            
            return fixedImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating fixed image for: {Path}", originalImage.LegacyRelativePath);
            return null;
        }
    }
}

/// <summary>
/// Result of a data repair operation
/// </summary>
public class DataRepairResult
{
    public int CollectionsFixed { get; set; }
    public int ImagesFixed { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Interface for data repair operations
/// </summary>
public interface IDataRepairService
{
    Task<DataRepairResult> RepairTruncatedZipPathsAsync();
}
