using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Update device request DTO
/// </summary>
public class UpdateDeviceRequest
{
    /// <summary>
    /// Device ID
    /// </summary>
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Device name
    /// </summary>
    [StringLength(100)]
    public string? DeviceName { get; set; }

    /// <summary>
    /// Is device trusted
    /// </summary>
    public bool? IsTrusted { get; set; }
}
