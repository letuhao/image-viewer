using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Content moderation entity - represents moderation actions on content
/// </summary>
public class ContentModeration : BaseEntity
{
    [BsonElement("contentId")]
    public ObjectId ContentId { get; private set; }

    [BsonElement("contentType")]
    public string ContentType { get; private set; } = string.Empty; // "image", "collection", "comment"

    [BsonElement("moderatorId")]
    public ObjectId? ModeratorId { get; private set; }

    [BsonElement("reporterId")]
    public ObjectId? ReporterId { get; private set; }

    [BsonElement("moderationType")]
    public string ModerationType { get; private set; } = string.Empty; // "flag", "review", "appeal"

    [BsonElement("reason")]
    public string Reason { get; private set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; private set; } = string.Empty; // "pending", "approved", "rejected", "escalated"

    [BsonElement("severity")]
    public string Severity { get; private set; } = string.Empty; // "low", "medium", "high", "critical"

    [BsonElement("action")]
    public string Action { get; private set; } = string.Empty; // "none", "hide", "remove", "ban_user"

    [BsonElement("notes")]
    public string Notes { get; private set; } = string.Empty;

    [BsonElement("evidence")]
    public List<string> Evidence { get; private set; } = new(); // URLs or file paths to evidence

    [BsonElement("autoDetected")]
    public bool AutoDetected { get; private set; }

    [BsonElement("appealDeadline")]
    public DateTime? AppealDeadline { get; private set; }

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User? Moderator { get; private set; }

    [BsonIgnore]
    public User? Reporter { get; private set; }

    // Private constructor for EF Core
    private ContentModeration() { }

    public ContentModeration(
        ObjectId contentId,
        string contentType,
        string moderationType,
        string reason,
        ObjectId? reporterId = null,
        string severity = "medium")
    {
        ContentId = contentId;
        ContentType = contentType;
        ModerationType = moderationType;
        Reason = reason;
        ReporterId = reporterId;
        Status = "pending";
        Severity = severity;
        Action = "none";
        AutoDetected = false;
        Evidence = new List<string>();
    }

    public void AssignModerator(ObjectId moderatorId)
    {
        ModeratorId = moderatorId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(string status, string action = "none", string notes = "")
    {
        Status = status;
        Action = action;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;

        if (status == "resolved" || status == "approved" || status == "rejected")
        {
            ResolvedAt = DateTime.UtcNow;
        }
    }

    public void AddEvidence(string evidenceUrl)
    {
        if (!Evidence.Contains(evidenceUrl))
        {
            Evidence.Add(evidenceUrl);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetAppealDeadline(DateTime deadline)
    {
        AppealDeadline = deadline;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeAppealed()
    {
        return Status == "rejected" && 
               (AppealDeadline == null || AppealDeadline > DateTime.UtcNow);
    }
}
