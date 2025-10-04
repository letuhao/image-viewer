using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Auth;

/// <summary>
/// Refresh token request DTO
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
