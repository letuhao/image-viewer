namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Geolocation security result DTO
/// </summary>
public class GeolocationSecurityResult
{
    /// <summary>
    /// Is location trusted
    /// </summary>
    public bool IsTrusted { get; set; }

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Geolocation info
    /// </summary>
    public GeolocationInfo? GeolocationInfo { get; set; }

    /// <summary>
    /// Security recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Should require additional verification
    /// </summary>
    public bool RequireAdditionalVerification { get; set; }
}
