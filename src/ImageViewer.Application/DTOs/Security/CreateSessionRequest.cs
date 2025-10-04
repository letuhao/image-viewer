using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Create session request DTO
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// Session name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string SessionName { get; set; } = string.Empty;

    /// <summary>
    /// IP address
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session expiry in minutes
    /// </summary>
    [Range(1, 1440)] // 1 minute to 24 hours
    public int ExpiryMinutes { get; set; } = 60;
}
