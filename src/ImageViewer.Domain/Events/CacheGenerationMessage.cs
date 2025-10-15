using MongoDB.Bson;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Message for cache generation operations
/// </summary>
public class CacheGenerationMessage : MessageEvent
{
    public string ImageId { get; set; } = string.Empty; // Changed from ObjectId to string for JSON serialization
    public string CollectionId { get; set; } = string.Empty; // Changed from ObjectId to string for JSON serialization
    public ArchiveEntryInfo? ArchiveEntry { get; set; } // DTO for archive entries (required)
    public string CachePath { get; set; } = string.Empty;
    public int CacheWidth { get; set; } = 1920;
    public int CacheHeight { get; set; } = 1080;
    public int Quality { get; set; } = 85;
    public string Format { get; set; } = "jpeg"; // jpeg, webp, original
    public bool PreserveOriginal { get; set; } = false; // If true, don't resize, keep original quality
    public bool ForceRegenerate { get; set; } = false;
    public string? CreatedBy { get; set; }
    public string? CreatedBySystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? JobId { get; set; } // Link to background job for tracking
    public string? ScanJobId { get; set; } // Link to parent scan job for multi-stage tracking

    public CacheGenerationMessage()
    {
        MessageType = "CacheGeneration";
    }
}
