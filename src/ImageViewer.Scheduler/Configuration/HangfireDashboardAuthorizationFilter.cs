using Hangfire.Dashboard;

namespace ImageViewer.Scheduler.Configuration;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// Hangfire仪表板授权过滤器 - Bộ lọc ủy quyền cho dashboard Hangfire
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // For development: allow all
        // For production: check if user is authenticated and has Admin role
        #if DEBUG
        return true;
        #else
        return httpContext.User.Identity?.IsAuthenticated == true &&
               (httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Scheduler"));
        #endif
    }
}

