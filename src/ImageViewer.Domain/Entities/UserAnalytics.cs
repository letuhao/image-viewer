using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// UserAnalytics - represents user analytics and metrics
/// </summary>
public class UserAnalytics : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

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

    [BsonElement("averageSessionDuration")]
    public TimeSpan AverageSessionDuration { get; private set; }

    [BsonElement("lastActivityDate")]
    public DateTime? LastActivityDate { get; private set; }

    [BsonElement("preferredContentTypes")]
    public List<string> PreferredContentTypes { get; private set; } = new();

    [BsonElement("preferredViewingTimes")]
    public Dictionary<string, int> PreferredViewingTimes { get; private set; } = new();

    // Navigation properties
    public User User { get; private set; } = null!;

    // Private constructor for MongoDB
    private UserAnalytics() { }

    public UserAnalytics(ObjectId userId)
    {
        UserId = userId;
        TotalViews = 0;
        TotalDownloads = 0;
        TotalShares = 0;
        TotalLikes = 0;
        TotalComments = 0;
        AverageSessionDuration = TimeSpan.Zero;
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

    public void UpdateSessionDuration(TimeSpan duration)
    {
        // Simple moving average calculation
        var totalSessions = TotalViews + 1; // Approximate
        AverageSessionDuration = TimeSpan.FromTicks(
            (AverageSessionDuration.Ticks * (totalSessions - 1) + duration.Ticks) / totalSessions);
    }

    public void AddPreferredContentType(string contentType)
    {
        if (!PreferredContentTypes.Contains(contentType))
        {
            PreferredContentTypes.Add(contentType);
        }
    }

    public void RecordViewingTime(int hour)
    {
        var hourKey = $"{hour:D2}:00";
        PreferredViewingTimes[hourKey] = PreferredViewingTimes.GetValueOrDefault(hourKey, 0) + 1;
    }

    private void UpdateLastActivity()
    {
        LastActivityDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
