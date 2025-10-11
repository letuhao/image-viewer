namespace ImageViewer.Application.DTOs.Collections;

/// <summary>
/// DTO for collection navigation information
/// </summary>
public class CollectionNavigationDto
{
    public string? PreviousCollectionId { get; set; }
    public string? NextCollectionId { get; set; }
    public int CurrentPosition { get; set; }
    public int TotalCollections { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

/// <summary>
/// DTO for collection siblings list
/// </summary>
public class CollectionSiblingsDto
{
    public List<CollectionOverviewDto> Siblings { get; set; } = new();
    public int CurrentPosition { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// DTO for collection position request
/// </summary>
public class CollectionPositionRequest
{
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "desc";
}

