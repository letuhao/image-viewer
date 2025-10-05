namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Security alert DTO
/// </summary>
public class SecurityAlert
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
    /// Alert type
    /// </summary>
    public Application.Services.SecurityAlertType AlertType { get; set; }

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
    /// Is read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Read date
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Additional data
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}
