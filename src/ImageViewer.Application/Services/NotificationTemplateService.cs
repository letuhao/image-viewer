using MongoDB.Bson;
using ImageViewer.Application.DTOs.Notifications;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NotificationTemplateEntity = ImageViewer.Domain.Entities.NotificationTemplate;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for Notification Template Management features
/// </summary>
public class NotificationTemplateService : INotificationTemplateService
{
    private readonly INotificationTemplateRepository _notificationTemplateRepository;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(
        INotificationTemplateRepository notificationTemplateRepository,
        ILogger<NotificationTemplateService> logger)
    {
        _notificationTemplateRepository = notificationTemplateRepository ?? throw new ArgumentNullException(nameof(notificationTemplateRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NotificationTemplateEntity> CreateTemplateAsync(global::ImageViewer.Application.DTOs.Notifications.CreateNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.TemplateName))
            throw new ValidationException("Template name cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(request.TemplateType))
            throw new ValidationException("Template type cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(request.Category))
            throw new ValidationException("Category cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ValidationException("Subject cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ValidationException("Content cannot be null or empty.");

        _logger.LogInformation("Creating new notification template: {TemplateName}", request.TemplateName);

        var existingTemplate = await _notificationTemplateRepository.GetByTemplateNameAsync(request.TemplateName, cancellationToken);
        if (existingTemplate != null)
            throw new DuplicateEntityException($"Notification template with name '{request.TemplateName}' already exists.");

        var template = new NotificationTemplateEntity(
            request.TemplateName,
            request.TemplateType,
            request.Category,
            request.Subject,
            request.Content
        );

        if (!string.IsNullOrWhiteSpace(request.HtmlContent))
            template.SetHtmlContent(request.HtmlContent);
        if (!string.IsNullOrWhiteSpace(request.Priority))
            template.UpdatePriority(request.Priority);
        if (request.Channels != null && request.Channels.Any())
        {
            foreach (var channel in request.Channels)
            {
                template.AddChannel(channel);
            }
        }
        if (request.Tags != null && request.Tags.Any())
        {
            foreach (var tag in request.Tags)
            {
                template.AddTag(tag);
            }
        }
        if (request.ParentTemplateId.HasValue)
            template.SetParentTemplate(request.ParentTemplateId.Value);

        await _notificationTemplateRepository.CreateAsync(template);

        _logger.LogInformation("Notification template {TemplateName} created successfully with ID {TemplateId}", request.TemplateName, template.Id);
        return template;
    }

    public async Task<NotificationTemplateEntity?> GetTemplateByIdAsync(ObjectId templateId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving notification template by ID: {TemplateId}", templateId);
        return await _notificationTemplateRepository.GetByIdAsync(templateId);
    }

    public async Task<NotificationTemplateEntity?> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

        _logger.LogDebug("Retrieving notification template by name: {TemplateName}", templateName);
        return await _notificationTemplateRepository.GetByTemplateNameAsync(templateName, cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplateEntity>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all notification templates.");
        return await _notificationTemplateRepository.GetAllAsync();
    }

    public async Task<NotificationTemplateEntity> UpdateTemplateAsync(ObjectId templateId, global::ImageViewer.Application.DTOs.Notifications.UpdateNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Updating notification template {TemplateId}", templateId);

        var template = await _notificationTemplateRepository.GetByIdAsync(templateId);
        if (template == null)
            throw new EntityNotFoundException($"Notification template with ID '{templateId}' not found.");

        // Update template name if provided and different
        if (!string.IsNullOrWhiteSpace(request.TemplateName) && template.TemplateName != request.TemplateName)
        {
            var existingTemplate = await _notificationTemplateRepository.GetByTemplateNameAsync(request.TemplateName, cancellationToken);
            if (existingTemplate != null && existingTemplate.Id != templateId)
                throw new DuplicateEntityException($"Notification template with name '{request.TemplateName}' already exists.");
            template.UpdateTemplateName(request.TemplateName);
        }

        // Update template type if provided
        if (!string.IsNullOrWhiteSpace(request.TemplateType))
            template.UpdateTemplateType(request.TemplateType);

        // Update category if provided
        if (!string.IsNullOrWhiteSpace(request.Category))
            template.UpdateCategory(request.Category);

        // Update content - only update if not empty, otherwise preserve original
        var newSubject = !string.IsNullOrWhiteSpace(request.Subject) ? request.Subject : template.Subject;
        var newContent = !string.IsNullOrWhiteSpace(request.Content) ? request.Content : template.Content;
        
        template.UpdateContent(newSubject, newContent, request.HtmlContent);

        if (!string.IsNullOrWhiteSpace(request.Priority))
            template.UpdatePriority(request.Priority);

        if (!string.IsNullOrWhiteSpace(request.Language))
            template.UpdateLanguage(request.Language);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) template.Activate();
            else template.Deactivate();
        }

        if (request.Channels != null)
        {
            var channelsToRemove = template.Channels.Except(request.Channels).ToList();
            var channelsToAdd = request.Channels.Except(template.Channels).ToList();

            foreach (var channel in channelsToRemove) template.RemoveChannel(channel);
            foreach (var channel in channelsToAdd) template.AddChannel(channel);
        }

        if (request.Tags != null)
        {
            var tagsToRemove = template.Tags.Except(request.Tags).ToList();
            var tagsToAdd = request.Tags.Except(template.Tags).ToList();

            foreach (var tag in tagsToRemove) template.RemoveTag(tag);
            foreach (var tag in tagsToAdd) template.AddTag(tag);
        }

        // Handle parent template ID
        if (request.ParentTemplateId.HasValue)
        {
            if (request.ParentTemplateId.Value == ObjectId.Empty)
                template.SetParentTemplate(null); // Clear parent template
            else
                template.SetParentTemplate(request.ParentTemplateId.Value);
        }

        await _notificationTemplateRepository.UpdateAsync(template);

        _logger.LogInformation("Notification template {TemplateId} updated successfully", templateId);
        return template;
    }

    public async Task<bool> DeleteTemplateAsync(ObjectId templateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting notification template {TemplateId}", templateId);

        var template = await _notificationTemplateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            _logger.LogWarning("Notification template {TemplateId} not found for deletion", templateId);
            return false;
        }

        await _notificationTemplateRepository.DeleteAsync(templateId);

        _logger.LogInformation("Notification template {TemplateId} deleted successfully", templateId);
        return true;
    }

    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        _logger.LogDebug("Rendering template {TemplateName}", templateName);

        var template = await _notificationTemplateRepository.GetByTemplateNameAsync(templateName, cancellationToken);
        if (template == null)
            throw new EntityNotFoundException($"Notification template with name '{templateName}' not found.");

        template.MarkAsUsed();
        await _notificationTemplateRepository.UpdateAsync(template); // Update usage count and last used at

        return template.RenderTemplate(variables);
    }

    public async Task<NotificationTemplateEntity> ActivateTemplateAsync(ObjectId templateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating notification template {TemplateId}", templateId);

        var template = await _notificationTemplateRepository.GetByIdAsync(templateId);
        if (template == null)
            throw new EntityNotFoundException($"Notification template with ID '{templateId}' not found.");

        template.Activate();
        await _notificationTemplateRepository.UpdateAsync(template);

        _logger.LogInformation("Notification template {TemplateId} activated successfully", templateId);
        return template;
    }

    public async Task<NotificationTemplateEntity> DeactivateTemplateAsync(ObjectId templateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating notification template {TemplateId}", templateId);

        var template = await _notificationTemplateRepository.GetByIdAsync(templateId);
        if (template == null)
            throw new EntityNotFoundException($"Notification template with ID '{templateId}' not found.");

        template.Deactivate();
        await _notificationTemplateRepository.UpdateAsync(template);

        _logger.LogInformation("Notification template {TemplateId} deactivated successfully", templateId);
        return template;
    }

    public async Task<IEnumerable<NotificationTemplateEntity>> GetTemplatesByTypeAsync(string templateType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateType))
            throw new ArgumentException("Template type cannot be null or empty", nameof(templateType));

        _logger.LogDebug("Retrieving notification templates by type: {TemplateType}", templateType);
        return await _notificationTemplateRepository.GetByTemplateTypeAsync(templateType, cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplateEntity>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));

        _logger.LogDebug("Retrieving notification templates by category: {Category}", category);
        return await _notificationTemplateRepository.GetByCategoryAsync(category, cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplateEntity>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving active notification templates.");
        return await _notificationTemplateRepository.GetActiveTemplatesAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplateEntity>> GetTemplatesByLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be null or empty", nameof(language));

        _logger.LogDebug("Retrieving notification templates by language: {Language}", language);
        return await _notificationTemplateRepository.GetByLanguageAsync(language, cancellationToken);
    }
}