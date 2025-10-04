using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Register device request DTO
/// </summary>
public class RegisterDeviceRequest
{
    /// <summary>
    /// Device name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device type
    /// </summary>
    [Required]
    [StringLength(50)]
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Device identifier
    /// </summary>
    [Required]
    [StringLength(200)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// User agent
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }
}
