namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Security report DTO
/// </summary>
public class SecurityReport
{
    /// <summary>
    /// Report ID
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Report title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Report summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Security metrics
    /// </summary>
    public SecurityMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Key findings
    /// </summary>
    public List<string> KeyFindings { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Generated date
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Period start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Period end date
    /// </summary>
    public DateTime? EndDate { get; set; }
}
