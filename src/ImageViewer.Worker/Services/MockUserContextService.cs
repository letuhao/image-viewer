using ImageViewer.Application.Services;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Mock implementation of IUserContextService for Worker project
/// </summary>
public class MockUserContextService : IUserContextService
{
    public string GetCurrentUserId()
    {
        // Return a default system user ID for worker operations
        return "system-worker";
    }

    public string GetCurrentUserName()
    {
        // Return a default system user name for worker operations
        return "System Worker";
    }

    public bool IsAuthenticated()
    {
        // Worker operations are always considered authenticated
        return true;
    }

    public Task<string> GetCurrentUserIdAsync()
    {
        return Task.FromResult(GetCurrentUserId());
    }

    public Task<string> GetCurrentUserNameAsync()
    {
        return Task.FromResult(GetCurrentUserName());
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(IsAuthenticated());
    }
}
