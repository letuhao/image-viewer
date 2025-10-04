using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for notification operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new notification
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var notification = await _notificationService.CreateNotificationAsync(request);
            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var notificationId))
                return BadRequest(new { message = "Invalid notification ID format" });

            var notification = await _notificationService.GetNotificationByIdAsync(notificationId);
            return Ok(notification);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { message = "Feature not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification with ID {NotificationId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notifications for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userObjectId, page, pageSize);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get unread notifications for a user
    /// </summary>
    [HttpGet("user/{userId}/unread")]
    public async Task<IActionResult> GetUnreadNotifications(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var notifications = await _notificationService.GetUnreadNotificationsAsync(userObjectId);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread notifications for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var notificationId))
                return BadRequest(new { message = "Invalid notification ID format" });

            var notification = await _notificationService.MarkAsReadAsync(notificationId);
            return Ok(notification);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { message = "Feature not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    [HttpPost("user/{userId}/mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            await _notificationService.MarkAllAsReadAsync(userObjectId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var notificationId))
                return BadRequest(new { message = "Invalid notification ID format" });

            await _notificationService.DeleteNotificationAsync(notificationId);
            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete all notifications for a user
    /// </summary>
    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> DeleteAllNotifications(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            await _notificationService.DeleteAllNotificationsAsync(userObjectId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all notifications for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Send real-time notification
    /// </summary>
    [HttpPost("send/realtime")]
    public async Task<IActionResult> SendRealTimeNotification([FromBody] SendRealTimeNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var message = new NotificationMessage
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Priority = request.Priority,
                ActionUrl = request.ActionUrl,
                Metadata = request.Metadata
            };

            await _notificationService.SendRealTimeNotificationAsync(userObjectId, message);
            return Ok(new { message = "Real-time notification sent successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Send broadcast notification
    /// </summary>
    [HttpPost("send/broadcast")]
    public async Task<IActionResult> SendBroadcastNotification([FromBody] SendBroadcastNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var message = new NotificationMessage
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Priority = request.Priority,
                ActionUrl = request.ActionUrl,
                Metadata = request.Metadata
            };

            await _notificationService.SendBroadcastNotificationAsync(message);
            return Ok(new { message = "Broadcast notification sent successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send broadcast notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Send group notification
    /// </summary>
    [HttpPost("send/group")]
    public async Task<IActionResult> SendGroupNotification([FromBody] SendGroupNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.UserIds == null || !request.UserIds.Any())
                return BadRequest(new { message = "User IDs list cannot be null or empty" });

            var userIds = request.UserIds.Select(id => ObjectId.Parse(id)).ToList();
            var message = new NotificationMessage
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Priority = request.Priority,
                ActionUrl = request.ActionUrl,
                Metadata = request.Metadata
            };

            await _notificationService.SendGroupNotificationAsync(userIds, message);
            return Ok(new { message = $"Group notification sent to {userIds.Count} users successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send group notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create notification template
    /// </summary>
    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateNotificationTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = await _notificationService.CreateTemplateAsync(request);
            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification template");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification template by ID
    /// </summary>
    [HttpGet("templates/{id}")]
    public async Task<IActionResult> GetTemplate(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var templateId))
                return BadRequest(new { message = "Invalid template ID format" });

            var template = await _notificationService.GetTemplateByIdAsync(templateId);
            return Ok(template);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { message = "Feature not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification template {TemplateId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification templates by type
    /// </summary>
    [HttpGet("templates/type/{type}")]
    public async Task<IActionResult> GetTemplatesByType(string type)
    {
        try
        {
            if (!Enum.TryParse<NotificationType>(type, out var notificationType))
                return BadRequest(new { message = "Invalid notification type" });

            var templates = await _notificationService.GetTemplatesByTypeAsync(notificationType);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification templates by type {Type}", type);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update notification template
    /// </summary>
    [HttpPut("templates/{id}")]
    public async Task<IActionResult> UpdateTemplate(string id, [FromBody] UpdateNotificationTemplateRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var templateId))
                return BadRequest(new { message = "Invalid template ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = await _notificationService.UpdateTemplateAsync(templateId, request);
            return Ok(template);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { message = "Feature not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification template {TemplateId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete notification template
    /// </summary>
    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var templateId))
                return BadRequest(new { message = "Invalid template ID format" });

            await _notificationService.DeleteTemplateAsync(templateId);
            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification template {TemplateId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user notification preferences
    /// </summary>
    [HttpGet("preferences/{userId}")]
    public async Task<IActionResult> GetUserPreferences(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var preferences = await _notificationService.GetUserPreferencesAsync(userObjectId);
            return Ok(preferences);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user notification preferences
    /// </summary>
    [HttpPut("preferences/{userId}")]
    public async Task<IActionResult> UpdateUserPreferences(string userId, [FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var preferences = await _notificationService.UpdateUserPreferencesAsync(userObjectId, request);
            return Ok(preferences);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if notification is enabled for user and type
    /// </summary>
    [HttpGet("preferences/{userId}/enabled/{type}")]
    public async Task<IActionResult> IsNotificationEnabled(string userId, string type)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!Enum.TryParse<NotificationType>(type, out var notificationType))
                return BadRequest(new { message = "Invalid notification type" });

            var isEnabled = await _notificationService.IsNotificationEnabledAsync(userObjectId, notificationType);
            return Ok(new { enabled = isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check notification enabled status for user {UserId} and type {Type}", userId, type);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetNotificationAnalytics([FromQuery] string? userId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            ObjectId? userObjectId = null;
            if (!string.IsNullOrEmpty(userId) && ObjectId.TryParse(userId, out var parsedUserId))
            {
                userObjectId = parsedUserId;
            }

            var analytics = await _notificationService.GetNotificationAnalyticsAsync(userObjectId, fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetNotificationStatistics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var statistics = await _notificationService.GetNotificationStatisticsAsync(fromDate, toDate);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for sending real-time notification
/// </summary>
public class SendRealTimeNotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request model for sending broadcast notification
/// </summary>
public class SendBroadcastNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request model for sending group notification
/// </summary>
public class SendGroupNotificationRequest
{
    public List<string> UserIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
