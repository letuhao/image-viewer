namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Risk assessment DTO
/// </summary>
public class RiskAssessment
{
    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Risk factors
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Assessment date
    /// </summary>
    public DateTime AssessedAt { get; set; }

    /// <summary>
    /// Should require additional verification
    /// </summary>
    public bool RequireAdditionalVerification { get; set; }
}
