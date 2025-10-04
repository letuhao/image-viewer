using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Auth;

/// <summary>
/// Login request DTO
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username or email
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Two-factor authentication code (required if 2FA is enabled)
    /// </summary>
    [StringLength(10)]
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// Remember me flag
    /// </summary>
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// IP address of the login attempt
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the login attempt
    /// </summary>
    public string? UserAgent { get; set; }
}
