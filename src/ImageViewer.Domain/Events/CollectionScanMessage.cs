using MongoDB.Bson;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Message for collection scanning operations
/// </summary>
public class CollectionScanMessage : MessageEvent
{
    public ObjectId CollectionId { get; set; }
    public string CollectionPath { get; set; } = string.Empty;
    public CollectionType CollectionType { get; set; }
    public bool ForceRescan { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedBySystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CollectionScanMessage()
    {
        MessageType = "CollectionScan";
    }
}
