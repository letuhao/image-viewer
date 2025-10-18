namespace ImageViewer.Api.DTOs;

/// <summary>
/// Tag data transfer object
/// </summary>
public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TagColorDto Color { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Tag color data transfer object
/// </summary>
public class TagColorDto
{
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
}

/// <summary>
/// Create tag request DTO
/// </summary>
public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TagColorDto Color { get; set; } = new();
}

/// <summary>
/// Update tag request DTO
/// </summary>
public class UpdateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TagColorDto Color { get; set; } = new();
}

/// <summary>
/// Tag list response DTO
/// </summary>
public class TagListResponse
{
    public List<TagDto> Tags { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

