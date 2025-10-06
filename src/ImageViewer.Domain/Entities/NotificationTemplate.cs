using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Notification template entity - represents notification templates for different types of notifications
/// </summary>
public class NotificationTemplate : BaseEntity
{
    [BsonElement("templateName")]
    public string TemplateName { get; private set; } = string.Empty;

    [BsonElement("templateType")]
    public string TemplateType { get; private set; } = string.Empty; // "email", "push", "sms", "in_app"

    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty; // "system", "social", "security", "content"

    [BsonElement("subject")]
    public string Subject { get; private set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; private set; } = string.Empty;

    [BsonElement("htmlContent")]
    public string? HtmlContent { get; private set; }

    [BsonElement("variables")]
    public List<string> Variables { get; private set; } = new(); // Template variables like {userName}, {collectionName}

    [BsonElement("priority")]
    public string Priority { get; private set; } = "normal"; // "low", "normal", "high", "urgent"

    [BsonElement("channels")]
    public List<string> Channels { get; private set; } = new(); // "email", "push", "sms", "in_app"

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isSystemTemplate")]
    public bool IsSystemTemplate { get; private set; } = false;

    [BsonElement("language")]
    public string Language { get; private set; } = "en";

    [BsonElement("version")]
    public int Version { get; private set; } = 1;

    [BsonElement("parentTemplateId")]
    public ObjectId? ParentTemplateId { get; private set; }

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("usageCount")]
    public long UsageCount { get; private set; } = 0;

    [BsonElement("lastUsedAt")]
    public DateTime? LastUsedAt { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public NotificationTemplate? ParentTemplate { get; private set; }

    [BsonIgnore]
    public List<NotificationTemplate> ChildTemplates { get; private set; } = new();

    // Private constructor for EF Core
    private NotificationTemplate() { }

    public NotificationTemplate(
        string templateName,
        string templateType,
        string category,
        string subject,
        string content)
    {
        TemplateName = templateName;
        TemplateType = templateType;
        Category = category;
        Subject = subject;
        Content = content;
        Priority = "normal";
        IsActive = true;
        IsSystemTemplate = false;
        Language = "en";
        Version = 1;
        Variables = ExtractVariables(content);
        Channels = new List<string> { templateType };
        Tags = new List<string>();
        UsageCount = 0;
    }

    public void UpdateContent(string subject, string content, string? htmlContent = null)
    {
        Subject = subject;
        Content = content;
        HtmlContent = htmlContent;
        Variables = ExtractVariables(content);
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetHtmlContent(string htmlContent)
    {
        HtmlContent = htmlContent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePriority(string priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddChannel(string channel)
    {
        if (!Channels.Contains(channel))
        {
            Channels.Add(channel);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveChannel(string channel)
    {
        if (Channels.Contains(channel))
        {
            Channels.Remove(channel);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddTag(string tag)
    {
        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTag(string tag)
    {
        if (Tags.Contains(tag))
        {
            Tags.Remove(tag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsUsed()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParentTemplate(ObjectId? parentTemplateId)
    {
        ParentTemplateId = parentTemplateId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTemplateName(string templateName)
    {
        TemplateName = templateName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTemplateType(string templateType)
    {
        TemplateType = templateType;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCategory(string category)
    {
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLanguage(string language)
    {
        Language = language;
        UpdatedAt = DateTime.UtcNow;
    }

    public string RenderTemplate(Dictionary<string, string> variables)
    {
        var renderedContent = Content;
        var renderedSubject = Subject;

        foreach (var variable in variables)
        {
            var placeholder = $"{{{variable.Key}}}";
            renderedContent = renderedContent.Replace(placeholder, variable.Value);
            renderedSubject = renderedSubject.Replace(placeholder, variable.Value);
        }

        return renderedContent;
    }

    public string RenderSubject(Dictionary<string, string> variables)
    {
        var renderedSubject = Subject;

        foreach (var variable in variables)
        {
            var placeholder = $"{{{variable.Key}}}";
            renderedSubject = renderedSubject.Replace(placeholder, variable.Value);
        }

        return renderedSubject;
    }

    public bool HasVariable(string variableName)
    {
        return Variables.Contains(variableName);
    }

    public bool IsValidForChannel(string channel)
    {
        return Channels.Contains(channel);
    }

    private List<string> ExtractVariables(string content)
    {
        var variables = new List<string>();
        var regex = new System.Text.RegularExpressions.Regex(@"\{(\w+)\}");
        var matches = regex.Matches(content);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            if (!variables.Contains(variableName))
            {
                variables.Add(variableName);
            }
        }

        return variables;
    }
}
