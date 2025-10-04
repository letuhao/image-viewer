namespace ImageViewer.Application.DTOs.Auth;

/// <summary>
/// Logout request DTO
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// Refresh token to invalidate
    /// </summary>
    public string? RefreshToken { get; set; }
}
