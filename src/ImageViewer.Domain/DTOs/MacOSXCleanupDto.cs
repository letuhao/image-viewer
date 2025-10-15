namespace ImageViewer.Domain.DTOs;

/// <summary>
/// Result of __MACOSX cleanup operation
/// </summary>
public class MacOSXCleanupResult
{
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public int AffectedCollections { get; set; }
    public int TotalImagesRemoved { get; set; }
    public int TotalThumbnailsRemoved { get; set; }
    public int TotalCacheImagesRemoved { get; set; }
    public long TotalSpaceFreed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<CollectionCleanupDetail> AffectedCollectionDetails { get; set; } = new();
}

/// <summary>
/// Preview of what would be cleaned up
/// </summary>
public class MacOSXCleanupPreview
{
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public int AffectedCollections { get; set; }
    public int TotalImagesToRemove { get; set; }
    public int TotalThumbnailsToRemove { get; set; }
    public int TotalCacheImagesToRemove { get; set; }
    public long TotalSpaceToFree { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<CollectionCleanupPreviewDetail> AffectedCollectionDetails { get; set; } = new();
}

/// <summary>
/// Details of cleanup for a specific collection
/// </summary>
public class CollectionCleanupDetail
{
    public string CollectionId { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string CollectionPath { get; set; } = string.Empty;
    public int ImagesRemoved { get; set; }
    public int ThumbnailsRemoved { get; set; }
    public int CacheImagesRemoved { get; set; }
    public long SpaceFreed { get; set; }
}

/// <summary>
/// Preview details for a specific collection
/// </summary>
public class CollectionCleanupPreviewDetail
{
    public string CollectionId { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string CollectionPath { get; set; } = string.Empty;
    public int ImagesToRemove { get; set; }
    public int ThumbnailsToRemove { get; set; }
    public int CacheImagesToRemove { get; set; }
    public long SpaceToFree { get; set; }
}

/// <summary>
/// Internal result for a single collection cleanup
/// </summary>
public class CollectionCleanupResult
{
    public int ImagesRemoved { get; set; }
    public int ThumbnailsRemoved { get; set; }
    public int CacheImagesRemoved { get; set; }
    public long SpaceFreed { get; set; }
}

/// <summary>
/// Internal preview for a single collection
/// </summary>
public class CollectionCleanupPreview
{
    public int ImagesToRemove { get; set; }
    public int ThumbnailsToRemove { get; set; }
    public int CacheImagesToRemove { get; set; }
    public long SpaceToFree { get; set; }
}
