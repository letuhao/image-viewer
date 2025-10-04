using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// RewardTransaction - represents reward transaction history
/// </summary>
public class RewardTransaction : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("rewardId")]
    public ObjectId? RewardId { get; private set; }

    [BsonElement("transactionType")]
    public string TransactionType { get; private set; } = string.Empty;

    [BsonElement("points")]
    public int Points { get; private set; }

    [BsonElement("description")]
    public string Description { get; private set; } = string.Empty;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("isProcessed")]
    public bool IsProcessed { get; private set; }

    [BsonElement("processedAt")]
    public DateTime? ProcessedAt { get; private set; }

    [BsonElement("balanceAfter")]
    public int BalanceAfter { get; private set; }

    [BsonElement("source")]
    public string? Source { get; private set; }

    [BsonElement("referenceId")]
    public ObjectId? ReferenceId { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public UserReward? Reward { get; private set; }

    // Private constructor for MongoDB
    private RewardTransaction() { }

    public RewardTransaction(ObjectId userId, string transactionType, int points, string description, ObjectId? rewardId = null)
    {
        UserId = userId;
        TransactionType = transactionType ?? throw new ArgumentNullException(nameof(transactionType));
        Points = points;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        RewardId = rewardId;
        IsProcessed = false;
        BalanceAfter = 0;
    }

    public void Process(int balanceAfter)
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
        BalanceAfter = balanceAfter;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSource(string source)
    {
        Source = source;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetReference(ObjectId referenceId)
    {
        ReferenceId = referenceId;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsEarned()
    {
        return TransactionType == "Earn" || TransactionType == "Bonus" || TransactionType == "Achievement";
    }

    public bool IsSpent()
    {
        return TransactionType == "Spend" || TransactionType == "Redemption" || TransactionType == "Penalty";
    }
}
