namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Session information DTO
/// </summary>
public class SessionInfo
{
    /// <summary>
    /// Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Session name
    /// </summary>
    public string SessionName { get; set; } = string.Empty;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiry date
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Is session active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Last activity date
    /// </summary>
    public DateTime? LastActivity { get; set; }
}
