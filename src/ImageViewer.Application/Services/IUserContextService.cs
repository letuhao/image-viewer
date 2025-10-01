namespace ImageViewer.Application.Services;

/// <summary>
/// User context service for tracking current user
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Get current user ID
    /// </summary>
    string GetCurrentUserId();

    /// <summary>
    /// Get current user name
    /// </summary>
    string GetCurrentUserName();

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    bool IsAuthenticated();
}
