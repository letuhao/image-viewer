namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Geolocation information DTO
/// </summary>
public class GeolocationInfo
{
    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Region/State
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Latitude
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Time zone
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// ISP
    /// </summary>
    public string? Isp { get; set; }

    /// <summary>
    /// Organization
    /// </summary>
    public string? Organization { get; set; }
}
