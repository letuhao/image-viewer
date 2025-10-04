namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// IP whitelist entry DTO
/// </summary>
public class IPWhitelistEntry
{
    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }
}
