using System.Text.Json.Serialization;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for bulk operations
/// </summary>
public interface IBulkService
{
    /// <summary>
    /// Bulk add collections from parent directory
    /// </summary>
    Task<BulkOperationResult> BulkAddCollectionsAsync(BulkAddCollectionsRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for bulk add collections
/// </summary>
public class BulkAddCollectionsRequest
{
    [JsonPropertyName("libraryId")]
    public ObjectId? LibraryId { get; set; } // Nullable - collections may not belong to any library
    
    [JsonPropertyName("parentPath")]
    public string ParentPath { get; set; } = string.Empty;
    
    [JsonPropertyName("collectionPrefix")]
    public string? CollectionPrefix { get; set; }
    
    [JsonPropertyName("includeSubfolders")]
    public bool IncludeSubfolders { get; set; } = false;
    
    [JsonPropertyName("autoAdd")]
    public bool AutoAdd { get; set; } = false;
    
    [JsonPropertyName("overwriteExisting")]
    public bool OverwriteExisting { get; set; } = false;
    
    [JsonPropertyName("thumbnailWidth")]
    public int? ThumbnailWidth { get; set; }
    
    [JsonPropertyName("thumbnailHeight")]
    public int? ThumbnailHeight { get; set; }
    
    [JsonPropertyName("cacheWidth")]
    public int? CacheWidth { get; set; }
    
    [JsonPropertyName("cacheHeight")]
    public int? CacheHeight { get; set; }
    
    [JsonPropertyName("enableCache")]
    public bool? EnableCache { get; set; }
    
    [JsonPropertyName("autoScan")]
    public bool? AutoScan { get; set; }
    
    [JsonPropertyName("createdAfter")]
    public DateTime? CreatedAfter { get; set; }
    
    [JsonPropertyName("createdBefore")]
    public DateTime? CreatedBefore { get; set; }
    
    [JsonPropertyName("modifiedAfter")]
    public DateTime? ModifiedAfter { get; set; }
    
    [JsonPropertyName("modifiedBefore")]
    public DateTime? ModifiedBefore { get; set; }
}

/// <summary>
/// Result of bulk operation
/// </summary>
public class BulkOperationResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public int ScannedCount { get; set; }
    public List<BulkCollectionResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result for individual collection in bulk operation
/// </summary>
public class BulkCollectionResult
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ObjectId? CollectionId { get; set; }
}

/// <summary>
/// Potential collection found during scanning
/// </summary>
public class PotentialCollection
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType Type { get; set; }
}
