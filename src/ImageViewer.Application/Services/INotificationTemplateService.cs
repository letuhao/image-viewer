using MongoDB.Bson;
using NotificationTemplateEntity = ImageViewer.Domain.Entities.NotificationTemplate;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for Notification Template Management features
/// </summary>
public interface INotificationTemplateService
{
    /// <summary>
    /// Creates a new notification template.
    /// </summary>
    /// <param name="request">The request containing template details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created notification template.</returns>
    Task<NotificationTemplateEntity> CreateTemplateAsync(DTOs.Notifications.CreateNotificationTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification template by its ID.
    /// </summary>
    /// <param name="templateId">The ID of the template to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification template, or null if not found.</returns>
    Task<NotificationTemplateEntity?> GetTemplateByIdAsync(ObjectId templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification template by its name.
    /// </summary>
    /// <param name="templateName">The name of the template to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification template, or null if not found.</returns>
    Task<NotificationTemplateEntity?> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all notification templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all notification templates.</returns>
    Task<IEnumerable<NotificationTemplateEntity>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification template.
    /// </summary>
    /// <param name="templateId">The ID of the template to update.</param>
    /// <param name="request">The request containing updated template details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated notification template.</returns>
    Task<NotificationTemplateEntity> UpdateTemplateAsync(ObjectId templateId, DTOs.Notifications.UpdateNotificationTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification template by its ID.
    /// </summary>
    /// <param name="templateId">The ID of the template to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the template was deleted, false otherwise.</returns>
    Task<bool> DeleteTemplateAsync(ObjectId templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a notification template with provided variables.
    /// </summary>
    /// <param name="templateName">The name of the template to render.</param>
    /// <param name="variables">A dictionary of variables to replace in the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered content of the template.</returns>
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a notification template.
    /// </summary>
    /// <param name="templateId">The ID of the template to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The activated notification template.</returns>
    Task<NotificationTemplateEntity> ActivateTemplateAsync(ObjectId templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a notification template.
    /// </summary>
    /// <param name="templateId">The ID of the template to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deactivated notification template.</returns>
    Task<NotificationTemplateEntity> DeactivateTemplateAsync(ObjectId templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves notification templates by type.
    /// </summary>
    /// <param name="templateType">The type of templates to retrieve (e.g., "email", "push").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of notification templates matching the specified type.</returns>
    Task<IEnumerable<NotificationTemplateEntity>> GetTemplatesByTypeAsync(string templateType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves notification templates by category.
    /// </summary>
    /// <param name="category">The category of templates to retrieve (e.g., "system", "security").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of notification templates matching the specified category.</returns>
    Task<IEnumerable<NotificationTemplateEntity>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active notification templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active notification templates.</returns>
    Task<IEnumerable<NotificationTemplateEntity>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves notification templates by language.
    /// </summary>
    /// <param name="language">The language of templates to retrieve (e.g., "en", "es").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of notification templates matching the specified language.</returns>
    Task<IEnumerable<NotificationTemplateEntity>> GetTemplatesByLanguageAsync(string language, CancellationToken cancellationToken = default);
}