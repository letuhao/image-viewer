using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Copyright management entity - represents copyright information and management
/// </summary>
public class CopyrightManagement : BaseEntity
{
    [BsonElement("contentId")]
    public ObjectId ContentId { get; private set; }

    [BsonElement("contentType")]
    public string ContentType { get; private set; } = string.Empty; // "image", "collection"

    [BsonElement("ownerId")]
    public ObjectId? OwnerId { get; private set; }

    [BsonElement("copyrightStatus")]
    public string CopyrightStatus { get; private set; } = string.Empty; // "unknown", "copyrighted", "public_domain", "creative_commons", "fair_use"

    [BsonElement("licenseType")]
    public string LicenseType { get; private set; } = string.Empty; // "all_rights_reserved", "cc_by", "cc_by_sa", "cc_by_nc", "cc0", "public_domain"

    [BsonElement("attribution")]
    public string Attribution { get; private set; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; private set; } = string.Empty;

    [BsonElement("originalUrl")]
    public string OriginalUrl { get; private set; } = string.Empty;

    [BsonElement("detectionMethod")]
    public string DetectionMethod { get; private set; } = string.Empty; // "manual", "automated", "ai_detection", "user_report"

    [BsonElement("confidence")]
    public double Confidence { get; private set; } // 0.0 to 1.0

    [BsonElement("verified")]
    public bool Verified { get; private set; }

    [BsonElement("verifiedBy")]
    public ObjectId? VerifiedBy { get; private set; }

    [BsonElement("verifiedAt")]
    public DateTime? VerifiedAt { get; private set; }

    [BsonElement("dmcaReports")]
    public List<DMCAReport> DMCAReports { get; private set; } = new();

    [BsonElement("permissions")]
    public List<ContentPermission> Permissions { get; private set; } = new();

    [BsonElement("restrictions")]
    public List<string> Restrictions { get; private set; } = new(); // "commercial_use", "modification", "distribution"

    [BsonElement("expirationDate")]
    public DateTime? ExpirationDate { get; private set; }

    [BsonElement("notes")]
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    [BsonIgnore]
    public User? Owner { get; private set; }

    [BsonIgnore]
    public User? Verifier { get; private set; }

    // Private constructor for EF Core
    private CopyrightManagement() { }

    public CopyrightManagement(
        ObjectId contentId,
        string contentType,
        string copyrightStatus = "unknown",
        string licenseType = "all_rights_reserved")
    {
        ContentId = contentId;
        ContentType = contentType;
        CopyrightStatus = copyrightStatus;
        LicenseType = licenseType;
        DetectionMethod = "manual";
        Confidence = 0.5;
        Verified = false;
        DMCAReports = new List<DMCAReport>();
        Permissions = new List<ContentPermission>();
        Restrictions = new List<string>();
    }

    public void SetOwner(ObjectId ownerId)
    {
        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCopyrightInfo(string status, string licenseType, double confidence = 0.5)
    {
        CopyrightStatus = status;
        LicenseType = licenseType;
        Confidence = confidence;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify(ObjectId verifierId)
    {
        Verified = true;
        VerifiedBy = verifierId;
        VerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddDMCAReport(DMCAReport report)
    {
        DMCAReports.Add(report);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPermission(ContentPermission permission)
    {
        Permissions.Add(permission);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRestriction(string restriction)
    {
        if (!Restrictions.Contains(restriction))
        {
            Restrictions.Add(restriction);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsCommercialUseAllowed()
    {
        return !Restrictions.Contains("commercial_use") && 
               (LicenseType == "cc0" || LicenseType == "public_domain" || LicenseType == "cc_by");
    }

    public bool IsModificationAllowed()
    {
        return !Restrictions.Contains("modification") && 
               (LicenseType == "cc0" || LicenseType == "public_domain" || LicenseType == "cc_by" || LicenseType == "cc_by_sa");
    }
}

/// <summary>
/// DMCA report entity
/// </summary>
public class DMCAReport
{
    [BsonElement("reportId")]
    public string ReportId { get; set; } = string.Empty;

    [BsonElement("reporterName")]
    public string ReporterName { get; set; } = string.Empty;

    [BsonElement("reporterEmail")]
    public string ReporterEmail { get; set; } = string.Empty;

    [BsonElement("copyrightOwner")]
    public string CopyrightOwner { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty; // "pending", "processing", "resolved", "rejected"

    [BsonElement("submittedAt")]
    public DateTime SubmittedAt { get; set; }

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("evidence")]
    public List<string> Evidence { get; set; } = new();
}

/// <summary>
/// Content permission entity
/// </summary>
public class ContentPermission
{
    [BsonElement("permissionType")]
    public string PermissionType { get; set; } = string.Empty; // "use", "modify", "distribute", "commercial"

    [BsonElement("grantedTo")]
    public ObjectId? GrantedTo { get; set; }

    [BsonElement("grantedBy")]
    public ObjectId GrantedBy { get; set; }

    [BsonElement("grantedAt")]
    public DateTime GrantedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [BsonElement("conditions")]
    public string Conditions { get; set; } = string.Empty;

    [BsonElement("active")]
    public bool Active { get; set; } = true;
}
