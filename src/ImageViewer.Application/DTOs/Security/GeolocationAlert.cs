namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Geolocation alert DTO
/// </summary>
public class GeolocationAlert
{
    /// <summary>
    /// Alert ID
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Alert message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Alert severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Is acknowledged
    /// </summary>
    public bool IsAcknowledged { get; set; }
}
