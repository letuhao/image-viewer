using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Api.DTOs;

/// <summary>
/// Collection data transfer object
/// </summary>
public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType Type { get; set; }
    public CollectionSettingsDto Settings { get; set; } = new();
    public string? CoverImagePath { get; set; }
    public int ImageCount { get; set; }
    public long TotalSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ImageDto> Images { get; set; } = new();
    public List<TagDto> Tags { get; set; } = new();
    public CollectionStatisticsDto? Statistics { get; set; }
}

/// <summary>
/// Collection settings data transfer object
/// </summary>
public class CollectionSettingsDto
{
    public bool AutoScan { get; set; }
    public bool RecursiveScan { get; set; }
    public List<string> AllowedFormats { get; set; } = new();
    public int MaxImageSize { get; set; }
    public bool GenerateThumbnails { get; set; }
    public int ThumbnailSize { get; set; }
    public bool GeneratePreviews { get; set; }
    public int PreviewSize { get; set; }
    public int PreviewQuality { get; set; }
    public bool CacheImages { get; set; }
    public int CacheExpirationDays { get; set; }
    public bool EnableCompression { get; set; }
    public int CompressionQuality { get; set; }
}

/// <summary>
/// Collection statistics data transfer object
/// </summary>
public class CollectionStatisticsDto
{
    public Guid Id { get; set; }
    public Guid CollectionId { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create collection request DTO
/// </summary>
public class CreateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType Type { get; set; }
    public CollectionSettingsDto Settings { get; set; } = new();
}

/// <summary>
/// Update collection request DTO
/// </summary>
public class UpdateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionSettingsDto Settings { get; set; } = new();
}

/// <summary>
/// Collection list response DTO
/// </summary>
public class CollectionListResponse
{
    public List<CollectionDto> Collections { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

