using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// ContentPopularity - represents content popularity metrics
/// </summary>
public class ContentPopularity : BaseEntity
{
    [BsonElement("contentId")]
    public ObjectId ContentId { get; private set; }

    [BsonElement("contentType")]
    public string ContentType { get; private set; } = string.Empty;

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

    [BsonElement("popularityScore")]
    public double PopularityScore { get; private set; }

    [BsonElement("trendingScore")]
    public double TrendingScore { get; private set; }

    [BsonElement("lastCalculated")]
    public DateTime LastCalculated { get; private set; }

    [BsonElement("dailyViews")]
    public Dictionary<DateTime, long> DailyViews { get; private set; } = new();

    [BsonElement("weeklyViews")]
    public Dictionary<DateTime, long> WeeklyViews { get; private set; } = new();

    // Private constructor for MongoDB
    private ContentPopularity() { }

    public ContentPopularity(ObjectId contentId, string contentType)
    {
        ContentId = contentId;
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        TotalViews = 0;
        TotalDownloads = 0;
        TotalShares = 0;
        TotalLikes = 0;
        TotalComments = 0;
        PopularityScore = 0.0;
        TrendingScore = 0.0;
        LastCalculated = DateTime.UtcNow;
    }

    public void IncrementViews(long count = 1)
    {
        TotalViews += count;
        RecordDailyView();
        UpdateLastCalculated();
    }

    public void IncrementDownloads(long count = 1)
    {
        TotalDownloads += count;
        UpdateLastCalculated();
    }

    public void IncrementShares(long count = 1)
    {
        TotalShares += count;
        UpdateLastCalculated();
    }

    public void IncrementLikes(long count = 1)
    {
        TotalLikes += count;
        UpdateLastCalculated();
    }

    public void IncrementComments(long count = 1)
    {
        TotalComments += count;
        UpdateLastCalculated();
    }

    public void CalculatePopularityScore()
    {
        // Weighted calculation: views(1.0) + downloads(2.0) + shares(3.0) + likes(2.0) + comments(1.5)
        PopularityScore = (TotalViews * 1.0) + 
                         (TotalDownloads * 2.0) + 
                         (TotalShares * 3.0) + 
                         (TotalLikes * 2.0) + 
                         (TotalComments * 1.5);
        
        UpdateLastCalculated();
    }

    public void CalculateTrendingScore()
    {
        // Calculate trending based on recent activity (last 7 days)
        var recentViews = DailyViews
            .Where(kvp => kvp.Key >= DateTime.UtcNow.AddDays(-7))
            .Sum(kvp => kvp.Value);

        var totalRecentActivity = recentViews + TotalDownloads + TotalShares + TotalLikes + TotalComments;
        TrendingScore = totalRecentActivity / 7.0; // Average daily activity

        UpdateLastCalculated();
    }

    private void RecordDailyView()
    {
        var today = DateTime.UtcNow.Date;
        DailyViews[today] = DailyViews.GetValueOrDefault(today, 0) + 1;

        // Keep only last 30 days of daily data
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-30);
        var keysToRemove = DailyViews.Keys.Where(date => date < cutoffDate).ToList();
        foreach (var key in keysToRemove)
        {
            DailyViews.Remove(key);
        }
    }

    private void UpdateLastCalculated()
    {
        LastCalculated = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
