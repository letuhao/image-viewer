using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Torrent entity - represents torrent file distribution and management
/// </summary>
public class Torrent : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("infoHash")]
    public string InfoHash { get; private set; } = string.Empty;

    [BsonElement("filePath")]
    public string FilePath { get; private set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; private set; }

    [BsonElement("pieceCount")]
    public int PieceCount { get; private set; }

    [BsonElement("pieceSize")]
    public long PieceSize { get; private set; }

    [BsonElement("trackerUrls")]
    public List<string> TrackerUrls { get; private set; } = new();

    [BsonElement("status")]
    public string Status { get; private set; } = "Active"; // Active, Paused, Stopped, Completed, Error

    [BsonElement("downloadSpeed")]
    public long DownloadSpeed { get; private set; } = 0;

    [BsonElement("uploadSpeed")]
    public long UploadSpeed { get; private set; } = 0;

    [BsonElement("downloadedBytes")]
    public long DownloadedBytes { get; private set; } = 0;

    [BsonElement("uploadedBytes")]
    public long UploadedBytes { get; private set; } = 0;

    [BsonElement("seeders")]
    public int Seeders { get; private set; } = 0;

    [BsonElement("leechers")]
    public int Leechers { get; private set; } = 0;

    [BsonElement("peerCount")]
    public int PeerCount { get; private set; } = 0;

    [BsonElement("progress")]
    public double Progress { get; private set; } = 0.0;

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0;

    [BsonElement("isPrivate")]
    public bool IsPrivate { get; private set; } = false;

    [BsonElement("isPaused")]
    public bool IsPaused { get; private set; } = false;

    [BsonElement("isCompleted")]
    public bool IsCompleted { get; private set; } = false;

    [BsonElement("downloadLimit")]
    public long? DownloadLimit { get; private set; }

    [BsonElement("uploadLimit")]
    public long? UploadLimit { get; private set; }

    [BsonElement("ratioLimit")]
    public double? RatioLimit { get; private set; }

    [BsonElement("seedTimeLimit")]
    public TimeSpan? SeedTimeLimit { get; private set; }

    [BsonElement("createdBy")]
    public ObjectId? CreatedBy { get; private set; }

    [BsonElement("collectionId")]
    public ObjectId? CollectionId { get; private set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("lastActivity")]
    public DateTime? LastActivity { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public Collection? Collection { get; private set; }

    // Private constructor for EF Core
    private Torrent() { }

    public static Torrent Create(string name, string infoHash, string filePath, long fileSize, int pieceCount, long pieceSize, ObjectId? createdBy = null, ObjectId? collectionId = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(infoHash))
            throw new ArgumentException("Info hash cannot be empty", nameof(infoHash));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (fileSize <= 0)
            throw new ArgumentException("File size must be greater than 0", nameof(fileSize));

        if (pieceCount <= 0)
            throw new ArgumentException("Piece count must be greater than 0", nameof(pieceCount));

        if (pieceSize <= 0)
            throw new ArgumentException("Piece size must be greater than 0", nameof(pieceSize));

        return new Torrent
        {
            Name = name,
            InfoHash = infoHash,
            FilePath = filePath,
            FileSize = fileSize,
            PieceCount = pieceCount,
            PieceSize = pieceSize,
            Description = description,
            CreatedBy = createdBy,
            CollectionId = collectionId,
            Status = "Active",
            DownloadedBytes = 0,
            UploadedBytes = 0,
            Progress = 0.0,
            Priority = 0,
            IsPrivate = false,
            IsPaused = false,
            IsCompleted = false,
            TrackerUrls = new List<string>(),
            Metadata = new Dictionary<string, object>(),
            Tags = new List<string>()
        };
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void AddTracker(string trackerUrl)
    {
        if (string.IsNullOrWhiteSpace(trackerUrl))
            throw new ArgumentException("Tracker URL cannot be empty", nameof(trackerUrl));

        if (!TrackerUrls.Contains(trackerUrl))
        {
            TrackerUrls.Add(trackerUrl);
            UpdateTimestamp();
        }
    }

    public void RemoveTracker(string trackerUrl)
    {
        TrackerUrls.Remove(trackerUrl);
        UpdateTimestamp();
    }

    public void UpdateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty", nameof(status));

        Status = status;
        LastActivity = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdateSpeeds(long downloadSpeed, long uploadSpeed)
    {
        DownloadSpeed = downloadSpeed;
        UploadSpeed = uploadSpeed;
        LastActivity = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdateProgress(long downloadedBytes, long uploadedBytes)
    {
        DownloadedBytes = downloadedBytes;
        UploadedBytes = uploadedBytes;
        
        if (FileSize > 0)
        {
            Progress = (double)downloadedBytes / FileSize * 100;
        }

        if (Progress >= 100.0)
        {
            IsCompleted = true;
            Status = "Completed";
        }

        LastActivity = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdatePeerInfo(int seeders, int leechers)
    {
        Seeders = seeders;
        Leechers = leechers;
        PeerCount = seeders + leechers;
        LastActivity = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetPrivate(bool isPrivate)
    {
        IsPrivate = isPrivate;
        UpdateTimestamp();
    }

    public void Pause()
    {
        IsPaused = true;
        Status = "Paused";
        UpdateTimestamp();
    }

    public void Resume()
    {
        IsPaused = false;
        Status = "Active";
        UpdateTimestamp();
    }

    public void SetLimits(long? downloadLimit, long? uploadLimit, double? ratioLimit, TimeSpan? seedTimeLimit)
    {
        DownloadLimit = downloadLimit;
        UploadLimit = uploadLimit;
        RatioLimit = ratioLimit;
        SeedTimeLimit = seedTimeLimit;
        UpdateTimestamp();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateTimestamp();
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public double GetRatio()
    {
        if (DownloadedBytes == 0) return 0;
        return (double)UploadedBytes / DownloadedBytes;
    }

    public bool HasReachedRatioLimit()
    {
        return RatioLimit.HasValue && GetRatio() >= RatioLimit.Value;
    }

    public bool IsSeeding()
    {
        return IsCompleted && !IsPaused;
    }
}
