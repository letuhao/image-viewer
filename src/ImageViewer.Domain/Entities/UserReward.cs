using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// UserReward - represents user rewards and achievements
/// </summary>
public class UserReward : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("rewardType")]
    public string RewardType { get; private set; } = string.Empty;

    [BsonElement("rewardName")]
    public string RewardName { get; private set; } = string.Empty;

    [BsonElement("rewardDescription")]
    public string RewardDescription { get; private set; } = string.Empty;

    [BsonElement("points")]
    public int Points { get; private set; }

    [BsonElement("isEarned")]
    public bool IsEarned { get; private set; }

    [BsonElement("earnedAt")]
    public DateTime? EarnedAt { get; private set; }

    [BsonElement("isClaimed")]
    public bool IsClaimed { get; private set; }

    [BsonElement("claimedAt")]
    public DateTime? ClaimedAt { get; private set; }

    [BsonElement("requirements")]
    public Dictionary<string, object> Requirements { get; private set; } = new();

    [BsonElement("progress")]
    public Dictionary<string, object> Progress { get; private set; } = new();

    [BsonElement("tier")]
    public string? Tier { get; private set; }

    [BsonElement("badgeUrl")]
    public string? BadgeUrl { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;

    // Private constructor for MongoDB
    private UserReward() { }

    public UserReward(ObjectId userId, string rewardType, string rewardName, string rewardDescription, int points)
    {
        UserId = userId;
        RewardType = rewardType ?? throw new ArgumentNullException(nameof(rewardType));
        RewardName = rewardName ?? throw new ArgumentNullException(nameof(rewardName));
        RewardDescription = rewardDescription ?? throw new ArgumentNullException(nameof(rewardDescription));
        Points = points;
        IsEarned = false;
        IsClaimed = false;
    }

    public void Earn()
    {
        IsEarned = true;
        EarnedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Claim()
    {
        if (!IsEarned)
            throw new InvalidOperationException("Cannot claim a reward that hasn't been earned yet");

        IsClaimed = true;
        ClaimedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRequirement(string key, object value)
    {
        Requirements[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(string key, object value)
    {
        Progress[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTier(string tier)
    {
        Tier = tier;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBadgeUrl(string badgeUrl)
    {
        BadgeUrl = badgeUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public double GetProgressPercentage()
    {
        if (Requirements.Count == 0) return 0;

        var completedRequirements = 0;
        foreach (var requirement in Requirements)
        {
            if (Progress.ContainsKey(requirement.Key))
            {
                var required = Convert.ToDouble(requirement.Value);
                var current = Convert.ToDouble(Progress[requirement.Key]);
                if (current >= required)
                {
                    completedRequirements++;
                }
            }
        }

        return (double)completedRequirements / Requirements.Count * 100;
    }
}
