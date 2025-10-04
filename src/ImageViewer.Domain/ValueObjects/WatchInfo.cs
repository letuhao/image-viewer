using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Watch info value object for file system monitoring
/// </summary>
public class WatchInfo
{
    [BsonElement("isWatching")]
    public bool IsWatching { get; private set; }
    
    [BsonElement("watchPath")]
    public string WatchPath { get; private set; }
    
    [BsonElement("watchFilters")]
    public List<string> WatchFilters { get; private set; }
    
    [BsonElement("lastWatchDate")]
    public DateTime? LastWatchDate { get; private set; }
    
    [BsonElement("watchCount")]
    public long WatchCount { get; private set; }
    
    [BsonElement("lastChangeDetected")]
    public DateTime? LastChangeDetected { get; private set; }
    
    [BsonElement("changeCount")]
    public long ChangeCount { get; private set; }

    public WatchInfo()
    {
        IsWatching = false;
        WatchPath = string.Empty;
        WatchFilters = new List<string>();
        WatchCount = 0;
        ChangeCount = 0;
    }

    public void EnableWatching()
    {
        IsWatching = true;
        LastWatchDate = DateTime.UtcNow;
        WatchCount++;
    }

    public void DisableWatching()
    {
        IsWatching = false;
    }

    public void UpdateWatchPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Watch path cannot be null or empty", nameof(path));
        
        WatchPath = path;
    }

    public void AddWatchFilter(string filter)
    {
        if (!string.IsNullOrWhiteSpace(filter) && !WatchFilters.Contains(filter))
        {
            WatchFilters.Add(filter);
        }
    }

    public void RemoveWatchFilter(string filter)
    {
        WatchFilters.Remove(filter);
    }

    public void RecordChange()
    {
        LastChangeDetected = DateTime.UtcNow;
        ChangeCount++;
    }

    public void ResetChangeCount()
    {
        ChangeCount = 0;
    }
}
