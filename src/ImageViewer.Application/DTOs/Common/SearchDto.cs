namespace ImageViewer.Application.DTOs.Common;

/// <summary>
/// Search request DTO
/// </summary>
public class SearchRequestDto
{
    public string? Query { get; set; }
    public string? SearchIn { get; set; } = "all"; // all, name, description, tags
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Format { get; set; }
    public int? MinWidth { get; set; }
    public int? MinHeight { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
}

/// <summary>
/// Search response DTO
/// </summary>
public class SearchResponseDto<T>
{
    public IEnumerable<T> Results { get; set; } = Enumerable.Empty<T>();
    public int TotalResults { get; set; }
    public string? Query { get; set; }
    public TimeSpan SearchTime { get; set; }
    public Dictionary<string, int> Facets { get; set; } = new();
}
