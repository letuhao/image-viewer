using MongoDB.Bson;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Message for image processing operations
/// </summary>
public class ImageProcessingMessage : MessageEvent
{
    public string ImageId { get; set; } = string.Empty; // Changed from ObjectId to string for JSON serialization
    public string CollectionId { get; set; } = string.Empty; // Changed from ObjectId to string for JSON serialization
    public string ImagePath { get; set; } = string.Empty;
    public string ImageFormat { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    public bool GenerateThumbnail { get; set; } = true;
    public bool OptimizeImage { get; set; } = false;
    public string? TargetFormat { get; set; }
    public int? TargetWidth { get; set; }
    public int? TargetHeight { get; set; }
    public int? Quality { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedBySystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? JobId { get; set; } // Link to background job for tracking
    public string? ScanJobId { get; set; } // Link to parent scan job for multi-stage tracking

    public ImageProcessingMessage()
    {
        MessageType = "ImageProcessing";
    }
}
