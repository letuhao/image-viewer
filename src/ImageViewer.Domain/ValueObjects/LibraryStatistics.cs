using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Library statistics value object
/// </summary>
public class LibraryStatistics
{
    [BsonElement("totalCollections")]
    public long TotalCollections { get; private set; }
    
    [BsonElement("totalMediaItems")]
    public long TotalMediaItems { get; private set; }
    
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
    
    [BsonElement("lastScanDate")]
    public DateTime? LastScanDate { get; private set; }
    
    [BsonElement("scanCount")]
    public long ScanCount { get; private set; }
    
    [BsonElement("lastActivity")]
    public DateTime? LastActivity { get; private set; }
    
    [BsonElement("totalLibraries")]
    public long TotalLibraries { get; set; }
    
    [BsonElement("activeLibraries")]
    public long ActiveLibraries { get; set; }
    
    [BsonElement("publicLibraries")]
    public long PublicLibraries { get; set; }
    
    [BsonElement("newLibrariesThisMonth")]
    public long NewLibrariesThisMonth { get; set; }
    
    [BsonElement("newLibrariesThisWeek")]
    public long NewLibrariesThisWeek { get; set; }
    
    [BsonElement("newLibrariesToday")]
    public long NewLibrariesToday { get; set; }

    public LibraryStatistics()
    {
        TotalCollections = 0;
        TotalMediaItems = 0;
        TotalSize = 0;
        TotalViews = 0;
        TotalDownloads = 0;
        TotalShares = 0;
        TotalLikes = 0;
        TotalComments = 0;
        ScanCount = 0;
        TotalLibraries = 0;
        ActiveLibraries = 0;
        PublicLibraries = 0;
        NewLibrariesThisMonth = 0;
        NewLibrariesThisWeek = 0;
        NewLibrariesToday = 0;
    }

    public void IncrementCollections(long count = 1)
    {
        TotalCollections += count;
        UpdateLastActivity();
    }

    public void DecrementCollections(long count = 1)
    {
        TotalCollections = Math.Max(0, TotalCollections - count);
        UpdateLastActivity();
    }

    public void IncrementMediaItems(long count = 1)
    {
        TotalMediaItems += count;
        UpdateLastActivity();
    }

    public void DecrementMediaItems(long count = 1)
    {
        TotalMediaItems = Math.Max(0, TotalMediaItems - count);
        UpdateLastActivity();
    }

    public void IncrementSize(long size)
    {
        TotalSize += size;
        UpdateLastActivity();
    }

    public void DecrementSize(long size)
    {
        TotalSize = Math.Max(0, TotalSize - size);
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

    public void UpdateLastScanDate(DateTime scanDate)
    {
        LastScanDate = scanDate;
        ScanCount++;
        UpdateLastActivity();
    }

    public void UpdateLastActivity()
    {
        LastActivity = DateTime.UtcNow;
    }
}
