using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Update session request DTO
/// </summary>
public class UpdateSessionRequest
{
    /// <summary>
    /// Session ID
    /// </summary>
    [Required]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Session name
    /// </summary>
    [StringLength(100)]
    public string? SessionName { get; set; }

    /// <summary>
    /// Session expiry in minutes
    /// </summary>
    [Range(1, 1440)] // 1 minute to 24 hours
    public int? ExpiryMinutes { get; set; }
}
