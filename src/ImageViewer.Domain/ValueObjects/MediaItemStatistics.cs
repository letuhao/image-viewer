using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Media item statistics value object
/// </summary>
public class MediaItemStatistics
{
    [BsonElement("totalItems")]
    public long TotalItems { get; private set; }
    
    [BsonElement("totalSize")]
    public long TotalSize { get; private set; }
    
    [BsonElement("totalViews")]
    public long TotalViews { get; private set; }
    
    [BsonElement("totalDownloads")]
    public long TotalDownloads { get; private set; }
    
    [BsonElement("totalShares")]
    public long TotalShares { get; private set; }
    
    [BsonElement("totalLikes")]
    public long TotalLikes { get; private set; }
    
    [BsonElement("totalComments")]
    public long TotalComments { get; private set; }
    
    [BsonElement("averageFileSize")]
    public double AverageFileSize { get; private set; }
    
    [BsonElement("lastActivity")]
    public DateTime? LastActivity { get; private set; }

    public MediaItemStatistics()
    {
        TotalItems = 0;
        TotalSize = 0;
        TotalViews = 0;
        TotalDownloads = 0;
        TotalShares = 0;
        TotalLikes = 0;
        TotalComments = 0;
        AverageFileSize = 0;
    }

    public void UpdateStats(long totalItems, long totalSize)
    {
        TotalItems = totalItems;
        TotalSize = totalSize;
        AverageFileSize = totalItems > 0 ? (double)totalSize / totalItems : 0;
        UpdateLastActivity();
    }

    public void IncrementViews(long count = 1)
    {
        TotalViews += count;
        UpdateLastActivity();
    }

    public void IncrementDownloads(long count = 1)
    {
        TotalDownloads += count;
        UpdateLastActivity();
    }

    public void IncrementShares(long count = 1)
    {
        TotalShares += count;
        UpdateLastActivity();
    }

    public void IncrementLikes(long count = 1)
    {
        TotalLikes += count;
        UpdateLastActivity();
    }

    public void IncrementComments(long count = 1)
    {
        TotalComments += count;
        UpdateLastActivity();
    }

    public void UpdateLastActivity()
    {
        LastActivity = DateTime.UtcNow;
    }
}
