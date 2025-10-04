using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// ViewSession entity - represents a user's viewing session
/// </summary>
public class ViewSession : BaseEntity
{
    public new Guid Id { get; private set; }
    public Guid CollectionId { get; private set; }
    public Guid? CurrentImageId { get; private set; }
    public ViewSessionSettings Settings { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public int ImagesViewed { get; private set; }
    public TimeSpan TotalViewTime { get; private set; }
    public TimeSpan ViewDuration => TotalViewTime;
    public DateTime CreatedAt => StartedAt;

    // Navigation properties
    public Collection Collection { get; private set; } = null!;
    public Image? CurrentImage { get; private set; }

    // Private constructor for EF Core
    private ViewSession() { }

    public ViewSession(Guid collectionId, ViewSessionSettings settings)
    {
        Id = Guid.NewGuid();
        CollectionId = collectionId;
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        StartedAt = DateTime.UtcNow;
        ImagesViewed = 0;
        TotalViewTime = TimeSpan.Zero;
    }

    public void SetCurrentImage(Guid imageId)
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
