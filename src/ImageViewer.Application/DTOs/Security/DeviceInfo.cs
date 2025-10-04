namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Device information DTO
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// Device ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Device name
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device type
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Is device trusted
    /// </summary>
    public bool IsTrusted { get; set; }

    /// <summary>
    /// Registration date
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// Last access date
    /// </summary>
    public DateTime? LastAccessDate { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }
}
