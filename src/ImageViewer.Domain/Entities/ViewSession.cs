using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// ViewSession entity - represents a user's viewing session
/// </summary>
public class ViewSession : BaseEntity
{
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? UserId { get; private set; }
    
    [BsonElement("collectionId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId CollectionId { get; private set; }
    
    [BsonElement("currentImageId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? CurrentImageId { get; private set; }
    
    [BsonElement("settings")]
    public ViewSessionSettings Settings { get; private set; }
    
    [BsonElement("startedAt")]
    public DateTime StartedAt { get; private set; }
    
    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; private set; }
    
    [BsonElement("imagesViewed")]
    public int ImagesViewed { get; private set; }
    
    [BsonElement("totalViewTime")]
    public TimeSpan TotalViewTime { get; private set; }
    
    [BsonIgnore]
    public TimeSpan ViewDuration => TotalViewTime;

    // Navigation properties
    [BsonIgnore]
    public Collection Collection { get; private set; } = null!;
    // public Image? CurrentImage { get; private set; } // Removed - Image entity deleted

    // Private constructor for EF Core
    private ViewSession() { }

    public ViewSession(ObjectId collectionId, ViewSessionSettings settings, ObjectId? userId = null)
    {
        UserId = userId;
        CollectionId = collectionId;
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        StartedAt = DateTime.UtcNow;
        ImagesViewed = 0;
        TotalViewTime = TimeSpan.Zero;
    }

    public void SetCurrentImage(ObjectId imageId)
    {
        CurrentImageId = imageId;
    }

    public void ClearCurrentImage()
    {
        CurrentImageId = null;
    }

    public void UpdateSettings(ViewSessionSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void IncrementImagesViewed()
    {
        ImagesViewed++;
    }

    public void AddViewTime(TimeSpan viewTime)
    {
        TotalViewTime = TotalViewTime.Add(viewTime);
    }

    public void EndSession()
    {
        EndedAt = DateTime.UtcNow;
    }

    public bool IsActive()
    {
        return EndedAt == null;
    }

    public TimeSpan GetSessionDuration()
    {
        var endTime = EndedAt ?? DateTime.UtcNow;
        return endTime - StartedAt;
    }

    public double GetAverageViewTimePerImage()
    {
        return ImagesViewed > 0 ? TotalViewTime.TotalSeconds / ImagesViewed : 0;
    }
}
