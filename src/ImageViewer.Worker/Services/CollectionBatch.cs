using ImageViewer.Domain.Events;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Generic batch collection for organizing messages by collection ID
/// </summary>
/// <typeparam name="T">Type of message (ThumbnailGenerationMessage or CacheGenerationMessage)</typeparam>
public class CollectionBatch<T> where T : class
{
    public string CollectionId { get; }
    public List<T> Messages { get; }
    public DateTime LastAddedTime { get; set; }
    public bool Processing { get; set; }
    public object Lock { get; }

    public int Count => Messages.Count;

    public CollectionBatch(string collectionId)
    {
        CollectionId = collectionId;
        Messages = new List<T>();
        LastAddedTime = DateTime.UtcNow;
        Processing = false;
        Lock = new object();
    }
}

/// <summary>
/// Specific batch for thumbnail messages
/// </summary>
public class ThumbnailCollectionBatch : CollectionBatch<ThumbnailGenerationMessage>
{
    public ThumbnailCollectionBatch(string collectionId) : base(collectionId)
    {
    }
}

/// <summary>
/// Specific batch for cache messages
/// </summary>
public class CacheCollectionBatch : CollectionBatch<CacheGenerationMessage>
{
    public CacheCollectionBatch(string collectionId) : base(collectionId)
    {
    }
}
