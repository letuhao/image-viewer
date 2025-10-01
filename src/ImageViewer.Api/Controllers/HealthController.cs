using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Infrastructure.Data;

namespace ImageViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ImageViewerDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ImageViewerDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check application health
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HealthStatus>> GetHealth()
    {
        try
        {
            var isDatabaseHealthy = await CheckDatabaseHealthAsync();
            
            var status = new HealthStatus
            {
                IsHealthy = isDatabaseHealthy,
                Timestamp = DateTime.UtcNow,
                Services = new Dictionary<string, bool>
                {
                    ["Database"] = isDatabaseHealthy,
                    ["API"] = true
                }
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health status");
            return StatusCode(500, new HealthStatus
            {
                IsHealthy = false,
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }

    private async Task<bool> CheckDatabaseHealthAsync()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return false;
        }
    }
}

public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, bool> Services { get; set; } = new();
    public string? Error { get; set; }
}