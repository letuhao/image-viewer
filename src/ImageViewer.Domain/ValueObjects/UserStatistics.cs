using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// User statistics value object
/// </summary>
public class UserStatistics
{
    [BsonElement("totalViews")]
    public long TotalViews { get; private set; }
    
    [BsonElement("totalSearches")]
    public long TotalSearches { get; private set; }
    
    [BsonElement("totalDownloads")]
    public long TotalDownloads { get; private set; }
    
    [BsonElement("totalUploads")]
    public long TotalUploads { get; private set; }
    
    [BsonElement("totalCollections")]
    public long TotalCollections { get; private set; }
    
    [BsonElement("totalLibraries")]
    public long TotalLibraries { get; private set; }
    
    [BsonElement("totalTags")]
    public long TotalTags { get; private set; }
    
    [BsonElement("totalComments")]
    public long TotalComments { get; private set; }
    
    [BsonElement("totalLikes")]
    public long TotalLikes { get; private set; }
    
    [BsonElement("totalShares")]
    public long TotalShares { get; private set; }
    
    [BsonElement("totalFollowers")]
    public long TotalFollowers { get; private set; }
    
    [BsonElement("totalFollowing")]
    public long TotalFollowing { get; private set; }
    
    [BsonElement("totalPoints")]
    public long TotalPoints { get; private set; }
    
    [BsonElement("totalAchievements")]
    public long TotalAchievements { get; private set; }
    
    [BsonElement("lastActivity")]
    public DateTime? LastActivity { get; private set; }
    
    [BsonElement("joinDate")]
    public DateTime JoinDate { get; private set; }

    public UserStatistics()
    {
        TotalViews = 0;
        TotalSearches = 0;
        TotalDownloads = 0;
        TotalUploads = 0;
        TotalCollections = 0;
        TotalLibraries = 0;
        TotalTags = 0;
        TotalComments = 0;
        TotalLikes = 0;
        TotalShares = 0;
        TotalFollowers = 0;
        TotalFollowing = 0;
        TotalPoints = 0;
        TotalAchievements = 0;
        JoinDate = DateTime.UtcNow;
    }

    public void IncrementViews(long count = 1)
    {
        TotalViews += count;
        UpdateLastActivity();
    }

    public void IncrementSearches(long count = 1)
    {
        TotalSearches += count;
        UpdateLastActivity();
    }

    public void IncrementDownloads(long count = 1)
    {
        TotalDownloads += count;
        UpdateLastActivity();
    }

    public void IncrementUploads(long count = 1)
    {
        TotalUploads += count;
        UpdateLastActivity();
    }

    public void IncrementCollections(long count = 1)
    {
        TotalCollections += count;
        UpdateLastActivity();
    }

    public void IncrementLibraries(long count = 1)
    {
        TotalLibraries += count;
        UpdateLastActivity();
    }

    public void IncrementTags(long count = 1)
    {
        TotalTags += count;
        UpdateLastActivity();
    }

    public void IncrementComments(long count = 1)
    {
        TotalComments += count;
        UpdateLastActivity();
    }

    public void IncrementLikes(long count = 1)
    {
        TotalLikes += count;
        UpdateLastActivity();
    }

    public void IncrementShares(long count = 1)
    {
        TotalShares += count;
        UpdateLastActivity();
    }

    public void IncrementFollowers(long count = 1)
    {
        TotalFollowers += count;
        UpdateLastActivity();
    }

    public void IncrementFollowing(long count = 1)
    {
        TotalFollowing += count;
        UpdateLastActivity();
    }

    public void IncrementPoints(long count = 1)
    {
        TotalPoints += count;
        UpdateLastActivity();
    }

    public void IncrementAchievements(long count = 1)
    {
        TotalAchievements += count;
        UpdateLastActivity();
    }

    public void UpdateLastActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    public void SetJoinDate(DateTime joinDate)
    {
        JoinDate = joinDate;
    }
}
