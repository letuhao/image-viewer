namespace ImageViewer.Application.DTOs.Collections;

/// <summary>
/// Collection overview DTO for list views (minimal data)
/// </summary>
public class CollectionOverviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "folder" or "archive"
    public bool IsNested { get; set; }
    public int Depth { get; set; }
    
    // Counts only (no embedded data)
    public long ImageCount { get; set; }
    public long ThumbnailCount { get; set; }
    public long CacheImageCount { get; set; }
    public long TotalSize { get; set; }
    
    // Thumbnail info for collection card display
    public string? ThumbnailPath { get; set; }
    public string? ThumbnailImageId { get; set; }
    public bool HasThumbnail { get; set; }
    
    /// <summary>
    /// Base64 encoded thumbnail for instant display (no additional HTTP request)
    /// Format: data:image/jpeg;base64,{base64Data}
    /// </summary>
    public string? ThumbnailBase64 { get; set; }
    
    /// <summary>
    /// First image ID in the collection for direct viewer navigation
    /// </summary>
    public string? FirstImageId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

