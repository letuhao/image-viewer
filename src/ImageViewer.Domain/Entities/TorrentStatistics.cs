using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Torrent statistics entity - represents torrent distribution statistics and analytics
/// </summary>
public class TorrentStatistics : BaseEntity
{
    [BsonElement("torrentId")]
    public ObjectId TorrentId { get; private set; }

    [BsonElement("date")]
    public DateTime Date { get; private set; }

    [BsonElement("totalDownloaded")]
    public long TotalDownloaded { get; private set; } = 0;

    [BsonElement("totalUploaded")]
    public long TotalUploaded { get; private set; } = 0;

    [BsonElement("totalSeeders")]
    public int TotalSeeders { get; private set; } = 0;

    [BsonElement("totalLeechers")]
    public int TotalLeechers { get; private set; } = 0;

    [BsonElement("averageSeeders")]
    public double AverageSeeders { get; private set; } = 0;

    [BsonElement("averageLeechers")]
    public double AverageLeechers { get; private set; } = 0;

    [BsonElement("maxSeeders")]
    public int MaxSeeders { get; private set; } = 0;

    [BsonElement("maxLeechers")]
    public int MaxLeechers { get; private set; } = 0;

    [BsonElement("minSeeders")]
    public int MinSeeders { get; private set; } = 0;

    [BsonElement("minLeechers")]
    public int MinLeechers { get; private set; } = 0;

    [BsonElement("averageDownloadSpeed")]
    public double AverageDownloadSpeed { get; private set; } = 0;

    [BsonElement("averageUploadSpeed")]
    public double AverageUploadSpeed { get; private set; } = 0;

    [BsonElement("maxDownloadSpeed")]
    public long MaxDownloadSpeed { get; private set; } = 0;

    [BsonElement("maxUploadSpeed")]
    public long MaxUploadSpeed { get; private set; } = 0;

    [BsonElement("completionRate")]
    public double CompletionRate { get; private set; } = 0;

    [BsonElement("averageRatio")]
    public double AverageRatio { get; private set; } = 0;

    [BsonElement("totalConnections")]
    public int TotalConnections { get; private set; } = 0;

    [BsonElement("failedConnections")]
    public int FailedConnections { get; private set; } = 0;

    [BsonElement("successfulConnections")]
    public int SuccessfulConnections { get; private set; } = 0;

    [BsonElement("trackerResponses")]
    public int TrackerResponses { get; private set; } = 0;

    [BsonElement("trackerErrors")]
    public int TrackerErrors { get; private set; } = 0;

    [BsonElement("pieceRequests")]
    public int PieceRequests { get; private set; } = 0;

    [BsonElement("pieceDownloads")]
    public int PieceDownloads { get; private set; } = 0;

    [BsonElement("pieceUploads")]
    public int PieceUploads { get; private set; } = 0;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("sampleCount")]
    public int SampleCount { get; private set; } = 0;

    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public Torrent Torrent { get; private set; } = null!;

    // Private constructor for EF Core
    private TorrentStatistics() { }

    public static TorrentStatistics Create(ObjectId torrentId, DateTime date)
    {
        return new TorrentStatistics
        {
            TorrentId = torrentId,
            Date = date.Date, // Normalize to start of day
            TotalDownloaded = 0,
            TotalUploaded = 0,
            TotalSeeders = 0,
            TotalLeechers = 0,
            AverageSeeders = 0,
            AverageLeechers = 0,
            MaxSeeders = 0,
            MaxLeechers = 0,
            MinSeeders = 0,
            MinLeechers = 0,
            AverageDownloadSpeed = 0,
            AverageUploadSpeed = 0,
            MaxDownloadSpeed = 0,
            MaxUploadSpeed = 0,
            CompletionRate = 0,
            AverageRatio = 0,
            TotalConnections = 0,
            FailedConnections = 0,
            SuccessfulConnections = 0,
            TrackerResponses = 0,
            TrackerErrors = 0,
            PieceRequests = 0,
            PieceDownloads = 0,
            PieceUploads = 0,
            SampleCount = 0,
            LastUpdated = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>()
        };
    }

    public void AddSample(int seeders, int leechers, long downloadSpeed, long uploadSpeed, long downloaded, long uploaded, int connections, int failedConnections, int trackerResponses, int trackerErrors, int pieceRequests, int pieceDownloads, int pieceUploads)
    {
        SampleCount++;

        // Update totals
        TotalDownloaded += downloaded;
        TotalUploaded += uploaded;
        TotalConnections += connections;
        FailedConnections += failedConnections;
        SuccessfulConnections += (connections - failedConnections);
        TrackerResponses += trackerResponses;
        TrackerErrors += trackerErrors;
        PieceRequests += pieceRequests;
        PieceDownloads += pieceDownloads;
        PieceUploads += pieceUploads;

        // Update seeders/leechers statistics
        TotalSeeders += seeders;
        TotalLeechers += leechers;

        if (seeders > MaxSeeders) MaxSeeders = seeders;
        if (leechers > MaxLeechers) MaxLeechers = leechers;
        if (MinSeeders == 0 || seeders < MinSeeders) MinSeeders = seeders;
        if (MinLeechers == 0 || leechers < MinLeechers) MinLeechers = leechers;

        // Update speed statistics
        if (downloadSpeed > MaxDownloadSpeed) MaxDownloadSpeed = downloadSpeed;
        if (uploadSpeed > MaxUploadSpeed) MaxUploadSpeed = uploadSpeed;

        // Recalculate averages
        AverageSeeders = (double)TotalSeeders / SampleCount;
        AverageLeechers = (double)TotalLeechers / SampleCount;
        AverageDownloadSpeed = (double)(TotalDownloaded / SampleCount);
        AverageUploadSpeed = (double)(TotalUploaded / SampleCount);

        // Calculate ratio
        if (TotalDownloaded > 0)
        {
            AverageRatio = (double)TotalUploaded / TotalDownloaded;
        }

        LastUpdated = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetCompletionRate(double completionRate)
    {
        CompletionRate = completionRate;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public double GetConnectionSuccessRate()
    {
        if (TotalConnections == 0) return 0;
        return (double)SuccessfulConnections / TotalConnections * 100;
    }

    public double GetTrackerSuccessRate()
    {
        var totalTrackerRequests = TrackerResponses + TrackerErrors;
        if (totalTrackerRequests == 0) return 0;
        return (double)TrackerResponses / totalTrackerRequests * 100;
    }

    public double GetPieceEfficiency()
    {
        if (PieceRequests == 0) return 0;
        return (double)PieceDownloads / PieceRequests * 100;
    }

    public bool IsHealthy()
    {
        return GetConnectionSuccessRate() > 80 && GetTrackerSuccessRate() > 70;
    }
}
