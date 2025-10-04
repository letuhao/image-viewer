using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Media statistics value object
/// </summary>
public class MediaStatistics
{
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
    
    [BsonElement("totalRatings")]
    public long TotalRatings { get; private set; }
    
    [BsonElement("averageRating")]
    public double AverageRating { get; private set; }
    
    [BsonElement("totalRatingSum")]
    public long TotalRatingSum { get; private set; }
    
    [BsonElement("lastViewed")]
    public DateTime? LastViewed { get; private set; }
    
    [BsonElement("lastDownloaded")]
    public DateTime? LastDownloaded { get; private set; }
    
    [BsonElement("lastShared")]
    public DateTime? LastShared { get; private set; }
    
    [BsonElement("lastLiked")]
    public DateTime? LastLiked { get; private set; }
    
    [BsonElement("lastCommented")]
    public DateTime? LastCommented { get; private set; }
    
    [BsonElement("lastRated")]
    public DateTime? LastRated { get; private set; }

    public MediaStatistics()
    {
        TotalViews = 0;
        TotalDownloads = 0;
        TotalShares = 0;
        TotalLikes = 0;
        TotalComments = 0;
        TotalRatings = 0;
        AverageRating = 0.0;
        TotalRatingSum = 0;
    }

    public void IncrementViews(long count = 1)
    {
        TotalViews += count;
        LastViewed = DateTime.UtcNow;
    }

    public void IncrementDownloads(long count = 1)
    {
        TotalDownloads += count;
        LastDownloaded = DateTime.UtcNow;
    }

    public void IncrementShares(long count = 1)
    {
        TotalShares += count;
        LastShared = DateTime.UtcNow;
    }

    public void IncrementLikes(long count = 1)
    {
        TotalLikes += count;
        LastLiked = DateTime.UtcNow;
    }

    public void IncrementComments(long count = 1)
    {
        TotalComments += count;
        LastCommented = DateTime.UtcNow;
    }

    public void AddRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));
        
        TotalRatings++;
        TotalRatingSum += rating;
        AverageRating = (double)TotalRatingSum / TotalRatings;
        LastRated = DateTime.UtcNow;
    }

    public void UpdateRating(int oldRating, int newRating)
    {
        if (oldRating < 1 || oldRating > 5)
            throw new ArgumentException("Old rating must be between 1 and 5", nameof(oldRating));
        
        if (newRating < 1 || newRating > 5)
            throw new ArgumentException("New rating must be between 1 and 5", nameof(newRating));
        
        TotalRatingSum = TotalRatingSum - oldRating + newRating;
        AverageRating = (double)TotalRatingSum / TotalRatings;
        LastRated = DateTime.UtcNow;
    }

    public void RemoveRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));
        
        if (TotalRatings > 0)
        {
            TotalRatings--;
            TotalRatingSum -= rating;
            
            if (TotalRatings > 0)
            {
                AverageRating = (double)TotalRatingSum / TotalRatings;
            }
            else
            {
                AverageRating = 0.0;
                TotalRatingSum = 0;
            }
        }
    }

    public void UpdateLastViewed()
    {
        LastViewed = DateTime.UtcNow;
    }

    public void UpdateLastDownloaded()
    {
        LastDownloaded = DateTime.UtcNow;
    }

    public void UpdateLastShared()
    {
        LastShared = DateTime.UtcNow;
    }

    public void UpdateLastLiked()
    {
        LastLiked = DateTime.UtcNow;
    }

    public void UpdateLastCommented()
    {
        LastCommented = DateTime.UtcNow;
    }

    public void UpdateLastRated()
    {
        LastRated = DateTime.UtcNow;
    }
}
