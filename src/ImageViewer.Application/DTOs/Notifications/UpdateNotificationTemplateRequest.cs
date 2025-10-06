using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Notifications;

/// <summary>
/// Request DTO for updating a notification template
/// </summary>
public class UpdateNotificationTemplateRequest
{
    /// <summary>
    /// The name of the template
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string? TemplateName { get; set; }

    /// <summary>
    /// The type of the template (e.g., "email", "push", "sms")
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public string? TemplateType { get; set; }

    /// <summary>
    /// The category of the template (e.g., "system", "security", "social")
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public string? Category { get; set; }

    /// <summary>
    /// The subject of the notification
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Subject { get; set; }

    /// <summary>
    /// The content of the notification
    /// </summary>
    [StringLength(5000, MinimumLength = 1)]
    public string? Content { get; set; }

    /// <summary>
    /// Optional HTML content for the notification
    /// </summary>
    [StringLength(10000)]
    public string? HtmlContent { get; set; }

    /// <summary>
    /// The priority of the notification (e.g., "low", "normal", "high", "urgent")
    /// </summary>
    [StringLength(20)]
    public string? Priority { get; set; }

    /// <summary>
    /// The language of the template (e.g., "en", "es", "fr")
    /// </summary>
    [StringLength(10)]
    public string? Language { get; set; }

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Whether this is a system template
    /// </summary>
    public bool? IsSystemTemplate { get; set; }

    /// <summary>
    /// The channels this template supports
    /// </summary>
    public List<string>? Channels { get; set; }

    /// <summary>
    /// Tags associated with this template
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// The ID of the parent template (for versioning)
    /// </summary>
    public MongoDB.Bson.ObjectId? ParentTemplateId { get; set; }
}
