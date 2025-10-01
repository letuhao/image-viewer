using System.Security.Claims;

namespace ImageViewer.Application.Services;

/// <summary>
/// JWT service interface
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT token
    /// </summary>
    string GenerateToken(string userId, string userName, IEnumerable<string> roles);

    /// <summary>
    /// Validate JWT token
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Get user ID from token
    /// </summary>
    string? GetUserIdFromToken(string token);

    /// <summary>
    /// Get user name from token
    /// </summary>
    string? GetUserNameFromToken(string token);
}
