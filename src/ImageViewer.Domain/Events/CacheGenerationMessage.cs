using MongoDB.Bson;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Message for cache generation operations
/// </summary>
public class CacheGenerationMessage : MessageEvent
{
    public string ImageId { get; set; } = string.Empty; // Changed from ObjectId to string for JSON serialization
    public string CollectionId { get; set; } = string.Empty; // Changed from ObjectId to string for JSON serialization
    public string ImagePath { get; set; } = string.Empty;
    public string CachePath { get; set; } = string.Empty;
    public int CacheWidth { get; set; } = 1920;
    public int CacheHeight { get; set; } = 1080;
    public int Quality { get; set; } = 85;
    public bool ForceRegenerate { get; set; } = false;
    public string? CreatedBy { get; set; }
    public string? CreatedBySystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? JobId { get; set; } // Link to background job for tracking

    public CacheGenerationMessage()
    {
        MessageType = "CacheGeneration";
    }
}
