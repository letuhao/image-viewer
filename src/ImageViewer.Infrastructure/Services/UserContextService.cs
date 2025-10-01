using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ImageViewer.Application.Services;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// User context service implementation
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string GetCurrentUserId()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) 
                    ?? httpContext.User.FindFirst("sub") 
                    ?? httpContext.User.FindFirst("user_id");
                
                if (userIdClaim != null)
                {
                    return userIdClaim.Value;
                }
            }

            // Fallback to anonymous user
            return "anonymous";
        }
        catch (Exception)
        {
            return "system";
        }
    }

    public string GetCurrentUserName()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userNameClaim = httpContext.User.FindFirst(ClaimTypes.Name) 
                    ?? httpContext.User.FindFirst("name") 
                    ?? httpContext.User.FindFirst("username");
                
                if (userNameClaim != null)
                {
                    return userNameClaim.Value;
                }
            }

            return "Anonymous User";
        }
        catch (Exception)
        {
            return "system";
        }
    }

    public bool IsAuthenticated()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.User?.Identity?.IsAuthenticated == true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
