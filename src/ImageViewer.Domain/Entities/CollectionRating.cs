using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Collection rating entity - represents user ratings for collections
/// </summary>
public class CollectionRating : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("collectionId")]
    public ObjectId CollectionId { get; private set; }

    [BsonElement("rating")]
    public int Rating { get; private set; } // 1-5 stars

    [BsonElement("review")]
    public string? Review { get; private set; }

    [BsonElement("isPublic")]
    public bool IsPublic { get; private set; }

    [BsonElement("helpfulVotes")]
    public int HelpfulVotes { get; private set; }

    [BsonElement("notHelpfulVotes")]
    public int NotHelpfulVotes { get; private set; }

    [BsonElement("reportedCount")]
    public int ReportedCount { get; private set; }

    [BsonElement("isModerated")]
    public bool IsModerated { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;
    
    [BsonIgnore]
    public Collection Collection { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionRating() { }

    public static CollectionRating Create(ObjectId userId, ObjectId collectionId, int rating, string? review = null, bool isPublic = true)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        return new CollectionRating
        {
            UserId = userId,
            CollectionId = collectionId,
            Rating = rating,
            Review = review,
            IsPublic = isPublic,
            HelpfulVotes = 0,
            NotHelpfulVotes = 0,
            ReportedCount = 0,
            IsModerated = false
        };
    }

    public void UpdateRating(int newRating, string? newReview = null)
    {
        if (newRating < 1 || newRating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(newRating));

        Rating = newRating;
        Review = newReview;
        UpdateTimestamp();
    }

    public void VoteHelpful()
    {
        HelpfulVotes++;
        UpdateTimestamp();
    }

    public void VoteNotHelpful()
    {
        NotHelpfulVotes++;
        UpdateTimestamp();
    }

    public void Report()
    {
        ReportedCount++;
        UpdateTimestamp();
    }

    public void Moderate()
    {
        IsModerated = true;
        UpdateTimestamp();
    }

    public void ToggleVisibility()
    {
        IsPublic = !IsPublic;
        UpdateTimestamp();
    }
}
