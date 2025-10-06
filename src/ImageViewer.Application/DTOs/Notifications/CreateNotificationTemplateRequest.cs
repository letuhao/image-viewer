using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Notifications;

/// <summary>
/// Request DTO for creating a notification template
/// </summary>
public class CreateNotificationTemplateRequest
{
    /// <summary>
    /// The name of the template
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// The type of the template (e.g., "email", "push", "sms")
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// The category of the template (e.g., "system", "security", "social")
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The subject of the notification
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The content of the notification
    /// </summary>
    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

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
    /// Whether this is a system template
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

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
