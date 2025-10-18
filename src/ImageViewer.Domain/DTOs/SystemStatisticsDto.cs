namespace ImageViewer.Domain.DTOs;

/// <summary>
/// System statistics DTO for efficient MongoDB aggregation results
/// </summary>
public class SystemStatisticsDto
{
    public int TotalCollections { get; set; }
    public long TotalImages { get; set; }
    public long TotalSize { get; set; }
    public long TotalCacheSize { get; set; }
    public int TotalViewSessions { get; set; }
    public double TotalViewTime { get; set; }
    public double AverageImagesPerCollection { get; set; }
    public double AverageViewTimePerSession { get; set; }
}
