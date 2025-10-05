using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// User setting entity - represents user preferences and configuration settings
/// </summary>
public class UserSetting : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("settingKey")]
    public string SettingKey { get; private set; } = string.Empty;

    [BsonElement("settingValue")]
    public string SettingValue { get; private set; } = string.Empty;

    [BsonElement("settingType")]
    public string SettingType { get; private set; } = "String"; // String, Integer, Boolean, JSON, etc.

    [BsonElement("category")]
    public string Category { get; private set; } = "General"; // General, UI, Privacy, Notifications, etc.

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("isEncrypted")]
    public bool IsEncrypted { get; private set; } = false;

    [BsonElement("isSensitive")]
    public bool IsSensitive { get; private set; } = false;

    [BsonElement("defaultValue")]
    public string? DefaultValue { get; private set; }

    [BsonElement("validationRules")]
    public Dictionary<string, object> ValidationRules { get; private set; } = new();

    [BsonElement("lastModifiedBy")]
    public ObjectId? LastModifiedBy { get; private set; }

    [BsonElement("source")]
    public string Source { get; private set; } = "User"; // User, System, Import, etc.

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;

    [BsonIgnore]
    public User? LastModifiedByUser { get; private set; }

    // Private constructor for EF Core
    private UserSetting() { }

    public static UserSetting Create(ObjectId userId, string settingKey, string settingValue, string settingType = "String", string category = "General", string? description = null, bool isEncrypted = false, bool isSensitive = false, string? defaultValue = null, string source = "User")
    {
        if (string.IsNullOrWhiteSpace(settingKey))
            throw new ArgumentException("Setting key cannot be empty", nameof(settingKey));

        if (string.IsNullOrWhiteSpace(settingType))
            throw new ArgumentException("Setting type cannot be empty", nameof(settingType));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        return new UserSetting
        {
            UserId = userId,
            SettingKey = settingKey,
            SettingValue = settingValue,
            SettingType = settingType,
            Category = category,
            Description = description,
            IsEncrypted = isEncrypted,
            IsSensitive = isSensitive,
            DefaultValue = defaultValue,
            Source = source,
            ValidationRules = new Dictionary<string, object>()
        };
    }

    public void UpdateValue(string newValue, ObjectId? modifiedBy = null)
    {
        SettingValue = newValue;
        LastModifiedBy = modifiedBy;
        UpdateTimestamp();
    }

    public void UpdateType(string newType)
    {
        if (string.IsNullOrWhiteSpace(newType))
            throw new ArgumentException("Setting type cannot be empty", nameof(newType));

        SettingType = newType;
        UpdateTimestamp();
    }

    public void UpdateCategory(string newCategory)
    {
        if (string.IsNullOrWhiteSpace(newCategory))
            throw new ArgumentException("Category cannot be empty", nameof(newCategory));

        Category = newCategory;
        UpdateTimestamp();
    }

    public void SetEncryption(bool isEncrypted)
    {
        IsEncrypted = isEncrypted;
        UpdateTimestamp();
    }

    public void SetSensitivity(bool isSensitive)
    {
        IsSensitive = isSensitive;
        UpdateTimestamp();
    }

    public void AddValidationRule(string key, object value)
    {
        ValidationRules[key] = value;
        UpdateTimestamp();
    }

    public void ResetToDefault()
    {
        if (DefaultValue != null)
        {
            SettingValue = DefaultValue;
            UpdateTimestamp();
        }
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
}
