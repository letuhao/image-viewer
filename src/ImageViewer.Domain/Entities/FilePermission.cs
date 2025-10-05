using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// File permission entity - represents file access permissions and sharing settings
/// </summary>
public class FilePermission : BaseEntity
{
    [BsonElement("fileId")]
    public ObjectId FileId { get; private set; }

    [BsonElement("fileName")]
    public string FileName { get; private set; } = string.Empty;

    [BsonElement("filePath")]
    public string FilePath { get; private set; } = string.Empty;

    [BsonElement("ownerId")]
    public ObjectId OwnerId { get; private set; }

    [BsonElement("permissionType")]
    public string PermissionType { get; private set; } = "Private"; // Private, Public, Shared, Restricted

    [BsonElement("accessLevel")]
    public string AccessLevel { get; private set; } = "Read"; // Read, Write, Delete, Admin

    [BsonElement("isInherited")]
    public bool IsInherited { get; private set; } = false;

    [BsonElement("parentPermissionId")]
    public ObjectId? ParentPermissionId { get; private set; }

    [BsonElement("childPermissions")]
    public List<ObjectId> ChildPermissions { get; private set; } = new();

    [BsonElement("allowedUsers")]
    public List<ObjectId> AllowedUsers { get; private set; } = new();

    [BsonElement("allowedGroups")]
    public List<ObjectId> AllowedGroups { get; private set; } = new();

    [BsonElement("allowedRoles")]
    public List<string> AllowedRoles { get; private set; } = new();

    [BsonElement("deniedUsers")]
    public List<ObjectId> DeniedUsers { get; private set; } = new();

    [BsonElement("deniedGroups")]
    public List<ObjectId> DeniedGroups { get; private set; } = new();

    [BsonElement("deniedRoles")]
    public List<string> DeniedRoles { get; private set; } = new();

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isReadOnly")]
    public bool IsReadOnly { get; private set; } = false;

    [BsonElement("isDownloadable")]
    public bool IsDownloadable { get; private set; } = true;

    [BsonElement("isShareable")]
    public bool IsShareable { get; private set; } = false;

    [BsonElement("isCommentable")]
    public bool IsCommentable { get; private set; } = false;

    [BsonElement("isRateable")]
    public bool IsRateable { get; private set; } = false;

    [BsonElement("maxDownloads")]
    public int? MaxDownloads { get; private set; }

    [BsonElement("downloadCount")]
    public int DownloadCount { get; private set; } = 0;

    [BsonElement("maxViews")]
    public int? MaxViews { get; private set; }

    [BsonElement("viewCount")]
    public int ViewCount { get; private set; } = 0;

    [BsonElement("password")]
    public string? Password { get; private set; }

    [BsonElement("isPasswordProtected")]
    public bool IsPasswordProtected { get; private set; } = false;

    [BsonElement("ipWhitelist")]
    public List<string> IpWhitelist { get; private set; } = new();

    [BsonElement("ipBlacklist")]
    public List<string> IpBlacklist { get; private set; } = new();

    [BsonElement("userAgentRestrictions")]
    public List<string> UserAgentRestrictions { get; private set; } = new();

    [BsonElement("geographicRestrictions")]
    public List<string> GeographicRestrictions { get; private set; } = new();

    [BsonElement("timeRestrictions")]
    public List<TimeRestriction> TimeRestrictions { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public ObjectId? CreatedBy { get; private set; }

    [BsonElement("lastAccessed")]
    public DateTime? LastAccessed { get; private set; }

    [BsonElement("lastModified")]
    public DateTime? LastModified { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public MediaItem File { get; private set; } = null!;

    [BsonIgnore]
    public User Owner { get; private set; } = null!;

    [BsonIgnore]
    public FilePermission? ParentPermission { get; private set; }

    [BsonIgnore]
    public User? Creator { get; private set; }

    // Private constructor for EF Core
    private FilePermission() { }

    public static FilePermission Create(ObjectId fileId, string fileName, string filePath, ObjectId ownerId, string permissionType = "Private", string accessLevel = "Read", ObjectId? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (string.IsNullOrWhiteSpace(permissionType))
            throw new ArgumentException("Permission type cannot be empty", nameof(permissionType));

        if (string.IsNullOrWhiteSpace(accessLevel))
            throw new ArgumentException("Access level cannot be empty", nameof(accessLevel));

        return new FilePermission
        {
            FileId = fileId,
            FileName = fileName,
            FilePath = filePath,
            OwnerId = ownerId,
            PermissionType = permissionType,
            AccessLevel = accessLevel,
            CreatedBy = createdBy,
            IsActive = true,
            IsReadOnly = false,
            IsDownloadable = true,
            IsShareable = false,
            IsCommentable = false,
            IsRateable = false,
            DownloadCount = 0,
            ViewCount = 0,
            IsPasswordProtected = false,
            AllowedUsers = new List<ObjectId>(),
            AllowedGroups = new List<ObjectId>(),
            AllowedRoles = new List<string>(),
            DeniedUsers = new List<ObjectId>(),
            DeniedGroups = new List<ObjectId>(),
            DeniedRoles = new List<string>(),
            ChildPermissions = new List<ObjectId>(),
            IpWhitelist = new List<string>(),
            IpBlacklist = new List<string>(),
            UserAgentRestrictions = new List<string>(),
            GeographicRestrictions = new List<string>(),
            TimeRestrictions = new List<TimeRestriction>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    public void UpdatePermissionType(string permissionType)
    {
        if (string.IsNullOrWhiteSpace(permissionType))
            throw new ArgumentException("Permission type cannot be empty", nameof(permissionType));

        PermissionType = permissionType;
        UpdateTimestamp();
    }

    public void UpdateAccessLevel(string accessLevel)
    {
        if (string.IsNullOrWhiteSpace(accessLevel))
            throw new ArgumentException("Access level cannot be empty", nameof(accessLevel));

        AccessLevel = accessLevel;
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetReadOnly(bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
        UpdateTimestamp();
    }

    public void SetDownloadable(bool isDownloadable)
    {
        IsDownloadable = isDownloadable;
        UpdateTimestamp();
    }

    public void SetShareable(bool isShareable)
    {
        IsShareable = isShareable;
        UpdateTimestamp();
    }

    public void SetCommentable(bool isCommentable)
    {
        IsCommentable = isCommentable;
        UpdateTimestamp();
    }

    public void SetRateable(bool isRateable)
    {
        IsRateable = isRateable;
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void SetPassword(string? password)
    {
        Password = password;
        IsPasswordProtected = !string.IsNullOrEmpty(password);
        UpdateTimestamp();
    }

    public void SetDownloadLimits(int? maxDownloads)
    {
        MaxDownloads = maxDownloads;
        UpdateTimestamp();
    }

    public void SetViewLimits(int? maxViews)
    {
        MaxViews = maxViews;
        UpdateTimestamp();
    }

    public void AddAllowedUser(ObjectId userId)
    {
        if (!AllowedUsers.Contains(userId))
        {
            AllowedUsers.Add(userId);
            UpdateTimestamp();
        }
    }

    public void RemoveAllowedUser(ObjectId userId)
    {
        AllowedUsers.Remove(userId);
        UpdateTimestamp();
    }

    public void AddAllowedGroup(ObjectId groupId)
    {
        if (!AllowedGroups.Contains(groupId))
        {
            AllowedGroups.Add(groupId);
            UpdateTimestamp();
        }
    }

    public void RemoveAllowedGroup(ObjectId groupId)
    {
        AllowedGroups.Remove(groupId);
        UpdateTimestamp();
    }

    public void AddAllowedRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (!AllowedRoles.Contains(role))
        {
            AllowedRoles.Add(role);
            UpdateTimestamp();
        }
    }

    public void RemoveAllowedRole(string role)
    {
        AllowedRoles.Remove(role);
        UpdateTimestamp();
    }

    public void AddDeniedUser(ObjectId userId)
    {
        if (!DeniedUsers.Contains(userId))
        {
            DeniedUsers.Add(userId);
            UpdateTimestamp();
        }
    }

    public void RemoveDeniedUser(ObjectId userId)
    {
        DeniedUsers.Remove(userId);
        UpdateTimestamp();
    }

    public void AddDeniedGroup(ObjectId groupId)
    {
        if (!DeniedGroups.Contains(groupId))
        {
            DeniedGroups.Add(groupId);
            UpdateTimestamp();
        }
    }

    public void RemoveDeniedGroup(ObjectId groupId)
    {
        DeniedGroups.Remove(groupId);
        UpdateTimestamp();
    }

    public void AddDeniedRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (!DeniedRoles.Contains(role))
        {
            DeniedRoles.Add(role);
            UpdateTimestamp();
        }
    }

    public void RemoveDeniedRole(string role)
    {
        DeniedRoles.Remove(role);
        UpdateTimestamp();
    }

    public void AddIpToWhitelist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));

        if (!IpWhitelist.Contains(ipAddress))
        {
            IpWhitelist.Add(ipAddress);
            UpdateTimestamp();
        }
    }

    public void RemoveIpFromWhitelist(string ipAddress)
    {
        IpWhitelist.Remove(ipAddress);
        UpdateTimestamp();
    }

    public void AddIpToBlacklist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));

        if (!IpBlacklist.Contains(ipAddress))
        {
            IpBlacklist.Add(ipAddress);
            UpdateTimestamp();
        }
    }

    public void RemoveIpFromBlacklist(string ipAddress)
    {
        IpBlacklist.Remove(ipAddress);
        UpdateTimestamp();
    }

    public void AddGeographicRestriction(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        if (!GeographicRestrictions.Contains(country))
        {
            GeographicRestrictions.Add(country);
            UpdateTimestamp();
        }
    }

    public void RemoveGeographicRestriction(string country)
    {
        GeographicRestrictions.Remove(country);
        UpdateTimestamp();
    }

    public void AddTimeRestriction(TimeRestriction restriction)
    {
        if (restriction == null)
            throw new ArgumentNullException(nameof(restriction));

        TimeRestrictions.Add(restriction);
        UpdateTimestamp();
    }

    public void RemoveTimeRestriction(TimeRestriction restriction)
    {
        TimeRestrictions.Remove(restriction);
        UpdateTimestamp();
    }

    public void SetParentPermission(ObjectId? parentPermissionId)
    {
        ParentPermissionId = parentPermissionId;
        IsInherited = parentPermissionId.HasValue;
        UpdateTimestamp();
    }

    public void AddChildPermission(ObjectId childPermissionId)
    {
        if (!ChildPermissions.Contains(childPermissionId))
        {
            ChildPermissions.Add(childPermissionId);
            UpdateTimestamp();
        }
    }

    public void RemoveChildPermission(ObjectId childPermissionId)
    {
        ChildPermissions.Remove(childPermissionId);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void RecordAccess()
    {
        LastAccessed = DateTime.UtcNow;
        ViewCount++;
        UpdateTimestamp();
    }

    public void RecordDownload()
    {
        DownloadCount++;
        UpdateTimestamp();
    }

    public void RecordModification()
    {
        LastModified = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool HasReachedDownloadLimit()
    {
        return MaxDownloads.HasValue && DownloadCount >= MaxDownloads.Value;
    }

    public bool HasReachedViewLimit()
    {
        return MaxViews.HasValue && ViewCount >= MaxViews.Value;
    }

    public bool IsAccessible()
    {
        return IsActive && !IsExpired() && !HasReachedViewLimit();
    }

    public bool CanDownload()
    {
        return IsDownloadable && !HasReachedDownloadLimit();
    }

    public bool IsUserAllowed(ObjectId userId)
    {
        if (DeniedUsers.Contains(userId))
            return false;

        return PermissionType == "Public" || AllowedUsers.Contains(userId);
    }

    public bool IsGroupAllowed(ObjectId groupId)
    {
        if (DeniedGroups.Contains(groupId))
            return false;

        return AllowedGroups.Contains(groupId);
    }

    public bool IsRoleAllowed(string role)
    {
        if (DeniedRoles.Contains(role))
            return false;

        return AllowedRoles.Contains(role);
    }

    public bool IsIpAllowed(string ipAddress)
    {
        if (IpBlacklist.Contains(ipAddress))
            return false;

        return IpWhitelist.Count == 0 || IpWhitelist.Contains(ipAddress);
    }

    public bool IsTimeAllowed()
    {
        if (TimeRestrictions.Count == 0)
            return true;

        var now = DateTime.UtcNow;
        return TimeRestrictions.Any(tr => tr.IsTimeAllowed(now));
    }
}

/// <summary>
/// Time restriction entity
/// </summary>
public class TimeRestriction
{
    [BsonElement("startTime")]
    public TimeSpan StartTime { get; set; }

    [BsonElement("endTime")]
    public TimeSpan EndTime { get; set; }

    [BsonElement("daysOfWeek")]
    public List<int> DaysOfWeek { get; set; } = new(); // 0 = Sunday, 1 = Monday, etc.

    [BsonElement("timezone")]
    public string Timezone { get; set; } = "UTC";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    public static TimeRestriction Create(TimeSpan startTime, TimeSpan endTime, List<int>? daysOfWeek = null, string timezone = "UTC")
    {
        return new TimeRestriction
        {
            StartTime = startTime,
            EndTime = endTime,
            DaysOfWeek = daysOfWeek ?? new List<int> { 0, 1, 2, 3, 4, 5, 6 }, // All days by default
            Timezone = timezone,
            IsActive = true
        };
    }

    public bool IsTimeAllowed(DateTime dateTime)
    {
        if (!IsActive)
            return false;

        var dayOfWeek = (int)dateTime.DayOfWeek;
        if (!DaysOfWeek.Contains(dayOfWeek))
            return false;

        var time = dateTime.TimeOfDay;
        return time >= StartTime && time <= EndTime;
    }
}
