using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.Notifications;

/// <summary>
/// DTO for template usage statistics
/// </summary>
public class TemplateUsageStatistics
{
    /// <summary>
    /// The ID of the template
    /// </summary>
    public ObjectId TemplateId { get; set; }

    /// <summary>
    /// The name of the template
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// The type of the template
    /// </summary>
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// The category of the template
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The current version of the template
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The number of times this template has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// The date and time when the template was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// The date and time when the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the template was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The most used channels for this template
    /// </summary>
    public List<string> MostUsedChannels { get; set; } = new List<string>();

    /// <summary>
    /// The tags associated with this template
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// Whether the template is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this is a system template
    /// </summary>
    public bool IsSystemTemplate { get; set; }
}
