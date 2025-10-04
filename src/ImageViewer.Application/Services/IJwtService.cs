using System.Security.Claims;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Services;

/// <summary>
/// JWT service interface
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate access token for user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generate refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>ClaimsPrincipal if valid, null if invalid</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Check if token is expired
    /// </summary>
    /// <param name="token">JWT token to check</param>
    /// <returns>True if expired, false otherwise</returns>
    bool IsTokenExpired(string token);

    /// <summary>
    /// Get user ID from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if found, null otherwise</returns>
    string? GetUserIdFromToken(string token);

    /// <summary>
    /// Get username from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Username if found, null otherwise</returns>
    string? GetUsernameFromToken(string token);

    /// <summary>
    /// Get role from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Role if found, null otherwise</returns>
    string? GetRoleFromToken(string token);
}
