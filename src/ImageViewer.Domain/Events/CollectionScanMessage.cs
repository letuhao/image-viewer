using MongoDB.Bson;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Message for collection scanning operations
/// </summary>
public class CollectionScanMessage : MessageEvent
{
    public string CollectionId { get; set; } = string.Empty; // Collection ID as string for JSON serialization
    public string CollectionPath { get; set; } = string.Empty;
    public CollectionType CollectionType { get; set; }
    public bool ForceRescan { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedBySystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? JobId { get; set; } // Link to background job for tracking

    public CollectionScanMessage()
    {
        MessageType = "CollectionScan";
    }
}
