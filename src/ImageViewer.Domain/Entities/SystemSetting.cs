using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// System setting entity - represents global system configuration settings
/// </summary>
public class SystemSetting : BaseEntity
{
    [BsonElement("settingKey")]
    public string SettingKey { get; private set; } = string.Empty;

    [BsonElement("settingValue")]
    public string SettingValue { get; private set; } = string.Empty;

    [BsonElement("settingType")]
    public string SettingType { get; private set; } = "String"; // String, Integer, Boolean, JSON, etc.

    [BsonElement("category")]
    public string Category { get; private set; } = "General"; // General, Security, Performance, UI, etc.

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("isEncrypted")]
    public bool IsEncrypted { get; private set; } = false;

    [BsonElement("isSensitive")]
    public bool IsSensitive { get; private set; } = false;

    [BsonElement("isReadOnly")]
    public bool IsReadOnly { get; private set; } = false;

    [BsonElement("defaultValue")]
    public string? DefaultValue { get; private set; }

    [BsonElement("validationRules")]
    public Dictionary<string, object> ValidationRules { get; private set; } = new();

    [BsonElement("lastModifiedBy")]
    public ObjectId? LastModifiedBy { get; private set; }

    [BsonElement("source")]
    public string Source { get; private set; } = "System"; // System, Admin, Import, etc.

    [BsonElement("version")]
    public int Version { get; private set; } = 1;

    [BsonElement("changeHistory")]
    public List<SettingChange> ChangeHistory { get; private set; } = new();

    [BsonElement("environment")]
    public string Environment { get; private set; } = "All"; // All, Development, Staging, Production

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    [BsonIgnore]
    public User? LastModifiedByUser { get; private set; }

    // Private constructor for EF Core
    private SystemSetting() { }

    public static SystemSetting Create(string settingKey, string settingValue, string settingType = "String", string category = "General", string? description = null, bool isEncrypted = false, bool isSensitive = false, bool isReadOnly = false, string? defaultValue = null, string source = "System", string environment = "All")
    {
        if (string.IsNullOrWhiteSpace(settingKey))
            throw new ArgumentException("Setting key cannot be empty", nameof(settingKey));

        if (string.IsNullOrWhiteSpace(settingType))
            throw new ArgumentException("Setting type cannot be empty", nameof(settingType));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        return new SystemSetting
        {
            SettingKey = settingKey,
            SettingValue = settingValue,
            SettingType = settingType,
            Category = category,
            Description = description,
            IsEncrypted = isEncrypted,
            IsSensitive = isSensitive,
            IsReadOnly = isReadOnly,
            DefaultValue = defaultValue,
            Source = source,
            Environment = environment,
            IsActive = true,
            Version = 1,
            ValidationRules = new Dictionary<string, object>(),
            ChangeHistory = new List<SettingChange>()
        };
    }

    public void UpdateValue(string newValue, ObjectId? modifiedBy = null)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot modify read-only setting");

        var oldValue = SettingValue;
        SettingValue = newValue;
        LastModifiedBy = modifiedBy;
        Version++;
        
        ChangeHistory.Add(SettingChange.Create(oldValue, newValue, modifiedBy));
        UpdateTimestamp();
    }

    public void UpdateType(string newType)
    {
        if (string.IsNullOrWhiteSpace(newType))
            throw new ArgumentException("Setting type cannot be empty", nameof(newType));

        SettingType = newType;
        Version++;
        UpdateTimestamp();
    }

    public void UpdateCategory(string newCategory)
    {
        if (string.IsNullOrWhiteSpace(newCategory))
            throw new ArgumentException("Category cannot be empty", nameof(newCategory));

        Category = newCategory;
        Version++;
        UpdateTimestamp();
    }

    public void SetEncryption(bool isEncrypted)
    {
        IsEncrypted = isEncrypted;
        Version++;
        UpdateTimestamp();
    }

    public void SetSensitivity(bool isSensitive)
    {
        IsSensitive = isSensitive;
        Version++;
        UpdateTimestamp();
    }

    public void SetReadOnly(bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
        Version++;
        UpdateTimestamp();
    }

    public void AddValidationRule(string key, object value)
    {
        ValidationRules[key] = value;
        Version++;
        UpdateTimestamp();
    }

    public void ResetToDefault()
    {
        if (DefaultValue != null && !IsReadOnly)
        {
            var oldValue = SettingValue;
            SettingValue = DefaultValue;
            Version++;
            
            ChangeHistory.Add(SettingChange.Create(oldValue, DefaultValue, LastModifiedBy));
            UpdateTimestamp();
        }
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetEnvironment(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Environment cannot be empty", nameof(environment));

        Environment = environment;
        UpdateTimestamp();
    }

    public T GetValue<T>()
    {
        try
        {
            return (T)Convert.ChangeType(SettingValue, typeof(T));
        }
        catch
        {
            throw new InvalidOperationException($"Cannot convert setting value '{SettingValue}' to type {typeof(T).Name}");
        }
    }

    public bool IsValidForEnvironment(string environment)
    {
        return Environment == "All" || Environment == environment;
    }
}

/// <summary>
/// Setting change record for audit trail
/// </summary>
public class SettingChange
{
    [BsonElement("oldValue")]
    public string? OldValue { get; set; }

    [BsonElement("newValue")]
    public string NewValue { get; set; } = string.Empty;

    [BsonElement("changedAt")]
    public DateTime ChangedAt { get; set; }

    [BsonElement("changedBy")]
    public ObjectId? ChangedBy { get; set; }

    [BsonElement("reason")]
    public string? Reason { get; set; }

    public static SettingChange Create(string? oldValue, string newValue, ObjectId? changedBy = null, string? reason = null)
    {
        return new SettingChange
        {
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy,
            Reason = reason
        };
    }
}
