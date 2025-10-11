using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Reward achievement entity - represents user achievements and milestones
/// </summary>
public class RewardAchievement : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = string.Empty; // Collection, Upload, Download, Share, Comment, etc.

    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty; // Bronze, Silver, Gold, Platinum, Diamond

    [BsonElement("tier")]
    public int Tier { get; private set; } = 1;

    [BsonElement("points")]
    public int Points { get; private set; } = 0;

    [BsonElement("iconUrl")]
    public string? IconUrl { get; private set; }

    [BsonElement("badgeUrl")]
    public string? BadgeUrl { get; private set; }

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isSecret")]
    public bool IsSecret { get; private set; } = false;

    [BsonElement("isRepeatable")]
    public bool IsRepeatable { get; private set; } = false;

    [BsonElement("maxRepeats")]
    public int? MaxRepeats { get; private set; }

    [BsonElement("requirements")]
    public Dictionary<string, object> Requirements { get; private set; } = new();

    [BsonElement("conditions")]
    public List<string> Conditions { get; private set; } = new();

    [BsonElement("rewardItems")]
    public List<RewardItem> RewardItems { get; private set; } = new();

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    [BsonElement("startDate")]
    public DateTime? StartDate { get; private set; }

    [BsonElement("endDate")]
    public DateTime? EndDate { get; private set; }

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0;

    [BsonElement("difficulty")]
    public string Difficulty { get; private set; } = "Easy"; // Easy, Medium, Hard, Expert

    [BsonElement("rarity")]
    public string Rarity { get; private set; } = "Common"; // Common, Uncommon, Rare, Epic, Legendary

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("createdBy")]
    public new ObjectId? CreatedBy { get; private set; }

    [BsonElement("totalEarned")]
    public int TotalEarned { get; private set; } = 0;

    [BsonElement("totalEarners")]
    public int TotalEarners { get; private set; } = 0;

    // Navigation properties
    [BsonIgnore]
    public User? Creator { get; private set; }

    // Private constructor for EF Core
    private RewardAchievement() { }

    public static RewardAchievement Create(string name, string type, string category, int points, ObjectId? createdBy = null, string? description = null, int tier = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        if (points < 0)
            throw new ArgumentException("Points cannot be negative", nameof(points));

        if (tier < 1)
            throw new ArgumentException("Tier must be at least 1", nameof(tier));

        return new RewardAchievement
        {
            Name = name,
            Type = type,
            Category = category,
            Points = points,
            Description = description,
            Tier = tier,
            CreatedBy = createdBy,
            IsActive = true,
            IsSecret = false,
            IsRepeatable = false,
            Priority = 0,
            Difficulty = "Easy",
            Rarity = "Common",
            TotalEarned = 0,
            TotalEarners = 0,
            Requirements = new Dictionary<string, object>(),
            Conditions = new List<string>(),
            RewardItems = new List<RewardItem>(),
            Metadata = new Dictionary<string, object>(),
            Tags = new List<string>()
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

    public void UpdatePoints(int points)
    {
        if (points < 0)
            throw new ArgumentException("Points cannot be negative", nameof(points));

        Points = points;
        UpdateTimestamp();
    }

    public void SetTier(int tier)
    {
        if (tier < 1)
            throw new ArgumentException("Tier must be at least 1", nameof(tier));

        Tier = tier;
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

    public void SetRepeatable(bool isRepeatable, int? maxRepeats = null)
    {
        IsRepeatable = isRepeatable;
        MaxRepeats = maxRepeats;
        UpdateTimestamp();
    }

    public void SetDifficulty(string difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty))
            throw new ArgumentException("Difficulty cannot be empty", nameof(difficulty));

        Difficulty = difficulty;
        UpdateTimestamp();
    }

    public void SetRarity(string rarity)
    {
        if (string.IsNullOrWhiteSpace(rarity))
            throw new ArgumentException("Rarity cannot be empty", nameof(rarity));

        Rarity = rarity;
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetAvailability(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void SetIconUrls(string? iconUrl, string? badgeUrl)
    {
        IconUrl = iconUrl;
        BadgeUrl = badgeUrl;
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

    public void AddCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            throw new ArgumentException("Condition cannot be empty", nameof(condition));

        if (!Conditions.Contains(condition))
        {
            Conditions.Add(condition);
            UpdateTimestamp();
        }
    }

    public void RemoveCondition(string condition)
    {
        Conditions.Remove(condition);
        UpdateTimestamp();
    }

    public void AddRewardItem(RewardItem rewardItem)
    {
        if (rewardItem == null)
            throw new ArgumentNullException(nameof(rewardItem));

        RewardItems.Add(rewardItem);
        UpdateTimestamp();
    }

    public void RemoveRewardItem(RewardItem rewardItem)
    {
        RewardItems.Remove(rewardItem);
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

    public void RecordEarned()
    {
        TotalEarned++;
        UpdateTimestamp();
    }

    public void RecordNewEarner()
    {
        TotalEarners++;
        UpdateTimestamp();
    }

    public bool IsAvailable()
    {
        var now = DateTime.UtcNow;
        
        if (!IsActive)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt.Value <= now)
            return false;

        if (StartDate.HasValue && StartDate.Value > now)
            return false;

        if (EndDate.HasValue && EndDate.Value <= now)
            return false;

        return true;
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public double GetCompletionRate()
    {
        if (TotalEarners == 0) return 0;
        return (double)TotalEarned / TotalEarners;
    }

    public bool IsRare()
    {
        return Rarity == "Rare" || Rarity == "Epic" || Rarity == "Legendary";
    }

    public bool IsDifficult()
    {
        return Difficulty == "Hard" || Difficulty == "Expert";
    }
}

/// <summary>
/// Reward item entity
/// </summary>
public class RewardItem
{
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty; // Points, Badge, Unlock, Discount, etc.

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("value")]
    public object Value { get; set; } = 0;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("iconUrl")]
    public string? IconUrl { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static RewardItem Create(string type, string name, object value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new RewardItem
        {
            Type = type,
            Name = name,
            Value = value,
            Description = description,
            Metadata = new Dictionary<string, object>()
        };
    }
}
