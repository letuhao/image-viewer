using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Reward badge entity - represents user badges and achievements
/// </summary>
public class RewardBadge : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = string.Empty; // Achievement, Milestone, Special, Event, etc.

    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty; // Collection, Upload, Social, Premium, etc.

    [BsonElement("level")]
    public int Level { get; private set; } = 1;

    [BsonElement("rarity")]
    public string Rarity { get; private set; } = "Common"; // Common, Uncommon, Rare, Epic, Legendary, Mythic

    [BsonElement("iconUrl")]
    public string? IconUrl { get; private set; }

    [BsonElement("badgeUrl")]
    public string? BadgeUrl { get; private set; }

    [BsonElement("frameUrl")]
    public string? FrameUrl { get; private set; }

    [BsonElement("color")]
    public string? Color { get; private set; } // Hex color code

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isSecret")]
    public bool IsSecret { get; private set; } = false;

    [BsonElement("isLimited")]
    public bool IsLimited { get; private set; } = false;

    [BsonElement("isPremium")]
    public bool IsPremium { get; private set; } = false;

    [BsonElement("points")]
    public int Points { get; private set; } = 0;

    [BsonElement("experience")]
    public int Experience { get; private set; } = 0;

    [BsonElement("requirements")]
    public Dictionary<string, object> Requirements { get; private set; } = new();

    [BsonElement("unlockConditions")]
    public List<string> UnlockConditions { get; private set; } = new();

    [BsonElement("benefits")]
    public List<string> Benefits { get; private set; } = new();

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    [BsonElement("availableFrom")]
    public DateTime? AvailableFrom { get; private set; }

    [BsonElement("availableUntil")]
    public DateTime? AvailableUntil { get; private set; }

    [BsonElement("maxEarners")]
    public int? MaxEarners { get; private set; }

    [BsonElement("currentEarners")]
    public int CurrentEarners { get; private set; } = 0;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; private set; } = 0;

    [BsonElement("isShowcase")]
    public bool IsShowcase { get; private set; } = false;

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public ObjectId? CreatedBy { get; private set; }

    [BsonElement("parentBadgeId")]
    public ObjectId? ParentBadgeId { get; private set; }

    [BsonElement("childBadges")]
    public List<ObjectId> ChildBadges { get; private set; } = new();

    // Navigation properties
    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public RewardBadge? ParentBadge { get; private set; }

    // Private constructor for EF Core
    private RewardBadge() { }

    public static RewardBadge Create(string name, string type, string category, ObjectId? createdBy = null, string? description = null, int level = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        if (level < 1)
            throw new ArgumentException("Level must be at least 1", nameof(level));

        return new RewardBadge
        {
            Name = name,
            Type = type,
            Category = category,
            Level = level,
            Description = description,
            CreatedBy = createdBy,
            IsActive = true,
            IsSecret = false,
            IsLimited = false,
            IsPremium = false,
            Points = 0,
            Experience = 0,
            CurrentEarners = 0,
            DisplayOrder = 0,
            IsShowcase = false,
            Requirements = new Dictionary<string, object>(),
            UnlockConditions = new List<string>(),
            Benefits = new List<string>(),
            Tags = new List<string>(),
            Metadata = new Dictionary<string, object>(),
            ChildBadges = new List<ObjectId>()
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

    public void SetLevel(int level)
    {
        if (level < 1)
            throw new ArgumentException("Level must be at least 1", nameof(level));

        Level = level;
        UpdateTimestamp();
    }

    public void SetRarity(string rarity)
    {
        if (string.IsNullOrWhiteSpace(rarity))
            throw new ArgumentException("Rarity cannot be empty", nameof(rarity));

        Rarity = rarity;
        UpdateTimestamp();
    }

    public void SetRewards(int points, int experience)
    {
        if (points < 0)
            throw new ArgumentException("Points cannot be negative", nameof(points));

        if (experience < 0)
            throw new ArgumentException("Experience cannot be negative", nameof(experience));

        Points = points;
        Experience = experience;
        UpdateTimestamp();
    }

    public void SetUrls(string? iconUrl, string? badgeUrl, string? frameUrl)
    {
        IconUrl = iconUrl;
        BadgeUrl = badgeUrl;
        FrameUrl = frameUrl;
        UpdateTimestamp();
    }

    public void SetColor(string? color)
    {
        Color = color;
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetSecret(bool isSecret)
    {
        IsSecret = isSecret;
        UpdateTimestamp();
    }

    public void SetLimited(bool isLimited, int? maxEarners = null)
    {
        IsLimited = isLimited;
        MaxEarners = maxEarners;
        UpdateTimestamp();
    }

    public void SetPremium(bool isPremium)
    {
        IsPremium = isPremium;
        UpdateTimestamp();
    }

    public void SetShowcase(bool isShowcase)
    {
        IsShowcase = isShowcase;
        UpdateTimestamp();
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdateTimestamp();
    }

    public void SetAvailability(DateTime? availableFrom, DateTime? availableUntil)
    {
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void SetParentBadge(ObjectId? parentBadgeId)
    {
        ParentBadgeId = parentBadgeId;
        UpdateTimestamp();
    }

    public void AddChildBadge(ObjectId childBadgeId)
    {
        if (!ChildBadges.Contains(childBadgeId))
        {
            ChildBadges.Add(childBadgeId);
            UpdateTimestamp();
        }
    }

    public void RemoveChildBadge(ObjectId childBadgeId)
    {
        ChildBadges.Remove(childBadgeId);
        UpdateTimestamp();
    }

    public void AddRequirement(string key, object value)
    {
        Requirements[key] = value;
        UpdateTimestamp();
    }

    public void RemoveRequirement(string key)
    {
        Requirements.Remove(key);
        UpdateTimestamp();
    }

    public void AddUnlockCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            throw new ArgumentException("Condition cannot be empty", nameof(condition));

        if (!UnlockConditions.Contains(condition))
        {
            UnlockConditions.Add(condition);
            UpdateTimestamp();
        }
    }

    public void RemoveUnlockCondition(string condition)
    {
        UnlockConditions.Remove(condition);
        UpdateTimestamp();
    }

    public void AddBenefit(string benefit)
    {
        if (string.IsNullOrWhiteSpace(benefit))
            throw new ArgumentException("Benefit cannot be empty", nameof(benefit));

        if (!Benefits.Contains(benefit))
        {
            Benefits.Add(benefit);
            UpdateTimestamp();
        }
    }

    public void RemoveBenefit(string benefit)
    {
        Benefits.Remove(benefit);
        UpdateTimestamp();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateTimestamp();
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void RecordEarner()
    {
        CurrentEarners++;
        UpdateTimestamp();
    }

    public bool IsAvailable()
    {
        var now = DateTime.UtcNow;
        
        if (!IsActive)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt.Value <= now)
            return false;

        if (AvailableFrom.HasValue && AvailableFrom.Value > now)
            return false;

        if (AvailableUntil.HasValue && AvailableUntil.Value <= now)
            return false;

        if (MaxEarners.HasValue && CurrentEarners >= MaxEarners.Value)
            return false;

        return true;
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool IsLimitedAvailability()
    {
        return IsLimited || MaxEarners.HasValue || AvailableUntil.HasValue;
    }

    public bool IsRare()
    {
        return Rarity == "Rare" || Rarity == "Epic" || Rarity == "Legendary" || Rarity == "Mythic";
    }

    public bool IsCollectible()
    {
        return IsLimited || IsSecret || IsRare();
    }

    public double GetAvailabilityPercentage()
    {
        if (!MaxEarners.HasValue) return 100;
        return (double)CurrentEarners / MaxEarners.Value * 100;
    }
}
