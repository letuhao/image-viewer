using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.Tags;

/// <summary>
/// Tag DTO
/// </summary>
public class TagDto
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TagColorDto Color { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Tag color DTO
/// </summary>
public class TagColorDto
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
}


/// <summary>
/// Collection tag DTO
/// </summary>
public class CollectionTagDto
{
    public string Tag { get; set; } = string.Empty;
    public int Count { get; set; }
    public string AddedBy { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

/// <summary>
/// Add tag to collection DTO
/// </summary>
public class AddTagToCollectionDto
{
    public string TagName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TagColorDto? Color { get; set; }
}

/// <summary>
/// Create tag DTO
/// </summary>
public class CreateTagDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TagColorDto Color { get; set; } = null!;
}

/// <summary>
/// Update tag DTO
/// </summary>
public class UpdateTagDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TagColorDto Color { get; set; } = null!;
}

/// <summary>
/// Tag statistics DTO
/// </summary>
public class TagStatisticsDto
{
    public int TotalTags { get; set; }
    public int TotalTagUsages { get; set; }
    public double AverageTagsPerCollection { get; set; }
    public IEnumerable<PopularTagDto> PopularTags { get; set; } = new List<PopularTagDto>();
}

/// <summary>
/// Popular tag DTO
/// </summary>
public class PopularTagDto
{
    public ObjectId TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

/// <summary>
/// Tag suggestion DTO
/// </summary>
public class TagSuggestionDto
{
    public string TagName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
}
