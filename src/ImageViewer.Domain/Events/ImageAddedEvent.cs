using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Domain event raised when an image is added to a collection
/// </summary>
public class ImageAddedEvent : IDomainEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }
    public Image Image { get; }
    public Collection Collection { get; }

    public ImageAddedEvent(Image image, Collection collection)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        Image = image ?? throw new ArgumentNullException(nameof(image));
        Collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }
}
