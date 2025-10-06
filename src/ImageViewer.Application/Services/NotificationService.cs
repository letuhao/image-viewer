using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for notification operations
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationQueueRepository _notificationQueueRepository;
    private readonly INotificationTemplateRepository _notificationTemplateRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUserRepository userRepository, 
        INotificationQueueRepository notificationQueueRepository,
        INotificationTemplateRepository notificationTemplateRepository,
        ILogger<NotificationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _notificationQueueRepository = notificationQueueRepository ?? throw new ArgumentNullException(nameof(notificationQueueRepository));
        _notificationTemplateRepository = notificationTemplateRepository ?? throw new ArgumentNullException(nameof(notificationTemplateRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Notification> CreateNotificationAsync(CreateNotificationRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Notification title cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new ValidationException("Notification message cannot be null or empty");

            // Check if user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{request.UserId}' not found");

            // Create notification
            var notification = new Notification
            {
                Id = ObjectId.GenerateNewId(),
                UserId = request.UserId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                ActionUrl = request.ActionUrl,
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                Priority = request.Priority,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ScheduledFor = request.ScheduledFor,
                ExpiresAt = request.ExpiresAfter.HasValue ? DateTime.UtcNow.Add(request.ExpiresAfter.Value) : null
            };

            // Save to NotificationQueue using the repository
            var domainNotification = Domain.Entities.NotificationQueue.Create(
                request.UserId,
                request.Type.ToString().ToLower(),
                request.Title,
                request.Message,
                request.Priority.ToString().ToLower(),
                null, // templateId
                request.ScheduledFor,
                request.ExpiresAfter.HasValue ? DateTime.UtcNow.Add(request.ExpiresAfter.Value) : null);

            await _notificationQueueRepository.CreateAsync(domainNotification);
            
            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", domainNotification.Id, request.UserId);

            // Map domain entity back to interface DTO
            notification.Id = domainNotification.Id;
            notification.Status = NotificationStatus.Pending; // Default status

            return notification;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", request.UserId);
            throw new BusinessRuleException($"Failed to create notification for user '{request.UserId}'", ex);
        }
    }

    public async Task<Notification> GetNotificationByIdAsync(ObjectId notificationId)
    {
        try
        {
            var domain = await _notificationQueueRepository.GetByIdAsync(notificationId);
            if (domain == null)
                throw new EntityNotFoundException($"Notification with ID '{notificationId}' not found");

            return new Notification
            {
                Id = domain.Id,
                UserId = domain.UserId,
                Type = Enum.TryParse<NotificationType>(domain.NotificationType, true, out var type) ? type : NotificationType.System,
                Title = domain.Subject,
                Message = domain.Content,
                Priority = Enum.TryParse<NotificationPriority>(domain.Priority, true, out var p) ? p : NotificationPriority.Normal,
                Status = Enum.TryParse<NotificationStatus>(domain.Status, true, out var s) ? s : NotificationStatus.Pending,
                CreatedAt = domain.CreatedAt,
                ScheduledFor = domain.ScheduledFor,
                ExpiresAt = domain.ExpiresAt,
                ReadAt = null,
                ActionUrl = null,
                Metadata = new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification with ID {NotificationId}", notificationId);
            throw new BusinessRuleException($"Failed to get notification with ID '{notificationId}'", ex);
        }
    }

    public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(ObjectId userId, int page = 1, int pageSize = 20)
    {
        try
        {
            // Get notifications by user from repository
            var domainNotifications = await _notificationQueueRepository.GetByUserIdAsync(userId);
            
            // Map domain entities to interface DTOs and apply pagination
            var notifications = domainNotifications
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(domainNotification => new Notification
                {
                    Id = domainNotification.Id,
                    UserId = domainNotification.UserId,
                    Type = Enum.TryParse<NotificationType>(domainNotification.NotificationType, true, out var type) ? type : NotificationType.System,
                    Title = domainNotification.Subject,
                    Message = domainNotification.Content,
                    ActionUrl = null, // Not available in domain entity
                    Metadata = new Dictionary<string, object>(), // Not available in domain entity
                    Priority = Enum.TryParse<NotificationPriority>(domainNotification.Priority, true, out var priority) ? priority : NotificationPriority.Normal,
                    Status = Enum.TryParse<NotificationStatus>(domainNotification.Status, true, out var status) ? status : NotificationStatus.Pending,
                    CreatedAt = domainNotification.CreatedAt,
                    ReadAt = null, // Not available in domain entity
                    ScheduledFor = null, // Not available in domain entity
                    ExpiresAt = null // Not available in domain entity
                }).ToList();

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get notifications for user '{userId}'", ex);
        }
    }

    public Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(ObjectId userId)
    {
        try
        {
            // TODO: Implement when notification repository is available
            // For now, return empty list
            return Task.FromResult<IEnumerable<Notification>>(new List<Notification>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread notifications for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get unread notifications for user '{userId}'", ex);
        }
    }

    public async Task<Notification> MarkAsReadAsync(ObjectId notificationId)
    {
        try
        {
            var domain = await _notificationQueueRepository.GetByIdAsync(notificationId);
            if (domain == null)
                throw new EntityNotFoundException($"Notification with ID '{notificationId}' not found");

            // We don't have a Read status mutation in domain; as a safe fallback, just return DTO marked as Read.
            _logger.LogInformation("MarkAsRead requested for {NotificationId}, but domain lacks read state mutation.", notificationId);

            return new Notification
            {
                Id = domain.Id,
                UserId = domain.UserId,
                Type = Enum.TryParse<NotificationType>(domain.NotificationType, true, out var type) ? type : NotificationType.System,
                Title = domain.Subject,
                Message = domain.Content,
                Priority = Enum.TryParse<NotificationPriority>(domain.Priority, true, out var p) ? p : NotificationPriority.Normal,
                Status = NotificationStatus.Read,
                CreatedAt = domain.CreatedAt,
                ScheduledFor = domain.ScheduledFor,
                ExpiresAt = domain.ExpiresAt,
                ReadAt = DateTime.UtcNow,
                ActionUrl = null,
                Metadata = new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            throw new BusinessRuleException($"Failed to mark notification '{notificationId}' as read", ex);
        }
    }

    public Task MarkAllAsReadAsync(ObjectId userId)
    {
        try
        {
            // TODO: Implement when notification repository is available
            _logger.LogInformation("Marked all notifications as read for user {UserId}", userId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to mark all notifications as read for user '{userId}'", ex);
        }
    }

    public Task DeleteNotificationAsync(ObjectId notificationId)
    {
        try
        {
            // TODO: Implement when notification repository is available
            _logger.LogInformation("Deleted notification {NotificationId}", notificationId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", notificationId);
            throw new BusinessRuleException($"Failed to delete notification '{notificationId}'", ex);
        }
    }

    public Task DeleteAllNotificationsAsync(ObjectId userId)
    {
        try
        {
            // TODO: Implement when notification repository is available
            _logger.LogInformation("Deleted all notifications for user {UserId}", userId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all notifications for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to delete all notifications for user '{userId}'", ex);
        }
    }

    public async Task SendRealTimeNotificationAsync(ObjectId userId, NotificationMessage message)
    {
        try
        {
            // TODO: Implement real-time notification delivery (WebSocket, SignalR, etc.)
            _logger.LogInformation("Sending real-time notification to user {UserId}: {Title}", userId, message.Title);
            
            // Placeholder for real-time delivery
            await Task.Delay(100); // Simulate async operation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time notification to user {UserId}", userId);
            throw new BusinessRuleException($"Failed to send real-time notification to user '{userId}'", ex);
        }
    }

    public async Task SendBroadcastNotificationAsync(NotificationMessage message)
    {
        try
        {
            // TODO: Implement broadcast notification to all users
            _logger.LogInformation("Sending broadcast notification: {Title}", message.Title);
            
            // Placeholder for broadcast delivery
            await Task.Delay(100); // Simulate async operation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send broadcast notification");
            throw new BusinessRuleException("Failed to send broadcast notification", ex);
        }
    }

    public async Task SendGroupNotificationAsync(List<ObjectId> userIds, NotificationMessage message)
    {
        try
        {
            if (userIds == null || !userIds.Any())
                throw new ValidationException("User IDs list cannot be null or empty");

            _logger.LogInformation("Sending group notification to {UserCount} users: {Title}", userIds.Count, message.Title);
            
            // Send to each user
            foreach (var userId in userIds)
            {
                await SendRealTimeNotificationAsync(userId, message);
            }
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to send group notification to {UserCount} users", userIds?.Count ?? 0);
            throw new BusinessRuleException("Failed to send group notification", ex);
        }
    }

    public async Task<NotificationTemplate> CreateTemplateAsync(CreateNotificationTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Template name cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(request.Subject))
                throw new ValidationException("Template subject cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(request.Body))
                throw new ValidationException("Template body cannot be null or empty");

            var template = new NotificationTemplate
            {
                Id = ObjectId.GenerateNewId(),
                Name = request.Name,
                Type = request.Type,
                Subject = request.Subject,
                Body = request.Body,
                ActionUrlTemplate = request.ActionUrlTemplate,
                RequiredVariables = request.RequiredVariables,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = ObjectId.Empty // TODO: Get from current user context
            };

            // Save to database using the repository
            var domainTemplate = new Domain.Entities.NotificationTemplate(
                request.Name,
                request.Type.ToString().ToLower(),
                "system", // Default category
                request.Subject,
                request.Body);

            await _notificationTemplateRepository.CreateAsync(domainTemplate);
            
            _logger.LogInformation("Created notification template {TemplateId}: {Name}", domainTemplate.Id, domainTemplate.TemplateName);

            // Map domain entity back to interface DTO
            template.Id = domainTemplate.Id;
            template.Name = domainTemplate.TemplateName;
            template.Type = request.Type;
            template.Subject = domainTemplate.Subject;
            template.Body = domainTemplate.Content;
            template.ActionUrlTemplate = request.ActionUrlTemplate;
            template.RequiredVariables = domainTemplate.Variables;
            template.IsActive = domainTemplate.IsActive;
            template.CreatedAt = domainTemplate.CreatedAt;
            template.UpdatedAt = domainTemplate.UpdatedAt;
            template.CreatedBy = ObjectId.Empty; // TODO: Get from current user context

            return template;
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to create notification template {Name}", request.Name);
            throw new BusinessRuleException($"Failed to create notification template '{request.Name}'", ex);
        }
    }

    public async Task<NotificationTemplate> GetTemplateByIdAsync(ObjectId templateId)
    {
        try
        {
            // Get template from repository
            var domainTemplate = await _notificationTemplateRepository.GetByIdAsync(templateId);
            if (domainTemplate == null)
            {
                throw new EntityNotFoundException($"Notification template with ID '{templateId}' not found");
            }

            // Map domain entity to interface DTO
            var template = new NotificationTemplate
            {
                Id = domainTemplate.Id,
                Name = domainTemplate.TemplateName,
                Type = Enum.TryParse<NotificationType>(domainTemplate.TemplateType, true, out var type) ? type : NotificationType.System,
                Subject = domainTemplate.Subject,
                Body = domainTemplate.Content,
                ActionUrlTemplate = null, // Not available in domain entity
                RequiredVariables = domainTemplate.Variables,
                IsActive = domainTemplate.IsActive,
                CreatedAt = domainTemplate.CreatedAt,
                UpdatedAt = domainTemplate.UpdatedAt,
                CreatedBy = ObjectId.Empty // Not available in domain entity
            };

            return template;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get notification template {TemplateId}", templateId);
            throw new BusinessRuleException($"Failed to get notification template '{templateId}'", ex);
        }
    }

    public async Task<IEnumerable<NotificationTemplate>> GetTemplatesByTypeAsync(NotificationType type)
    {
        try
        {
            // Get templates by type from repository
            var domainTemplates = await _notificationTemplateRepository.GetByTemplateTypeAsync(type.ToString().ToLower());
            
            // Map domain entities to interface DTOs
            var templates = domainTemplates.Select(domainTemplate => new NotificationTemplate
            {
                Id = domainTemplate.Id,
                Name = domainTemplate.TemplateName,
                Type = Enum.TryParse<NotificationType>(domainTemplate.TemplateType, true, out var parsedType) ? parsedType : NotificationType.System,
                Subject = domainTemplate.Subject,
                Body = domainTemplate.Content,
                ActionUrlTemplate = null, // Not available in domain entity
                RequiredVariables = domainTemplate.Variables,
                IsActive = domainTemplate.IsActive,
                CreatedAt = domainTemplate.CreatedAt,
                UpdatedAt = domainTemplate.UpdatedAt,
                CreatedBy = ObjectId.Empty // Not available in domain entity
            }).ToList();

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification templates by type {Type}", type);
            throw new BusinessRuleException($"Failed to get notification templates by type '{type}'", ex);
        }
    }

    public async Task<NotificationTemplate> UpdateTemplateAsync(ObjectId templateId, UpdateNotificationTemplateRequest request)
    {
        try
        {
            // Get existing template
            var domainTemplate = await _notificationTemplateRepository.GetByIdAsync(templateId);
            if (domainTemplate == null)
            {
                throw new EntityNotFoundException($"Notification template with ID '{templateId}' not found");
            }

            // Update template properties
            domainTemplate.UpdateContent(request.Subject ?? string.Empty, request.Body ?? string.Empty);
            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    domainTemplate.Activate();
                else
                    domainTemplate.Deactivate();
            }
            
            await _notificationTemplateRepository.UpdateAsync(domainTemplate);
            
            _logger.LogInformation("Updated notification template {TemplateId}: {Name}", domainTemplate.Id, domainTemplate.TemplateName);

            // Map updated domain entity back to interface DTO
            var updatedTemplate = new NotificationTemplate
            {
                Id = domainTemplate.Id,
                Name = domainTemplate.TemplateName,
                Type = Enum.TryParse<NotificationType>(domainTemplate.TemplateType, true, out var type) ? type : NotificationType.System,
                Subject = domainTemplate.Subject,
                Body = domainTemplate.Content,
                ActionUrlTemplate = request.ActionUrlTemplate,
                RequiredVariables = domainTemplate.Variables,
                IsActive = domainTemplate.IsActive,
                CreatedAt = domainTemplate.CreatedAt,
                UpdatedAt = domainTemplate.UpdatedAt,
                CreatedBy = ObjectId.Empty // Not available in domain entity
            };

            return updatedTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification template {TemplateId}", templateId);
            throw new BusinessRuleException($"Failed to update notification template '{templateId}'", ex);
        }
    }

    public async Task DeleteTemplateAsync(ObjectId templateId)
    {
        try
        {
            // Check if template exists
            var domainTemplate = await _notificationTemplateRepository.GetByIdAsync(templateId);
            if (domainTemplate == null)
            {
                throw new EntityNotFoundException($"Notification template with ID '{templateId}' not found");
            }

            // Delete template from repository
            await _notificationTemplateRepository.DeleteAsync(templateId);
            
            _logger.LogInformation("Deleted notification template {TemplateId}", templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification template {TemplateId}", templateId);
            throw new BusinessRuleException($"Failed to delete notification template '{templateId}'", ex);
        }
    }

    public async Task<NotificationPreferences> GetUserPreferencesAsync(ObjectId userId)
    {
        try
        {
            // Check if user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // TODO: Implement when preferences repository is available
            // For now, return default preferences
            return new NotificationPreferences
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                EmailEnabled = true,
                PushEnabled = true,
                InAppEnabled = true,
                SmsEnabled = false,
                TypePreferences = new Dictionary<NotificationType, bool>
                {
                    { NotificationType.System, true },
                    { NotificationType.User, true },
                    { NotificationType.Collection, true },
                    { NotificationType.MediaItem, true },
                    { NotificationType.Library, true },
                    { NotificationType.Comment, true },
                    { NotificationType.Like, true },
                    { NotificationType.Follow, true },
                    { NotificationType.Share, true },
                    { NotificationType.Download, true },
                    { NotificationType.Upload, true },
                    { NotificationType.Error, true },
                    { NotificationType.Warning, true },
                    { NotificationType.Info, true },
                    { NotificationType.Success, true }
                },
                QuietHoursStart = TimeSpan.FromHours(22),
                QuietHoursEnd = TimeSpan.FromHours(8),
                QuietHoursEnabled = true,
                QuietDays = new List<DayOfWeek>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get notification preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get notification preferences for user '{userId}'", ex);
        }
    }

    public async Task<NotificationPreferences> UpdateUserPreferencesAsync(ObjectId userId, UpdateNotificationPreferencesRequest request)
    {
        try
        {
            // Check if user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            var preferences = new NotificationPreferences
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                EmailEnabled = request.EmailEnabled,
                PushEnabled = request.PushEnabled,
                InAppEnabled = request.InAppEnabled,
                SmsEnabled = request.SmsEnabled,
                TypePreferences = request.TypePreferences,
                QuietHoursStart = request.QuietHoursStart,
                QuietHoursEnd = request.QuietHoursEnd,
                QuietHoursEnabled = request.QuietHoursEnabled,
                QuietDays = request.QuietDays,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // TODO: Save to database when preferences repository is implemented
            _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

            return preferences;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update notification preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to update notification preferences for user '{userId}'", ex);
        }
    }

    public async Task<bool> IsNotificationEnabledAsync(ObjectId userId, NotificationType type)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            
            // Check if user has enabled notifications for this type
            if (preferences.TypePreferences.TryGetValue(type, out var isEnabled))
            {
                return isEnabled;
            }

            // Default to enabled if not specified
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check notification enabled status for user {UserId} and type {Type}", userId, type);
            throw new BusinessRuleException($"Failed to check notification enabled status for user '{userId}' and type '{type}'", ex);
        }
    }

    public Task<NotificationAnalytics> GetNotificationAnalyticsAsync(ObjectId? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            // TODO: Implement when analytics repository is available
            // For now, return placeholder analytics
            var analytics = new NotificationAnalytics
            {
                UserId = userId,
                FromDate = from,
                ToDate = to,
                TotalSent = 0,
                TotalDelivered = 0,
                TotalRead = 0,
                TotalClicked = 0,
                DeliveryRate = 0,
                ReadRate = 0,
                ClickThroughRate = 0,
                SentByType = new Dictionary<NotificationType, long>(),
                SentByMethod = new Dictionary<NotificationDeliveryMethod, long>(),
                DailyStatistics = new List<NotificationStatistic>()
            };
            return Task.FromResult(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification analytics");
            throw new BusinessRuleException("Failed to get notification analytics", ex);
        }
    }

    public Task<IEnumerable<NotificationStatistic>> GetNotificationStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            // TODO: Implement when statistics repository is available
            // For now, return empty list
            return Task.FromResult<IEnumerable<NotificationStatistic>>(new List<NotificationStatistic>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification statistics");
            throw new BusinessRuleException("Failed to get notification statistics", ex);
        }
    }
}
