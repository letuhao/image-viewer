using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Api.DTOs;

/// <summary>
/// Image data transfer object
/// </summary>
public class ImageDto
{
    public Guid Id { get; set; }
    public Guid CollectionId { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long Size { get; set; }
    public ImageMetadataDto Metadata { get; set; } = new();
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ImageCacheInfoDto? CacheInfo { get; set; }
}

/// <summary>
/// Image metadata data transfer object
/// </summary>
public class ImageMetadataDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Quality { get; set; }
    public string? ColorSpace { get; set; }
    public string? Compression { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Image cache info data transfer object
/// </summary>
public class ImageCacheInfoDto
{
    public Guid Id { get; set; }
    public Guid ImageId { get; set; }
    public string CachePath { get; set; } = string.Empty;
    public ImageDimensionsDto Dimensions { get; set; } = new();
    public DateTime CachedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Image dimensions data transfer object
/// </summary>
public class ImageDimensionsDto
{
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Image list response DTO
/// </summary>
public class ImageListResponse
{
    public List<ImageDto> Images { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Image upload response DTO
/// </summary>
public class ImageUploadResponse
{
    public Guid ImageId { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

