using MongoDB.Bson;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionStatisticsEntity - represents statistics for a collection
/// </summary>
public class CollectionStatisticsEntity : BaseEntity
{
    public ObjectId CollectionId { get; private set; }
    public int TotalImages { get; private set; }
    public long TotalSizeBytes { get; private set; }
    public int AverageWidth { get; private set; }
    public int AverageHeight { get; private set; }
    public int ViewCount { get; private set; }
    public DateTime LastViewedAt { get; private set; }

    // Navigation properties
    public Collection Collection { get; private set; } = null!;

    // Private constructor for MongoDB
    private CollectionStatisticsEntity() { }

    public CollectionStatisticsEntity(ObjectId collectionId)
    {
        CollectionId = collectionId;
        TotalImages = 0;
        TotalSizeBytes = 0;
        AverageWidth = 0;
        AverageHeight = 0;
        ViewCount = 0;
        LastViewedAt = DateTime.UtcNow;
    }

    public void UpdateImageCount(int totalImages)
    {
        if (totalImages < 0)
            throw new ArgumentException("Total images cannot be negative", nameof(totalImages));

        TotalImages = totalImages;
        UpdateTimestamp();
    }

    public void UpdateTotalSize(long totalSizeBytes)
    {
        if (totalSizeBytes < 0)
            throw new ArgumentException("Total size cannot be negative", nameof(totalSizeBytes));

        TotalSizeBytes = totalSizeBytes;
        UpdateTimestamp();
    }

    public void UpdateAverageDimensions(int averageWidth, int averageHeight)
    {
        if (averageWidth < 0)
            throw new ArgumentException("Average width cannot be negative", nameof(averageWidth));
        if (averageHeight < 0)
            throw new ArgumentException("Average height cannot be negative", nameof(averageHeight));

        AverageWidth = averageWidth;
        AverageHeight = averageHeight;
        UpdateTimestamp();
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        LastViewedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void ResetViewCount()
    {
        ViewCount = 0;
        UpdateTimestamp();
    }

    public double GetAverageFileSize()
    {
        return TotalImages > 0 ? (double)TotalSizeBytes / TotalImages : 0;
    }

    public string GetFormattedTotalSize()
    {
        return FormatBytes(TotalSizeBytes);
    }

    public string GetFormattedAverageFileSize()
    {
        return FormatBytes((long)GetAverageFileSize());
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
