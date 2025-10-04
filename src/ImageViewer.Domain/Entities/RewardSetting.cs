using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// RewardSetting - represents reward system configuration settings
/// </summary>
public class RewardSetting : BaseEntity
{
    [BsonElement("settingKey")]
    public string SettingKey { get; private set; } = string.Empty;

    [BsonElement("settingValue")]
    public string SettingValue { get; private set; } = string.Empty;

    [BsonElement("settingType")]
    public string SettingType { get; private set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("isActive")]
    public bool IsActive { get; private set; }

    [BsonElement("isSystemSetting")]
    public bool IsSystemSetting { get; private set; }

    [BsonElement("validationRules")]
    public Dictionary<string, object> ValidationRules { get; private set; } = new();

    [BsonElement("defaultValue")]
    public string? DefaultValue { get; private set; }

    [BsonElement("lastModifiedBy")]
    public ObjectId? LastModifiedBy { get; private set; }

    // Navigation properties
    public User? LastModifiedByUser { get; private set; }

    // Private constructor for MongoDB
    private RewardSetting() { }

    public RewardSetting(string settingKey, string settingValue, string settingType, string category, bool isSystemSetting = false)
    {
        SettingKey = settingKey ?? throw new ArgumentNullException(nameof(settingKey));
        SettingValue = settingValue ?? throw new ArgumentNullException(nameof(settingValue));
        SettingType = settingType ?? throw new ArgumentNullException(nameof(settingType));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        IsSystemSetting = isSystemSetting;
        IsActive = true;
    }

    public void UpdateValue(string newValue)
    {
        SettingValue = newValue ?? throw new ArgumentNullException(nameof(newValue));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddValidationRule(string ruleName, object ruleValue)
    {
        ValidationRules[ruleName] = ruleValue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefaultValue(string? defaultValue)
    {
        DefaultValue = defaultValue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLastModifiedBy(ObjectId? userId)
    {
        LastModifiedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public T GetValueAs<T>()
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

    public bool IsValid()
    {
        // Basic validation logic can be implemented here
        return !string.IsNullOrWhiteSpace(SettingKey) && !string.IsNullOrWhiteSpace(SettingValue);
    }
}
