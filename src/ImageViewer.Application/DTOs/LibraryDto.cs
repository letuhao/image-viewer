namespace ImageViewer.Application.DTOs;

/// <summary>
/// DTO for Library entity
/// Ensures ObjectId fields are serialized as strings for frontend
/// </summary>
public class LibraryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; }
    public LibrarySettingsDto Settings { get; set; } = new();
    public LibraryMetadataDto Metadata { get; set; } = new();
    public LibraryStatisticsDto Statistics { get; set; } = new();
    public WatchInfoDto WatchInfo { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class LibrarySettingsDto
{
    public bool AutoScan { get; set; }
    public long ScanInterval { get; set; }
    public bool GenerateThumbnails { get; set; }
    public bool GenerateCache { get; set; }
    public bool EnableWatching { get; set; }
    public long MaxFileSize { get; set; }
    public List<string> AllowedFormats { get; set; } = new();
    public List<string> ExcludedPaths { get; set; } = new();
    public ThumbnailSettingsDto ThumbnailSettings { get; set; } = new();
    public CacheSettingsDto CacheSettings { get; set; } = new();
}

public class ThumbnailSettingsDto
{
    public bool Enabled { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Quality { get; set; }
    public string Format { get; set; } = string.Empty;
}

public class CacheSettingsDto
{
    public bool Enabled { get; set; }
    public long MaxSize { get; set; }
    public int CompressionLevel { get; set; }
    public int RetentionDays { get; set; }
}

public class LibraryMetadataDto
{
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

public class LibraryStatisticsDto
{
    public long TotalCollections { get; set; }
    public long TotalMediaItems { get; set; }
    public long TotalSize { get; set; }
    public long TotalViews { get; set; }
    public long TotalDownloads { get; set; }
    public DateTime? LastScannedAt { get; set; }
}

public class WatchInfoDto
{
    public bool IsWatching { get; set; }
    public string? WatchPath { get; set; }
    public List<string> WatchFilters { get; set; } = new();
    public DateTime? LastWatchEvent { get; set; }
}

