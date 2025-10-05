using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// User group entity - represents user groups for permissions and organization
/// </summary>
public class UserGroup : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = "Custom"; // System, Custom, Role, Team

    [BsonElement("permissions")]
    public List<string> Permissions { get; private set; } = new();

    [BsonElement("settings")]
    public Dictionary<string, object> Settings { get; private set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isSystemGroup")]
    public bool IsSystemGroup { get; private set; } = false;

    [BsonElement("memberCount")]
    public int MemberCount { get; private set; } = 0;

    [BsonElement("maxMembers")]
    public int? MaxMembers { get; private set; }

    [BsonElement("parentGroupId")]
    public ObjectId? ParentGroupId { get; private set; }

    [BsonElement("hierarchyLevel")]
    public int HierarchyLevel { get; private set; } = 0;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public ObjectId CreatedBy { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User Creator { get; private set; } = null!;

    [BsonIgnore]
    public UserGroup? ParentGroup { get; private set; }

    [BsonIgnore]
    public List<UserGroup> ChildGroups { get; private set; } = new();

    [BsonIgnore]
    public List<UserGroupMember> Members { get; private set; } = new();

    // Private constructor for EF Core
    private UserGroup() { }

    public static UserGroup Create(string name, ObjectId createdBy, string? description = null, string type = "Custom", ObjectId? parentGroupId = null, bool isSystemGroup = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        return new UserGroup
        {
            Name = name,
            Description = description,
            Type = type,
            CreatedBy = createdBy,
            ParentGroupId = parentGroupId,
            IsActive = true,
            IsSystemGroup = isSystemGroup,
            MemberCount = 0,
            HierarchyLevel = 0,
            Permissions = new List<string>(),
            Settings = new Dictionary<string, object>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void AddPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be empty", nameof(permission));

        if (!Permissions.Contains(permission))
        {
            Permissions.Add(permission);
            UpdateTimestamp();
        }
    }

    public void RemovePermission(string permission)
    {
        Permissions.Remove(permission);
        UpdateTimestamp();
    }

    public void SetPermission(string permission, bool granted)
    {
        if (granted)
        {
            AddPermission(permission);
        }
        else
        {
            RemovePermission(permission);
        }
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    public void AddSetting(string key, object value)
    {
        Settings[key] = value;
        UpdateTimestamp();
    }

    public void RemoveSetting(string key)
    {
        Settings.Remove(key);
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetMaxMembers(int? maxMembers)
    {
        MaxMembers = maxMembers;
        UpdateTimestamp();
    }

    public void SetParentGroup(ObjectId? parentGroupId, int hierarchyLevel)
    {
        ParentGroupId = parentGroupId;
        HierarchyLevel = hierarchyLevel;
        UpdateTimestamp();
    }

    public void IncrementMemberCount()
    {
        MemberCount++;
        UpdateTimestamp();
    }

    public void DecrementMemberCount()
    {
        if (MemberCount > 0)
        {
            MemberCount--;
            UpdateTimestamp();
        }
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool CanAddMember()
    {
        return MaxMembers == null || MemberCount < MaxMembers;
    }
}

/// <summary>
/// User group member entity - represents the relationship between users and groups
/// </summary>
public class UserGroupMember : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("groupId")]
    public ObjectId GroupId { get; private set; }

    [BsonElement("role")]
    public string Role { get; private set; } = "Member"; // Owner, Admin, Moderator, Member

    [BsonElement("joinedAt")]
    public DateTime JoinedAt { get; private set; }

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("permissions")]
    public List<string> CustomPermissions { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;

    [BsonIgnore]
    public UserGroup Group { get; private set; } = null!;

    // Private constructor for EF Core
    private UserGroupMember() { }

    public static UserGroupMember Create(ObjectId userId, ObjectId groupId, string role = "Member")
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        return new UserGroupMember
        {
            UserId = userId,
            GroupId = groupId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CustomPermissions = new List<string>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    public void UpdateRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        Role = role;
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void AddCustomPermission(string permission)
    {
        if (!CustomPermissions.Contains(permission))
        {
            CustomPermissions.Add(permission);
            UpdateTimestamp();
        }
    }

    public void RemoveCustomPermission(string permission)
    {
        CustomPermissions.Remove(permission);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }
}
